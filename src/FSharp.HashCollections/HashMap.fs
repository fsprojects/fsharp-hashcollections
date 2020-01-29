module FSharp.HashCollections.HashMap
open System.Collections.Generic

let inline internal keyExtractor (hme: KeyValuePair<_, _>) = hme.Key
let inline internal valueExtractor (hme: KeyValuePair<_, _>) = hme.Value

let tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption = 
    HashTrie.tryFind keyExtractor valueExtractor k hashMap.HashTrieRoot

let add (k: 'tk) (v: 'tv) (hashMap: HashMap<'tk, 'tv, 'teq>) =
    HashTrie.add keyExtractor (KeyValuePair<_, _>(k, v)) hashMap.HashTrieRoot |> HashMap

let remove (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) = 
    HashTrie.removeAll keyExtractor [k] hashMap.HashTrieRoot |> HashMap

let count (h: HashMap<_, _, _>) = HashTrie.count h.HashTrieRoot

let emptyWithComparer<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashMap<'tk, 'tv, 'teq> = HashTrie.emptyWithComparer<KeyValuePair<'tk, 'tv>, 'teq> |> HashMap

let empty<'tk, 'tv when 'tk :> System.IEquatable<'tk> and 'tk : equality> : HashMap<'tk, 'tv, StandardEqualityTemplate<'tk>> = HashTrie.emptyWithComparer<KeyValuePair<'tk, 'tv>, StandardEqualityTemplate<'tk>> |> HashMap

let toSeq (h: HashMap<'tk, 'tv, _>) : (struct ('tk * 'tv) seq) = 
    seq {
        for i in h.HashTrieRoot |> HashTrie.toSeq do
            yield struct (i.Key, i.Value)
    }

let isEmpty (h: HashMap<_, _, _>) = h.HashTrieRoot |> HashTrie.isEmpty

let ofSeq (s: #seq<KeyValuePair<'k, 'v>>) : HashMap<'k, 'v, StandardEqualityTemplate<'k>> = 
    HashTrie.ofSeq keyExtractor s empty.HashTrieRoot |> HashMap