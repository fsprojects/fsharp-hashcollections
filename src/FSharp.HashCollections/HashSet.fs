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