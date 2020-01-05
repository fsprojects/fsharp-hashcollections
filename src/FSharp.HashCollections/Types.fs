namespace FSharp.HashCollections

open System.Collections.Generic
open System

// type IEqualityTemplate<'tk>() = 
//     abstract member Equals: o1: 'tk * o2: 'tk -> bool
//     abstract member GetHashCode: 'tk -> int32


module EqualityTemplateLookup = 
    let inline eqComparer<'tk, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new: unit -> 'teq)> = 
        new 'teq()

type StandardEqualityTemplate<'tk when 'tk :> IEquatable<'tk> and 'tk : equality>() =
    //inherit EqualityTemplate<'tk>()
    interface IEqualityComparer<'tk> with
        member __.Equals(o1, o2) = o1.Equals(o2)
        member __.GetHashCode(o) = o.GetHashCode()

type [<System.Runtime.CompilerServices.IsReadOnly; Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | TrieNodeFull of nodes: HashTrieNode<'tk, 'tv> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk, 'tv>>
    | TrieNodeOne of index: int * node: HashTrieNode<'tk, 'tv>
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list

type [<Struct; System.Runtime.CompilerServices.IsReadOnly>] HashMap<'tk, 'tv, 'teq when 'tk :> IEquatable<'tk> and 'teq :> IEqualityComparer<'tk>> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk, 'tv>
    EqualityTemplate: 'teq // Static field lookup is expensive performance wise so put it here.
}