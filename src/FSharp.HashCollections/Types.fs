namespace FSharp.HashCollections

open System.Collections.Generic
open System
open System.Runtime.CompilerServices

module internal EqualityTemplateLookup = 
    let inline eqComparer<'tk, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new: unit -> 'teq)> = 
        new 'teq()

type StandardEqualityTemplate<'tk when 'tk :> IEquatable<'tk> and 'tk : equality> =
    // This is a struct for the following reasons:
    // 1. Low cost/no cost allocation meaning we don't need to store this somewhere and can create one each time.
    // Static field access is MUCH slower.
    // 2. Calls to struct can be inlined by the JIT and use the "call" instruction under the hood since there's no virtual dispatch.
    struct
    end
    //inherit EqualityTemplate<'tk>()
    interface IEqualityComparer<'tk> with
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member __.Equals(o1, o2) = o1.Equals(o2)
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member __.GetHashCode(o) = o.GetHashCode()

type [<IsReadOnly; Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | TrieNodeFull of nodes: HashTrieNode<'tk, 'tv> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk, 'tv>>
    | TrieNodeOne of index: int * node: HashTrieNode<'tk, 'tv>
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list

type [<Struct; IsReadOnly>] HashMap<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk>> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk, 'tv>
}