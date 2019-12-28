namespace HashTrie.FSharp

open System
open System.Collections.Generic

type [<System.Runtime.CompilerServices.IsReadOnly; Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | TrieNodeFull of nodes: HashTrieNode<'tk, 'tv> array
    | TrieNode of nodes: Compressed32PosArray<HashTrieNode<'tk, 'tv>>
    | TrieNodeOne of index: uint64 * node: HashTrieNode<'tk, 'tv>
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list

type [<Struct; System.Runtime.CompilerServices.IsReadOnly>] HashTrie<'tk, 'tv, 'teq> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk, 'tv>
}

module HashTrie =
    
    let inline hash< ^eq, ^t when ^eq: (static member GetHashCode: ^t -> int)> (o: ^t) =
        ( ^eq : (static member GetHashCode: ^t -> int) (o))

    let inline equals< ^eq, ^t when ^eq: (static member CheckEquality: ^t * ^t -> bool)> (o1: ^t) (o2: ^t) =
        ( ^eq : (static member CheckEquality: ^t * ^t -> bool) (o1, o2))
    
    // let inline private hash o = o.GetHashCode() // Performance significantly faster for Get operations than "hash" F# func.
    // let inline private (==) o1 o2 = o1.Equals(o2) // Shaves 30us of 10,000,000 factor test

    let tryFindValueInList equals k l =
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

    let inline tryFind k (hashTrie: HashTrie< ^tk, ^tv, ^teq>) : ^tv voption = 

        let inline equals x y = equals< ^teq, ^tk> x y

        let rec getRec node remainderHash =
            //printfn "Rec: Hash: %A, Node: %A" remainderHash node
            match node with
            | TrieNodeFull(nodes) ->
                let index = getIndexNoShift remainderHash |> int
                getRec nodes.[index] (remainderHash >>> PartitionSize)
            | TrieNode(nodes) ->
                let bitPos = Compressed32PosArray.getBitMapForIndex (getIndexNoShift remainderHash)
                //printfn "BitMapOfArray: %i; BitPos: %i" nodes.BitMap bitPos
                if Compressed32PosArray.boundsCheckIfSetForBitMapIndex nodes.BitMap bitPos // This checks if the bit was set in the first place.
                then
                    getRec
                        (nodes.Content.[Compressed32PosArray.getCompressedIndexForIndexBitmap nodes.BitMap bitPos]) 
                        (remainderHash >>> PartitionSize)
                else ValueNone
            | TrieNodeOne(nodeIndex, node) -> 
                let index = getIndexNoShift remainderHash
                if nodeIndex.Equals(index)
                then getRec node (remainderHash >>> PartitionSize) 
                else ValueNone
            | EntryNode entry -> if equals entry.Key k then ValueSome entry.Value else ValueNone
            | HashCollisionNode entries -> tryFindValueInList equals k entries
        
        let keyHash = hash< ^teq, _> k |> uint64
        getRec hashTrie.RootData (keyHash)
        // let keyHash = hash k |> uint64
        // match Compressed32PosArray.get (getIndexNoShift keyHash) hashTrie.RootData with
        // | ValueSome(node) -> getRec node (keyHash >>> PartitionSize)
        // | ValueNone -> 
        //     //printfn "Returning at first level"
        //     ValueNone

    let inline createTrieNode (nodes: Compressed32PosArray<_>) = 
        match nodes.Content.Length with
        | Compressed32PosArray.MaxSize -> TrieNodeFull(nodes.Content)
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
                        Compressed32PosArray.empty 
                        |> Compressed32PosArray.set existingEntryIndex (EntryNode existingEntry) 
                        |> Compressed32PosArray.set currentEntryIndex (EntryNode newEntry))
                else
                    let subNode = createRequiredDepthNodes (shift + PartitionSize)
                    TrieNodeOne(existingEntryIndex, subNode)
        createRequiredDepthNodes shift

    let inline add k v (hashTrie: HashTrie< ^tk, ^tv, ^eq>) : HashTrie< ^tk, ^tv, ^eq> =
        let inline equals x y = equals< ^eq, ^tk> x y
        let inline hash o = hash< ^eq, ^tk> o
        
        let keyHash = hash k |> uint64
        let newEntry = { Key = k; Value = v }
        let rec traverseNodes node shift =
            match node with
            | TrieNode(nodes) ->
                let index = getIndex keyHash shift
                match nodes |> Compressed32PosArray.get index with
                | ValueSome(nodeAtPos) ->
                    let struct (newPosNode, isAdded) = traverseNodes nodeAtPos (shift + PartitionSize)
                    let newNodes = nodes |> Compressed32PosArray.set index newPosNode
                    struct (TrieNode newNodes, isAdded)
                | ValueNone -> 
                    if nodes.Content.Length = Compressed32PosArray.MaxSize - 1
                    then
                        let uncompressedArray = ArrayUtils.copyArrayInsertInMiddle (int index) (EntryNode newEntry) nodes.Content
                        struct (TrieNodeFull uncompressedArray, true)
                    else struct (TrieNode (nodes |> Compressed32PosArray.set index (EntryNode newEntry)), true)
            | TrieNodeOne(nodeIndex, nodeAtPos) ->
                let index = getIndex keyHash shift
                if nodeIndex.Equals(index)
                then 
                    let struct (newPosNode, isAdded) = traverseNodes nodeAtPos (shift + PartitionSize)
                    struct (TrieNodeOne(index, newPosNode), isAdded)
                else 
                    let entryNode = EntryNode(newEntry)
                    let ca = Compressed32PosArray.empty |> Compressed32PosArray.set nodeIndex nodeAtPos |> Compressed32PosArray.set index entryNode
                    struct (TrieNode(ca), true)
            | TrieNodeFull(nodes) ->
                let index = getIndex keyHash shift |> int
                let nodeAtPos = nodes.[index]
                let struct (newPosNode, isAdded) = traverseNodes nodeAtPos (shift + PartitionSize)
                let newNodes = Array.zeroCreate Compressed32PosArray.MaxSize
                Array.Copy(nodes, newNodes, Compressed32PosArray.MaxSize)
                newNodes.[index] <- newPosNode
                struct (TrieNodeFull newNodes, isAdded)
            | EntryNode entry ->
                // At this point we may need to split the nodes or replace an existing one or make a hash collision node if we run out of hash partitions.
                if equals entry.Key k
                then struct (EntryNode newEntry, false) // Replacement
                else 
                    // Suspect this logic fails and is wrong. We could be doing a lot of hash collision nodes by mistake masking the performance bug.
                    // Interesting this doesn't happen when everything is divisible by 32 exactly.
                    struct (addNodesToResolveConflict entry newEntry (hash entry.Key |> uint64) keyHash shift, true)
            | HashCollisionNode entries ->
                /// This should only occur IF as above we are at the maximum point of shift (shift = MaxShiftValue)
                if shift < MaxShiftValue then failwithf "Not expected to exist"
                if entries |> List.exists (fun x -> x.Key = k)
                then struct (HashCollisionNode(entries |> List.map (fun x -> if x.Key = k then newEntry else x)), false)
                else struct (HashCollisionNode(newEntry :: entries), true)
        
        let struct (newRootData, isAdded) = traverseNodes hashTrie.RootData 0

        { CurrentCount = if isAdded then hashTrie.CurrentCount + 1 else hashTrie.CurrentCount
          RootData = newRootData }

    // let remove k hashTrie =
    //     let keyHash = hash k |> uint64
    //     let rec traverseNodes node nodes shift =
    //         let index = getIndex keyHash shift |> int
    //         match nodes |> Compressed32PosArray.get index with
    //         | ValueSome(subNode) ->
    //             let struct (newSubNodeList, didWeRemove) = 
    //                 match subNode with
    //                 | TrieNode subNodes -> 
    //                     let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode subNodes (shift + PartitionSize)
    //                     match childNodeOpt with
    //                     | ValueSome(childNode) -> struct (nodes |> Compressed32PosArray.set index childNode, didWeRemove)
    //                     | ValueNone -> struct (nodes |> Compressed32PosArray.unset index, didWeRemove)  
    //                 | TrieNodeFull(subNodes) -> 
    //                     let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode (Compressed32PosArray.ofArray subNodes) (shift + PartitionSize)
    //                     match childNodeOpt with
    //                     | ValueSome(childNode) -> struct (nodes |> Compressed32PosArray.set index childNode, didWeRemove)
    //                     | ValueNone -> struct (nodes |> Compressed32PosArray.unset index, didWeRemove)  
    //                 | EntryNode entry -> 
    //                     if entry.Key = k 
    //                     then struct (nodes |> Compressed32PosArray.unset index, true)
    //                     else struct (nodes, false) // Same hash but different key, don't remove.
    //                 | HashCollisionNode collisions ->
    //                     // TODO: This could be further optimised but hash collisions should be unlikely.
    //                     let newList = collisions |> List.filter (fun x -> x.Key <> k)
    //                     let didWeRemove = collisions |> List.exists (fun x -> x.Key = k)
    //                     match newList with
    //                     | [ entry ] -> struct (nodes |> Compressed32PosArray.set index (EntryNode entry), didWeRemove) // Project parent node to hash collision and unset where this node was.
    //                     | _ :: _ -> struct (nodes |> Compressed32PosArray.set index (HashCollisionNode(newList)), didWeRemove)
    //                     | [] -> failwithf "This should never happen; hash collision nodes should always have more than one entry"
    //             if newSubNodeList |> Compressed32PosArray.count = 0
    //             then struct (ValueNone, didWeRemove)
    //             else struct (ValueSome (createTrieNode (newSubNodeList)), didWeRemove)
    //         | ValueNone -> struct (ValueSome node, false) 
        
    //     let rootIndex = getIndex keyHash 0 |> int
    //     match hashTrie.RootData with
        // | TrieNode(nodes) ->  
        // match Compressed32PosArray.get rootIndex hashTrie.RootData with
        // | ValueSome(node) -> 
        //     match node with
        //     | TrieNode(nodes) -> 
        //         let struct (newNode, isRemoved) = traverseNodes node nodes PartitionSize
        //         if isRemoved
        //         then
        //             match newNode with
        //             | ValueSome(newNode) -> 
        //                 let newRootData = hashTrie.RootData |> Compressed32PosArray.set rootIndex newNode
        //                 { CurrentCount = hashTrie.CurrentCount - 1; RootData = newRootData }
        //             | ValueNone -> { CurrentCount = hashTrie.CurrentCount - 1; RootData = hashTrie.RootData |> Compressed32PosArray.unset rootIndex }
        //         else hashTrie
        //     | TrieNodeFull nodes -> 
        //         let compressedArrayEq = Compressed32PosArray.ofArray nodes
        //         let struct (newNode, isRemoved) = traverseNodes node compressedArrayEq PartitionSize
        //         if isRemoved
        //         then
        //             match newNode with
        //             | ValueSome(newNode) -> 
        //                 let newRootData = hashTrie.RootData |> Compressed32PosArray.set rootIndex newNode
        //                 { CurrentCount = hashTrie.CurrentCount - 1; RootData = newRootData }
        //             | ValueNone -> { CurrentCount = hashTrie.CurrentCount - 1; RootData = hashTrie.RootData |> Compressed32PosArray.unset rootIndex }
        //         else hashTrie
        //     | EntryNode entry -> 
        //         if entry.Key = k 
        //         then 
        //             { CurrentCount = hashTrie.CurrentCount - 1
        //               RootData = hashTrie.RootData |> Compressed32PosArray.unset rootIndex }
        //         else hashTrie
        //     | HashCollisionNode _ -> failwith "Not expected at root position"
        // | _ -> hashTrie // There's no data - remove is a no-op          

    let public count hashTrie = hashTrie.CurrentCount

    let public toSeq hashTrie =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode(nodes) -> for node in nodes.Content do yield! yieldNodes node
            | TrieNodeFull(nodes) -> for node in nodes do yield! yieldNodes node
            | TrieNodeOne(_, subNode) -> yield! yieldNodes subNode
            | EntryNode entry -> yield struct (entry.Key, entry.Value)
            | HashCollisionNode entries -> for entry in entries do yield struct (entry.Key, entry.Value)
        }

        seq { yield! yieldNodes hashTrie.RootData }

    type public StandardEqualityComparer = 
        static member inline CheckEquality (x: 't, y: 't) = x.Equals(y)
        static member inline GetHashCode(obj: 'tk): int = obj.GetHashCode()
    
    let [<GeneralizableValue>] public empty<'tk, 'tv when 'tk : equality> : HashTrie<'tk, 'tv, StandardEqualityComparer> = 
        { CurrentCount = 0; 
          RootData = TrieNode(Compressed32PosArray.empty) }

    let [<GeneralizableValue>] public emptyWithComparer =
        { CurrentCount = 0; 
          RootData = TrieNode(Compressed32PosArray.empty) }