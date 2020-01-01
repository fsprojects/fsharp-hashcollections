namespace FSharp.HashCollections

open System

module HashMap =

    module Constraints = 
        let inline hash< ^eq, ^t when ^eq: (static member GetHashCode: ^t -> int)> (o: ^t) =
            ( ^eq : (static member GetHashCode: ^t -> int) (o)) |> uint64

        let inline equals< ^eq, ^t when ^eq: (static member CheckEquality: ^t * ^t -> bool)> (o1: ^t) (o2: ^t) =
            ( ^eq : (static member CheckEquality: ^t * ^t -> bool) (o1, o2))


    let tryFindValueInList equals k (l : HashMapEntry<_, _> list) =
        //printfn  "Attempting to find hash collision [K: %A, Node: %A]" k l
        let rec findInList currentList =
            match currentList with
            | entry :: tail -> if equals entry.Key k then ValueSome entry.Value else findInList tail
            | [] -> ValueNone
        findInList l

    let [<Literal>] PartitionSize = 6
    let [<Literal>] PartitionMask = 0b111111UL
    let [<Literal>] MaxShiftValue = 32 // Partition Size amount of 1 bits

    let inline getIndexNoShift shiftedHash = shiftedHash &&& PartitionMask
    let inline getIndex keyHash shift = getIndexNoShift (keyHash >>> shift)

    let inline tryFind k (hashMap: HashMap< ^tk, ^tv, ^teq>) : ^tv voption =

        let inline equals x y = Constraints.equals< ^teq, ^tk> x y
        
        let rec getRec node remainderHash =
            //printfn "Rec: Hash: %A, Node: %A" remainderHash node
            match node with
            | TrieNodeFull(nodes) ->
                let index = getIndexNoShift remainderHash |> int
                getRec nodes.[index] (remainderHash >>> PartitionSize)
            | TrieNode(nodes) ->
                let bitPos = CompressedArray.getBitMapForIndex (getIndexNoShift remainderHash)
                if CompressedArray.boundsCheckIfSetForBitMapIndex nodes.BitMap bitPos // This checks if the bit was set in the first place.
                then
                    getRec
                        (nodes.Content.[CompressedArray.getCompressedIndexForIndexBitmap nodes.BitMap bitPos])
                        (remainderHash >>> PartitionSize)
                else ValueNone
            | TrieNodeOne(nodeIndex, node) ->
                let index = getIndexNoShift remainderHash
                if nodeIndex.Equals(index)
                then getRec node (remainderHash >>> PartitionSize)
                else ValueNone
            | EntryNode entry -> if equals entry.Key k then ValueSome entry.Value else ValueNone
            | HashCollisionNode entries -> tryFindValueInList equals k entries

        let keyHash = Constraints.hash< ^teq, _> k
        getRec hashMap.RootData keyHash

    let inline createTrieNode (nodes: CompressedArray<_>) =
        match nodes.Content.Length with
        | CompressedArray.MaxSize -> TrieNodeFull(nodes.Content)
        | _ -> TrieNode(nodes)

    let addNodesToResolveConflict existingEntry newEntry existingEntryHash currentKeyHash shift =
        let rec createRequiredDepthNodes shift =
            let existingEntryIndex = getIndex existingEntryHash shift
            let currentEntryIndex = getIndex currentKeyHash shift
            if shift >= MaxShiftValue
            then HashCollisionNode([ existingEntry; newEntry] ) // This is a hash collision node. We have reached max depth.
            else
                if existingEntryIndex <> currentEntryIndex
                then
                    TrieNode(
                        CompressedArray.empty
                        |> CompressedArray.set existingEntryIndex (EntryNode existingEntry)
                        |> CompressedArray.set currentEntryIndex (EntryNode newEntry))
                else
                    let subNode = createRequiredDepthNodes (shift + PartitionSize)
                    TrieNodeOne(existingEntryIndex, subNode)
        createRequiredDepthNodes shift

    let inline add k v (hashMap: HashMap< ^tk, ^tv, ^eq>) : HashMap< ^tk, ^tv, ^eq> =
        let inline equals x y = Constraints.equals< ^eq, ^tk> x y
        let inline hash o = Constraints.hash< ^eq, ^tk> o

        let keyHash = hash k
        let newEntry = { Key = k; Value = v }

        let rec addRec node shift =
            match node with
            | TrieNode(nodes) ->
                let index = getIndex keyHash shift
                match nodes |> CompressedArray.get index with
                | ValueSome(nodeAtPos) ->
                    let struct (newPosNode, isAdded) = addRec nodeAtPos (shift + PartitionSize)
                    let newNodes = nodes |> CompressedArray.set index newPosNode
                    struct (TrieNode newNodes, isAdded)
                | ValueNone ->
                    if nodes.Content.Length = CompressedArray.MaxSize - 1
                    then
                        let uncompressedArray = ArrayHelpers.copyArrayInsertInMiddle (int index) (EntryNode newEntry) nodes.Content
                        struct (TrieNodeFull uncompressedArray, true)
                    else struct (TrieNode (nodes |> CompressedArray.set index (EntryNode newEntry)), true)
            | TrieNodeOne(nodeIndex, nodeAtPos) ->
                let index = getIndex keyHash shift
                if nodeIndex.Equals(index)
                then
                    let struct (newPosNode, isAdded) = addRec nodeAtPos (shift + PartitionSize)
                    struct (TrieNodeOne(index, newPosNode), isAdded)
                else
                    let entryNode = EntryNode(newEntry)
                    let ca = CompressedArray.empty |> CompressedArray.set nodeIndex nodeAtPos |> CompressedArray.set index entryNode
                    struct (TrieNode(ca), true)
            | TrieNodeFull(nodes) ->
                let index = getIndex keyHash shift |> int
                let nodeAtPos = nodes.[index]
                let struct (newPosNode, isAdded) = addRec nodeAtPos (shift + PartitionSize)
                let newNodes = Array.zeroCreate CompressedArray.MaxSize
                Array.Copy(nodes, newNodes, CompressedArray.MaxSize)
                newNodes.[index] <- newPosNode
                struct (TrieNodeFull newNodes, isAdded)
            | EntryNode entry ->
                if equals entry.Key k
                then struct (EntryNode newEntry, false) // Replacement
                else struct (addNodesToResolveConflict entry newEntry (hash entry.Key) keyHash shift, true) // Create one or more levels required.
            | HashCollisionNode entries ->
                if shift < MaxShiftValue then failwithf "Not expected to exist"
                let rec replaceElementIfExists previouslySeen tailList = 
                    match tailList with
                    | entryNode :: tail -> 
                        if equals entryNode.Key k 
                        then (List.append (newEntry :: previouslySeen) tail, true)
                        else replaceElementIfExists (entryNode :: previouslySeen) tail
                    | [] -> ([], false)

                let (newList, replaced) = replaceElementIfExists [] entries
                if replaced 
                then struct (HashCollisionNode(newList), false) 
                else struct (HashCollisionNode(newEntry :: entries), true)
               
        let struct (newRootData, isAdded) = addRec hashMap.RootData 0

        { CurrentCount = if isAdded then hashMap.CurrentCount + 1 else hashMap.CurrentCount
          RootData = newRootData }

    let inline remove k (hashMap: HashMap< ^tk, ^tv, ^eq>) =

        let keyHash = Constraints.hash< ^eq, ^tk> k
        let equals = Constraints.equals< ^eq, ^tk>

        let removeInner k hashMap =
            let rec traverseNodes node nodes shift =
                let index = getIndex keyHash shift
                match nodes |> CompressedArray.get index with
                | ValueSome(subNode) ->
                    let struct (newSubNodeList, didWeRemove) =
                        match subNode with
                        | TrieNode subNodes ->
                            let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode subNodes (shift + PartitionSize)
                            match childNodeOpt with
                            | ValueSome(childNode) -> struct (nodes |> CompressedArray.set index childNode, didWeRemove)
                            | ValueNone -> struct (nodes |> CompressedArray.unset index, didWeRemove)
                        | TrieNodeFull(subNodes) ->
                            let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode (CompressedArray.ofArray subNodes) (shift + PartitionSize)
                            match childNodeOpt with
                            | ValueSome(childNode) -> struct (nodes |> CompressedArray.set index childNode, didWeRemove)
                            | ValueNone -> struct (nodes |> CompressedArray.unset index, didWeRemove)
                        | EntryNode entry ->
                            if equals entry.Key k
                            then struct (nodes |> CompressedArray.unset index, true)
                            else struct (nodes, false) // Same hash but different key, don't remove.
                        | HashCollisionNode collisions ->
                            // TODO: This could be further optimised but hash collisions should be unlikely.
                            let newList = collisions |> List.filter (fun x -> equals x.Key k |> not)
                            let didWeRemove = collisions |> List.exists (fun x -> equals x.Key k)
                            match newList with
                            | [ entry ] -> struct (nodes |> CompressedArray.set index (EntryNode entry), didWeRemove) // Project parent node to hash collision and unset where this node was.
                            | _ :: _ -> struct (nodes |> CompressedArray.set index (HashCollisionNode(newList)), didWeRemove)
                            | [] -> failwithf "This should never happen; hash collision nodes should always have more than one entry"
                        | TrieNodeOne(nodeIndex, node) ->
                            let nodeAsCArray = CompressedArray.ofSingleElement nodeIndex node
                            let (struct (childNodeOpt, didWeRemove)) = traverseNodes node nodeAsCArray (shift + PartitionSize)
                            match childNodeOpt with
                            | ValueSome(childNode) -> struct (nodes |> CompressedArray.set index childNode, didWeRemove)
                            | ValueNone -> struct (CompressedArray.empty, didWeRemove)
                    if newSubNodeList |> CompressedArray.count = 0
                    then struct (ValueNone, didWeRemove)
                    else struct (ValueSome (createTrieNode (newSubNodeList)), didWeRemove)
                | ValueNone -> struct (ValueSome node, false)

            let rootIndex = getIndex keyHash 0
            // Need to start the chain. This is harder since we need knowledge of two levels at once (current + sublevel)
            // This is because deletions on a sub-level can mean we create a different node on the current level.
            let changeAndRemovalStatus =
                match hashMap.RootData with
                | TrieNode(nodes) -> traverseNodes hashMap.RootData nodes 0
                | TrieNodeFull(nodes) -> traverseNodes hashMap.RootData (CompressedArray.ofFullArrayAsTransient nodes) 0
                | TrieNodeOne(nodeIndex, node) ->
                    if nodeIndex = rootIndex
                    then
                        let subNodesEmulated = CompressedArray.ofSingleElement nodeIndex node
                        let (struct (childNodeOpt, didWeRemove)) = traverseNodes hashMap.RootData subNodesEmulated 0
                        match childNodeOpt with
                        | ValueSome(childNode) -> struct (ValueSome (TrieNodeOne(nodeIndex, childNode)), didWeRemove)
                        | ValueNone -> struct (ValueNone, didWeRemove)
                    else struct (ValueSome hashMap.RootData, false)
                | _ -> failwithf "Not expected for other node types to be at root position [RootNode: %A]" hashMap.RootData

            match changeAndRemovalStatus with
            | struct (ValueSome newRootNode, true) -> { CurrentCount = hashMap.CurrentCount - 1; RootData = newRootNode }
            | struct (ValueNone, true) -> { CurrentCount = 0; RootData = TrieNode(CompressedArray.empty) }
            | struct (_, false) -> hashMap // If no removal then no change required (unlike Add where replace could occur).

        removeInner k hashMap

    let public count hashMap = hashMap.CurrentCount

    let public toSeq hashMap =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode(nodes) -> for node in nodes.Content do yield! yieldNodes node
            | TrieNodeFull(nodes) -> for node in nodes do yield! yieldNodes node
            | TrieNodeOne(_, subNode) -> yield! yieldNodes subNode
            | EntryNode entry -> yield struct (entry.Key, entry.Value)
            | HashCollisionNode entries -> for entry in entries do yield struct (entry.Key, entry.Value)
        }

        seq { yield! yieldNodes hashMap.RootData }

    let isEmpty hashMap = hashMap.CurrentCount > 0

    type public StandardEqualityComparer =
        static member inline CheckEquality (x: 't, y: 't) = x.Equals(y)
        static member inline GetHashCode(obj: 'tk): int = obj.GetHashCode()

    let inline emptyWithComparer< ^tk, 'tv, ^eq when ^eq: (static member GetHashCode: ^tk -> int) and ^eq: (static member CheckEquality: ^tk * ^tk -> bool)> : HashMap< ^tk, 'tv, ^eq> =
        { CurrentCount = 0; RootData = TrieNode(CompressedArray.empty) }

    [<GeneralizableValue>] 
    let public empty< 'tk, 'tv when 'tk : equality> : HashMap< 'tk, 'tv, StandardEqualityComparer> =
        { CurrentCount = 0; RootData = TrieNode(CompressedArray.empty) }