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

//type [<IsReadOnly; Struct>] internal HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type internal HashTrieNode<'tk> =
    | TrieNodeFull of nodes: HashTrieNode<'tk> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk>>
    | TrieNodeOne of index: int * node: HashTrieNode<'tk>
    | EntryNode of entry: 'tk
    | HashCollisionNode of entries: 'tk list

type [<Struct; IsReadOnly>] internal HashTrieRoot<'tk, 'teq> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk>
}

/// Immutable hash map.
type [<Struct; IsReadOnly>] HashMap<'tk, 'tv, 'teq> = 
    val internal HashTrieRoot: HashTrieRoot<KeyValuePair<'tk, 'tv>, 'teq>
    internal new(d) = { HashTrieRoot = d }

/// Immutable hash set.
type [<Struct; IsReadOnly>] HashSet<'tk, 'teq> = 
    val internal HashTrieRoot: HashTrieRoot<'tk, 'teq>
    internal new(d) = { HashTrieRoot = d }