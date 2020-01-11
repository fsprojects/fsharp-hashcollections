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

type HashTrieNode<'tk> =
    | TrieNodeFull of nodes: HashTrieNode<'tk> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk>>
    | TrieNodeOne of index: int * node: HashTrieNode<'tk>
    | EntryNode of entry: 'tk
    | HashCollisionNode of entries: 'tk list

type [<Struct; IsReadOnly>] HashSet<'tk, 'teq> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk>
}

/// Proxy equality template.
type HashMapEqualityTemplate<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : struct and 'teq : (new : unit -> 'teq)> = 
    struct
        [<DefaultValue(false)>] val Eq: 'teq
    end
    interface IEqualityComparer<HashMapEntry<'tk, 'tv>> with
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member this.Equals(x: HashMapEntry<'tk, 'tv>, y: HashMapEntry<'tk, 'tv>): bool = 
            this.Eq.Equals(x.Key, y.Key)
        [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
        member this.GetHashCode(obj: HashMapEntry<'tk, 'tv>): int = 
            this.Eq.GetHashCode(obj.Key)

/// Immutable hash map.
type HashMap<'tk, 'tv, 'teq> = HashSet<HashMapEntry<'tk, 'tv>, 'teq>