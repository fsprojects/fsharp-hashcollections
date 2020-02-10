module FSharp.HashCollections.HashSet

let contains (k: 'tk) (hashMap: HashSet<'tk, 'teq>) : bool = 
    HashTrie.tryFind id id hashMap.EqualityComparer k hashMap.HashTrieRoot |> ValueOption.isSome

let add (k: 'tk) (hashMap: HashSet<'tk, 'teq>) =
    HashSet<_, _>(HashTrie.add id hashMap.EqualityComparer k hashMap.HashTrieRoot, hashMap.EqualityComparer)

let remove (k: 'tk) (hashMap: HashSet<'tk, 'teq>) = 
    HashSet<_, _>(HashTrie.remove id hashMap.EqualityComparer k hashMap.HashTrieRoot, hashMap.EqualityComparer)

let count (h: HashSet<_, _>) = HashTrie.count h.HashTrieRoot

let emptyWithComparer<'tk, 'teq when 'teq :> System.Collections.Generic.IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashSet<'tk, 'teq> = 
    let eqTemplate = new 'teq()
    HashSet<_, _>(HashTrie.empty, eqTemplate)

let empty<'tk when 'tk : equality> : HashSet<'tk> = 
    HashSet<'tk>(HashTrie.empty, HashIdentity.Structural)

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