namespace FSharp.HashCollections

open System
open System.Collections.Generic

/// Underlying Hash Trie implementation for other collections.
module internal HashTrie =

    let [<Literal>] PartitionSize = 6
    let [<Literal>] PartitionMask = 0b111111
    let [<Literal>] MaxShiftValue = 32 // Partition Size amount of 1 bits

    let inline getIndexNoShift shiftedHash = shiftedHash &&& PartitionMask |> int
    let inline getIndex keyHash shift = getIndexNoShift (keyHash >>> shift)

    let inline tryFind (keyExtractor: 'tknode -> 'tk) (valueExtractor: 'tknode -> 'tv) (k: 'tk) (hashMap: HashTrieRoot<'tknode, 'teq>) : 'tv voption =
        let eqTemplate = EqualityTemplateLookup.eqComparer<'tk, 'teq>
        
        let inline tryFindValueInList (l : _ list) =
            let rec findInList currentList =
                match currentList with
                | entry :: tail -> 
                    let extractedKey = keyExtractor entry
                    if eqTemplate.Equals(extractedKey, k) 
                    then ValueSome (valueExtractor entry) 
                    else findInList tail
                | [] -> ValueNone
            findInList l
        
        let rec getRec node remainderHash =
            //printfn "Rec: Hash: %A, Node: %A" remainderHash node
            match node with
            | TrieNodeFull(nodes) ->
                let index = getIndexNoShift remainderHash
                getRec nodes.[index] (remainderHash >>> PartitionSize)
            | TrieNode(nodes) ->
                let bitPos = CompressedArray.getBitMapForIndex (getIndexNoShift remainderHash)
                if CompressedArray.boundsCheckIfSetForBitMapIndex nodes.BitMap bitPos // This checks if the bit was set in the first place.
                then
                    getRec
                        (nodes.Content.[CompressedArray.getCompressedIndexForIndexBitmap nodes.BitMap bitPos])
                        (remainderHash >>> PartitionSize)
                else 
                    ValueNone
            | TrieNodeOne(nodeIndex, node) ->
                let index = getIndexNoShift remainderHash
                if nodeIndex = index
                then getRec node (remainderHash >>> PartitionSize)
                else ValueNone
            | EntryNode entry -> 
                let extractedKey = keyExtractor entry
                if eqTemplate.Equals(extractedKey, k) then ValueSome (valueExtractor entry) else ValueNone
            | HashCollisionNode entries -> tryFindValueInList entries

        let keyHash = eqTemplate.GetHashCode(k)
        getRec hashMap.RootData keyHash

    let inline createTrieNode (nodes: CompressedArray<_>) =
        match nodes.Content.Length with
        | CompressedArray.MaxSize -> TrieNodeFull(nodes.Content)
        | _ -> TrieNode(nodes)

    let inline add (keyExtractor: 'tknode -> 'tk) (knode: 'tknode) (hashMap: HashTrieRoot<'tknode, 'teq>) : HashTrieRoot<'tknode, 'teq> =
        let eqTemplate = EqualityTemplateLookup.eqComparer<'tk, 'teq>
        let inline equals x y = eqTemplate.Equals(x, y)
        let inline hash o = eqTemplate.GetHashCode(o)

        let key = keyExtractor knode
        let keyHash = hash key
        //let newEntry = { Key = k; Value = v }

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
                        let uncompressedArray = ArrayHelpers.copyArrayInsertInMiddle (int index) (EntryNode knode) nodes.Content
                        struct (TrieNodeFull uncompressedArray, true)
                    else struct (TrieNode (nodes |> CompressedArray.set index (EntryNode knode)), true)
            | TrieNodeOne(nodeIndex, nodeAtPos) ->
                let index = getIndex keyHash shift
                if nodeIndex.Equals(index)
                then
                    let struct (newPosNode, isAdded) = addRec nodeAtPos (shift + PartitionSize)
                    struct (TrieNodeOne(index, newPosNode), isAdded)
                else
                    let entryNode = EntryNode(knode)
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
                let extractedKey = keyExtractor entry
                if equals extractedKey key
                then struct (EntryNode knode, false) // Replacement
                else struct (addNodesToResolveConflict entry knode (hash extractedKey) keyHash shift, true) // Create one or more levels required.
            | HashCollisionNode entries ->
                if shift < MaxShiftValue then failwithf "Not expected to exist"
                let rec replaceElementIfExists previouslySeen tailList = 
                    match tailList with
                    | entryNode :: tail -> 
                        let extractedKey = keyExtractor entryNode
                        if equals extractedKey key 
                        then (List.append (knode :: previouslySeen) tail, true)
                        else replaceElementIfExists (entryNode :: previouslySeen) tail
                    | [] -> ([], false)

                let (newList, replaced) = replaceElementIfExists [] entries
                if replaced 
                then struct (HashCollisionNode(newList), false) 
                else struct (HashCollisionNode(knode :: entries), true)
               
        let struct (newRootData, isAdded) = addRec hashMap.RootData 0

        { CurrentCount = if isAdded then hashMap.CurrentCount + 1 else hashMap.CurrentCount
          RootData = newRootData }

    let inline remove keyExtractor (key: 'tk) (hashMap: HashTrieRoot< 'tknode, 'teq>) =
        let eqTemplate = EqualityTemplateLookup.eqComparer<'tk, 'teq>
        let inline equals (x: 'tk) (y: 'tk) = eqTemplate.Equals(x, y)
        let inline hash (o: 'tk) = eqTemplate.GetHashCode(o)

        let keyHash = hash key

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
                            if equals (keyExtractor entry) k
                            then struct (nodes |> CompressedArray.unset index, true)
                            else struct (nodes, false) // Same hash but different key, don't remove.
                        | HashCollisionNode collisions ->
                            // TODO: This could be further optimised but hash collisions should be unlikely.
                            let newList = collisions |> List.filter (fun x -> equals (keyExtractor x) k |> not)
                            let didWeRemove = collisions |> List.exists (fun x -> equals (keyExtractor x) k)
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
            | struct (ValueSome newRootNode, true) -> { CurrentCount = hashMap.CurrentCount - 1; RootData = newRootNode; }
            | struct (ValueNone, true) -> { CurrentCount = 0; RootData = TrieNode(CompressedArray.empty); }
            | struct (_, false) -> hashMap // If no removal then no change required (unlike Add where replace could occur).

        removeInner key hashMap

    let public count hashMap = hashMap.CurrentCount

    let public toSeq hashMap =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode(nodes) -> for node in nodes.Content do yield! yieldNodes node
            | TrieNodeFull(nodes) -> for node in nodes do yield! yieldNodes node
            | TrieNodeOne(_, subNode) -> yield! yieldNodes subNode
            | EntryNode entry -> yield entry
            | HashCollisionNode entries -> for entry in entries do yield entry
        }

        seq { yield! yieldNodes hashMap.RootData }

    let isEmpty hashMap = hashMap.CurrentCount > 0

    [<GeneralizableValue>] 
    let emptyWithComparer< 'tk, 'eq when 'eq : (new : unit -> 'eq)> : HashTrieRoot< ^tk, 'eq> =
        { CurrentCount = 0; RootData = TrieNode(CompressedArray.empty) }

    [<GeneralizableValue>] 
    let public empty<'tk when 'tk :> IEquatable<'tk> and 'tk : equality> : HashTrieRoot< 'tk, StandardEqualityTemplate<'tk>> =
        emptyWithComparer<'tk, StandardEqualityTemplate<'tk>>