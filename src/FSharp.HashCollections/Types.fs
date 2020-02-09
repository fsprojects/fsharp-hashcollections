namespace FSharp.HashCollections

open System.Collections.Generic
open System.Runtime.CompilerServices

type internal HashTrieNode<'tk> =
    | TrieNodeFull of nodes: HashTrieNode<'tk> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk>>
    | TrieNodeOne of index: int * node: HashTrieNode<'tk>
    | EntryNode of entry: 'tk
    | HashCollisionNode of entries: 'tk list

type [<Struct>] internal HashTrieRoot<'tnode> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tnode>
}

/// Immutable hash map.
type [<Struct; IsReadOnly>] HashMap<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk>> = 
    val internal HashTrieRoot: HashTrieRoot<KeyValuePair<'tk, 'tv>>
    val internal EqualityComparer: 'teq
    internal new(d, eq) = { HashTrieRoot = d; EqualityComparer = eq }

type HashMap<'tk, 'tv> = HashMap<'tk, 'tv, IEqualityComparer<'tk>>

/// Immutable hash set.
type [<Struct; IsReadOnly>] HashSet<'tk, 'teq when 'teq :> IEqualityComparer<'tk>> = 
    val internal HashTrieRoot: HashTrieRoot<'tk>
    val internal EqualityComparer: 'teq
    internal new(d, eq) = { HashTrieRoot = d; EqualityComparer = eq }

type HashSet<'tk> = HashSet<'tk, IEqualityComparer<'tk>>