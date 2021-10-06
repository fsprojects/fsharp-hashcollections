namespace FSharp.HashCollections

open System.Collections.Generic
open System.Runtime.CompilerServices
open System

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