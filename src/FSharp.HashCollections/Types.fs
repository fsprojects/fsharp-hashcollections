namespace FSharp.HashCollections

open System.Collections.Generic

type internal TypeLookup<'tk, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new: unit -> 'teq)>() = 
    static member val EqualityComparer = new 'teq() with get

type [<System.Runtime.CompilerServices.IsReadOnly; Struct>] HashMapEntry<'tk, 'tv> = { Key: 'tk; Value: 'tv }

type HashTrieNode<'tk, 'tv> =
    | TrieNodeFull of nodes: HashTrieNode<'tk, 'tv> array
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk, 'tv>>
    | TrieNodeOne of index: uint64 * node: HashTrieNode<'tk, 'tv>
    | EntryNode of entry: HashMapEntry<'tk, 'tv>
    | HashCollisionNode of entries: HashMapEntry<'tk, 'tv> list

type [<Struct; System.Runtime.CompilerServices.IsReadOnly>] HashMap<'tk, 'tv, 'teq> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tk, 'tv>
}