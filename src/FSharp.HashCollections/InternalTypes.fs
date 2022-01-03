namespace FSharp.HashCollections

open System.Collections.Generic
open System.Runtime.CompilerServices
open System

type internal HashTrieNode<'tk> =
    | TrieNode of nodes: CompressedArray<HashTrieNode<'tk>> * entries: CompressedArray<'tk>
    | HashCollisionNode of entries: 'tk list

type [<Struct>] internal HashTrieRoot<'tnode> = {
    CurrentCount: int32
    RootData: HashTrieNode<'tnode>
}

module internal HelperFunctions =

    // Taken from https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/prim-types.fs to be consistent with F# Map handling (MIT License - https://github.com/fsharp/fsharp/blob/master/License.txt)
    let inline anyToString nullStr x =
        match box x with
        | null -> nullStr
        | :? System.IFormattable as f -> f.ToString(null,System.Globalization.CultureInfo.InvariantCulture)
        | obj ->  obj.ToString()

    let anyToStringShowingNull x = anyToString "null" x