module FSharp.HashCollections.HashSet

let inline internal keyExtractor hme = hme.Key
let inline internal valueExtractor hme = hme.Value

let contains (k: 'tk) (hashMap: HashSet<'tk, 'teq>) : bool = 
    HashTrie.tryFind id id k hashMap.HashTrieRoot |> ValueOption.isSome

let add (k: 'tk) (hashMap: HashSet<'tk, 'teq>) =
    HashTrie.add id k hashMap.HashTrieRoot |> HashSet

let remove (k: 'tk) (hashMap: HashSet<'tk, 'teq>) = 
    HashTrie.remove id k hashMap.HashTrieRoot |> HashSet

let count (h: HashSet<_, _>) = HashTrie.count h.HashTrieRoot

let emptyWithComparer<'tk, 'teq when 'teq :> System.Collections.Generic.IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashSet<'tk, 'teq> = 
    HashTrie.emptyWithComparer<'tk, 'teq> |> HashSet

let empty<'tk when 'tk :> System.IEquatable<'tk> and 'tk : equality> : HashSet<'tk, StandardEqualityTemplate<'tk>> = HashTrie.emptyWithComparer<'tk, StandardEqualityTemplate<'tk>> |> HashSet

let toSeq (h: HashSet<'tk, _>) : 'tk seq = h.HashTrieRoot |> HashTrie.toSeq

let isEmpty (h: HashSet<_, _>) = h.HashTrieRoot |> HashTrie.isEmpty

let inline private getSmallerAndLargerSet h1 h2 = if count h1 > count h2 then struct (h2, h1) else struct (h1, h2)

let intersect (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) = 
    let struct (smallerSet, largerSet) = getSmallerAndLargerSet h1 h2
    let mutable r = emptyWithComparer<'t, 'teq>
    for item in smallerSet |> toSeq do
        if largerSet |> contains item
        then r <- r |> add item
    r

let fold folder state (h: HashSet<'t, 'teq>) = h |> toSeq |> Seq.fold folder state

let difference (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) = 
    h2 |> fold (fun s t -> s |> remove t) h1

let union (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) = 
    let struct (smallerSet, largerSet) = getSmallerAndLargerSet h1 h2
    smallerSet |> fold (fun s t -> s |> add t) largerSet

let filter predicate (h: HashSet<'t, 'teq>) = 
    h |> fold (fun s t -> if predicate t then s |> add t else s) (emptyWithComparer<'t, 'teq>)

let map mapping (h: HashSet<_, 'teq>) = 
    h |> fold (fun s t -> s |> add (mapping t)) (emptyWithComparer<_, 'teq>)