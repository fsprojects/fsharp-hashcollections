namespace HashTrie.FSharp

open System

type [<Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list
    | TrieNode of nodes: Compressed32PosArray<HashTrieNode<'tk, 'tv>>

type [<Struct>] HashTrie<'tk, 'tv> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk, 'tv> voption
}

module HashTrie =

    let private tryFindValueInList k l =
        let rec findInList currentList =
            match currentList with
            | [] -> ValueNone
            | entry :: tail -> if entry.Key = k then ValueSome entry.Value else findInList tail
        findInList l

    let [<Literal>] PartitionSize = 5
    let [<Literal>] PartitionMask = 0b11111u
    let [<Literal>] MaxShiftValue = 30 // Partition Size amount of 1 bits

    let inline private getIndex (keyHash: uint32) shift = keyHash >>> shift &&& PartitionMask

    let inline private getFromNode k hashTrieNode =
        let keyHash = hash k |> uint32
        let rec traverseNodes k keyHash node shift =
            match node with
            | TrieNode(nodes) ->
                let index = getIndex keyHash shift
                // NOTE: Compressed32PosArray.get is inlined to avoid struct copying. Delivers a significant performance benefit in Get operations.
                match nodes |> Compressed32PosArray.get (int index) with
                | ValueSome(node) -> traverseNodes k keyHash node (shift + PartitionSize)
                | ValueNone -> ValueNone
            | EntryNode entry -> if entry.Key = k then ValueSome entry.Value else ValueNone
            | HashCollisionNode entries -> tryFindValueInList k entries
        traverseNodes k keyHash hashTrieNode 0

    let get k hashTrie = 
        match hashTrie.RootData with
        | ValueSome(rootData) -> getFromNode k rootData
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
                    //let subNode = addNodesToResolveConflict existingEntry newEntry existingEntryHash currentKeyHash (shift + PartitionSize)
                    TrieNode(Compressed32PosArray.empty |> Compressed32PosArray.set (int existingEntryIndex) (EntryNode existingEntry) |> Compressed32PosArray.set (int currentEntryIndex) (EntryNode newEntry))
                else
                    // We're still colliding but haven't reached max depth yet. Attempt next partition.
                    let subNode = createRequiredDepthNodes (shift + PartitionSize)
                    TrieNode(Compressed32PosArray.empty |> Compressed32PosArray.set (int existingEntryIndex) subNode)
        createRequiredDepthNodes shift            

    let inline private addFromNode k v hashTrieNode =
        let keyHash = hash k |> uint32
        let newEntry = { Key = k; Value = v; }
        let rec traverseNodes node shift =
            match node with
            | TrieNode(nodes) ->
                let index = getIndex keyHash shift |> int
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
        traverseNodes hashTrieNode 0

    let add k v hashTrie =
        let rootData = hashTrie.RootData |> ValueOption.defaultWith (fun () -> TrieNode(Compressed32PosArray.empty))
        let struct (newNode, isAdded) = addFromNode k v rootData
        { CurrentCount = if isAdded then hashTrie.CurrentCount + 1 else hashTrie.CurrentCount
          RootData = ValueSome newNode }

    // NOTE: This needs to change so that nodes are cleaned up (i.e. if TrieNode is empty then we don't need it anymore and it should be unset upstream)
    let private removeFromNode k hashTrieNode =
        let keyHash = hash k |> uint32
        let rec traverseNodes node shift =
            let (TrieNode(nodes)) = node // Removal always deals with TrieNodes and ONE level below.
            let index = getIndex keyHash shift |> int
            match nodes |> Compressed32PosArray.get index with
            | ValueSome(subNode) ->
                let struct (newSubNodeList, didWeRemove) = 
                    match subNode with
                    | TrieNode _ -> 
                        let (struct (childNodeOpt, didWeRemove)) = traverseNodes subNode (shift + PartitionSize)
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
            | ValueNone -> struct (ValueSome node, false) // No point projecting new node; nothing to remove.
        traverseNodes hashTrieNode 0

    let remove k hashTrie =
        match hashTrie.RootData with
        | ValueSome(rootData) -> 
            let struct (newNode, isRemoved) = removeFromNode k rootData
            { CurrentCount = if isRemoved then hashTrie.CurrentCount - 1 else hashTrie.CurrentCount
              RootData = newNode }
        | ValueNone -> hashTrie // There's no data - remove is a no-op          

    let public count hashTrie = hashTrie.CurrentCount

    let public toSeq hashTrie =
        let rec yieldNodes node = seq {
            match node with
            | TrieNode(nodes) -> for node in nodes.Content do yield! yieldNodes node
            | EntryNode entry -> yield struct (entry.Key, entry.Value)
            | HashCollisionNode entries -> for entry in entries do yield struct (entry.Key, entry.Value)
        }
        match hashTrie.RootData with
        | ValueSome(rootData) -> yieldNodes rootData
        | ValueNone -> Seq.empty

    let [<GeneralizableValue>] public empty<'tk, 'tv> : HashTrie<'tk, 'tv> = { CurrentCount = 0; RootData = ValueNone }