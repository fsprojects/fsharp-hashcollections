namespace HashTrie.FSharp

open System

type [<System.Runtime.CompilerServices.IsReadOnly; Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list
    | TrieNode of nodes: Compressed32PosArray<HashTrieNode<'tk, 'tv>>

type [<Struct; System.Runtime.CompilerServices.IsReadOnly>] HashTrie<'tk, 'tv> = {
    CurrentCount: int32
    RootData: Compressed32PosArray<HashTrieNode<'tk, 'tv>>
}

module HashTrie =

    let private tryFindValueInList k l =
        let rec findInList currentList =
            match currentList with
            | [] -> ValueNone
            | entry :: tail -> if entry.Key = k then ValueSome entry.Value else findInList tail
        findInList l

    let [<Literal>] PartitionSize = 4
    let [<Literal>] PartitionMask = 0b1111u
    let [<Literal>] MaxShiftValue = 32 // Partition Size amount of 1 bits

    let inline private getIndex keyHash shift = keyHash >>> shift &&& PartitionMask
    let inline private getIndexNoShift keyHash = keyHash &&& PartitionMask

    let tryFind k (hashTrie: HashTrie<_, _>) = 
        let keyHash = hash k |> uint32
        
        let rec getRec node shift =
            match node with
            | TrieNode(nodes) ->
                let bitPos = Compressed32PosArray.getBitMapForIndex (getIndex keyHash shift)
                if Compressed32PosArray.boundsCheckIfSetForBitMapIndex nodes.BitMap bitPos // This checks if the bit was set in the first place.
                then
                    getRec
                        (nodes.Content.[Compressed32PosArray.getCompressedIndexForIndexBitmap nodes.BitMap bitPos]) 
                        (shift + PartitionSize)
                else ValueNone
            | EntryNode entry -> if entry.Key = k then ValueSome entry.Value else ValueNone
            | HashCollisionNode entries -> tryFindValueInList k entries
        
        match Compressed32PosArray.get (getIndexNoShift keyHash) hashTrie.RootData with
        | ValueSome(node) -> getRec node PartitionSize
        | ValueNone -> ValueNone

    let private addNodesToResolveConflict existingEntry newEntry existingEntryHash currentKeyHash shift =
        let rec createRequiredDepthNodes shift =
            let existingEntryIndex = getIndex existingEntryHash shift
            let currentEntryIndex = getIndex currentKeyHash shift
            if shift = MaxShiftValue
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
                    TrieNode(Compressed32PosArray.empty |> Compressed32PosArray.set existingEntryIndex subNode)
        createRequiredDepthNodes shift

    let add k v hashTrie =
        let keyHash = hash k |> uint32
        let newEntry = { Key = k; Value = v }
        let rec traverseNodes node shift =
            match node with
            | TrieNode(nodes) ->
                let index = getIndex keyHash shift
                match nodes |> Compressed32PosArray.get index with
                | ValueSome(nodeAtPos) ->
                    let struct (newPosNode, isAdded) = traverseNodes nodeAtPos (shift + PartitionSize)
                    struct (TrieNode(nodes |> Compressed32PosArray.set index newPosNode), isAdded)
                | ValueNone -> struct (TrieNode(nodes |> Compressed32PosArray.set index (EntryNode newEntry)), true)
            | EntryNode entry ->
                // At this point we may need to split the nodes or replace an existing one or make a hash collision node if we run out of hash partitions.
                if entry.Key = k
                then struct (EntryNode newEntry, false) // Replacement
                else struct (addNodesToResolveConflict entry newEntry (hash entry.Key |> uint32) keyHash shift, true)
            | HashCollisionNode entries ->
                /// This should only occur IF as above we are at the maximum point of shift (shift = MaxShiftValue)
                if entries |> List.exists (fun x -> x.Key = k)
                then struct (HashCollisionNode(entries |> List.map (fun x -> if x.Key = k then newEntry else x)), false)
                else struct (HashCollisionNode(newEntry :: entries), true)
        
        let index = getIndexNoShift keyHash

        let struct (newRootData, isAdded) = 
            match Compressed32PosArray.get index hashTrie.RootData with
            | ValueSome(node) -> 
                let struct (newNode, isAdded) = traverseNodes node PartitionSize
                struct (
                    hashTrie.RootData |> Compressed32PosArray.set index newNode,
                    isAdded)
            | ValueNone -> struct (hashTrie.RootData |> Compressed32PosArray.set index (EntryNode newEntry), true)

        { CurrentCount = if isAdded then hashTrie.CurrentCount + 1 else hashTrie.CurrentCount
          RootData = newRootData }

    let remove k hashTrie =
        let keyHash = hash k |> uint32
        let rec traverseNodes node nodes shift =
            let index = getIndex keyHash shift |> int
            match nodes |> Compressed32PosArray.get index with
            | ValueSome(subNode) ->
                let struct (newSubNodeList, didWeRemove) = 
                    match subNode with
                    | TrieNode subNodes -> 
                        let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode subNodes (shift + PartitionSize)
                        match childNodeOpt with
                        | ValueSome(childNode) -> struct (nodes |> Compressed32PosArray.set index childNode, didWeRemove)
                        | ValueNone -> struct (nodes |> Compressed32PosArray.unset index, didWeRemove)  
                    | EntryNode entry -> 
                        if entry.Key = k 
                        then struct (nodes |> Compressed32PosArray.unset index, true)
                        else struct (nodes, false) // Same hash but different key, don't remove.
                    | HashCollisionNode collisions ->
                        // TODO: This could be further optimised but hash collisions should be unlikely.
                        let newList = collisions |> List.filter (fun x -> x.Key <> k)
                        let didWeRemove = collisions |> List.exists (fun x -> x.Key = k)
                        match newList with
                        | [ entry ] -> struct (nodes |> Compressed32PosArray.set index (EntryNode entry), didWeRemove) // Project parent node to hash collision and unset where this node was.
                        | _ :: _ -> struct (nodes |> Compressed32PosArray.set index (HashCollisionNode(newList)), didWeRemove)
                        | [] -> failwithf "This should never happen; hash collision nodes should always have more than one entry"
                if newSubNodeList |> Compressed32PosArray.count = 0
                then struct (ValueNone, didWeRemove)
                else struct (ValueSome (TrieNode(newSubNodeList)), didWeRemove)
            | ValueNone -> struct (ValueSome node, false) 
        
        let rootIndex = getIndex keyHash 0 |> int
        match Compressed32PosArray.get rootIndex hashTrie.RootData with
        | ValueSome(node) -> 
            match node with
            | TrieNode(nodes) -> 
                let struct (newNode, isRemoved) = traverseNodes node nodes PartitionSize
                if isRemoved
                then
                    match newNode with
                    | ValueSome(newNode) -> 
                        let newRootData = hashTrie.RootData |> Compressed32PosArray.set rootIndex newNode
                        { CurrentCount = hashTrie.CurrentCount - 1; RootData = newRootData }
                    | ValueNone -> { CurrentCount = hashTrie.CurrentCount - 1; RootData = hashTrie.RootData |> Compressed32PosArray.unset rootIndex }
                else hashTrie
            | EntryNode entry -> 
                if entry.Key = k 
                then 
                    { CurrentCount = hashTrie.CurrentCount - 1
                      RootData = hashTrie.RootData |> Compressed32PosArray.unset rootIndex }
                else hashTrie
            | HashCollisionNode _ -> failwith "Not expected at root position"
        | _ -> hashTrie // There's no data - remove is a no-op          

    let public count hashTrie = hashTrie.CurrentCount

    let public toSeq hashTrie =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode(nodes) -> for node in nodes.Content do yield! yieldNodes node
            | EntryNode entry -> yield struct (entry.Key, entry.Value)
            | HashCollisionNode entries -> for entry in entries do yield struct (entry.Key, entry.Value)
        }

        seq { for node in hashTrie.RootData.Content do yield! yieldNodes node }

    let [<GeneralizableValue>] public empty<'tk, 'tv> : HashTrie<'tk, 'tv> = { CurrentCount = 0; RootData = Compressed32PosArray.empty }