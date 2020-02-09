module FSharp.HashCollections.HashMap
open System.Collections.Generic

let inline internal keyExtractor (hme: KeyValuePair<_, _>) = hme.Key
let inline internal valueExtractor (hme: KeyValuePair<_, _>) = hme.Value

let tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption = 
    HashTrie.tryFind keyExtractor valueExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot

let add (k: 'tk) (v: 'tv) (hashMap: HashMap<'tk, 'tv, 'teq>) =
    HashMap<_, _, _>(
        HashTrie.add keyExtractor hashMap.EqualityComparer (KeyValuePair<_, _>(k, v)) hashMap.HashTrieRoot,
        hashMap.EqualityComparer)

let remove (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) = 
    HashMap<_, _, _>(
        HashTrie.remove keyExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot,
        hashMap.EqualityComparer)

let count (h: HashMap<_, _, _>) = HashTrie.count h.HashTrieRoot

let emptyWithComparer<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashMap<'tk, 'tv, 'teq> = 
    HashMap<_, _, _>(HashTrie.empty, new 'teq())

let empty<'tk, 'tv when 'tk : equality> : HashMap<'tk, 'tv> = 
    HashMap<_, _>(HashTrie.empty, HashIdentity.Structural)

let toSeq (h: HashMap<'tk, 'tv, _>) : (struct ('tk * 'tv) seq) = 
    seq {
        for i in h.HashTrieRoot |> HashTrie.toSeq do
            yield struct (i.Key, i.Value)
    }

let isEmpty (h: HashMap<_, _, _>) = h.HashTrieRoot |> HashTrie.isEmpty

let ofSeq (s: #seq<KeyValuePair<'k, 'v>>) : HashMap<'k, 'v> = 
    let eqComparer = HashIdentity.Structural
    HashMap<_, _>(
        HashTrie.ofSeq keyExtractor eqComparer s empty.HashTrieRoot,
        eqComparer)