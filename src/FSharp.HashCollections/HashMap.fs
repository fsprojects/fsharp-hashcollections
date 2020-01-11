module FSharp.HashCollections.HashMap
open System.Collections.Generic

let inline internal keyExtractor hme = hme.Key
let inline internal valueExtractor hme = hme.Value

let tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption = 
    HashTrie.tryFind keyExtractor valueExtractor k hashMap

let add (k: 'tk) (v: 'tv) (hashMap: HashMap<'tk, 'tv, 'teq>) =
    HashTrie.add keyExtractor { Key = k; Value = v } hashMap

let remove (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) = HashTrie.remove keyExtractor k hashMap

let count h = HashTrie.count h

let emptyWithComparer<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : struct and 'teq : (new : unit -> 'teq)> : HashMap<'tk, 'tv, 'teq> = HashTrie.emptyWithComparer<HashMapEntry<'tk, 'tv>, 'teq>

let empty<'tk, 'tv when 'tk :> System.IEquatable<'tk> and 'tk : equality> : HashMap<'tk, 'tv, StandardEqualityTemplate<'tk>> = HashTrie.emptyWithComparer<HashMapEntry<'tk, 'tv>, StandardEqualityTemplate<'tk>>

let toSeq (h: HashMap<'tk, 'tv, _>) : (struct ('tk * 'tv) seq) = 
    seq {
        for i in h |> HashTrie.toSeq do
            yield struct (i.Key, i.Value)
    }