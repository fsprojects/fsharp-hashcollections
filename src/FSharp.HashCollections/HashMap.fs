namespace FSharp.HashCollections

open System.Collections
open System.Collections.Generic
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module internal HashMapInternalSettings =
    let inline internal keyExtractor (hme: KeyValuePair<_, _>) = hme.Key
    let inline internal valueExtractor (hme: KeyValuePair<_, _>) = hme.Value

/// Immutable hash map.
type [<Struct; IsReadOnly; CustomEquality; NoComparison>] HashMap<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk>> =
    val internal HashTrieRoot: HashTrieRoot<KeyValuePair<'tk, 'tv>>
    val internal EqualityComparer: 'teq

    internal new(d, eq) = { HashTrieRoot = d; EqualityComparer = eq }

    // Taken from https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/map.fs#L631-L636 to keep to standard. (MIT License - https://github.com/fsharp/fsharp/blob/master/License.txt)
    override this.ToString() =
        match List.ofSeq (Seq.truncate 4 this) with
        | [] -> "hashMap []"
        | [KeyValue h1] -> System.Text.StringBuilder().Append("hashMap [").Append(HelperFunctions.anyToStringShowingNull h1).Append("]").ToString()
        | [KeyValue h1;KeyValue h2] -> System.Text.StringBuilder().Append("hashMap [").Append(HelperFunctions.anyToStringShowingNull h1).Append("; ").Append(HelperFunctions.anyToStringShowingNull h2).Append("]").ToString()
        | [KeyValue h1;KeyValue h2;KeyValue h3] -> System.Text.StringBuilder().Append("hashMap [").Append(HelperFunctions.anyToStringShowingNull h1).Append("; ").Append(HelperFunctions.anyToStringShowingNull h2).Append("; ").Append(HelperFunctions.anyToStringShowingNull h3).Append("]").ToString()
        | KeyValue h1 :: KeyValue h2 :: KeyValue h3 :: _ -> System.Text.StringBuilder().Append("hashMap [").Append(HelperFunctions.anyToStringShowingNull h1).Append("; ").Append(HelperFunctions.anyToStringShowingNull h2).Append("; ").Append(HelperFunctions.anyToStringShowingNull h3).Append("; ... ]").ToString()

    member this.Equals(other: HashMap<'tk, 'tv, 'teq>): bool = 
        let eqComparer = this.EqualityComparer
        HashTrie.equals 
            this.EqualityComparer 
            keyExtractor 
            (fun x y -> Unchecked.equals x.Value y.Value)
            (fun o -> HashCode.Combine(eqComparer.GetHashCode(keyExtractor o), Unchecked.hash (valueExtractor o)))
            this.HashTrieRoot 
            other.HashTrieRoot

    override this.Equals(other: obj) =
        match other with
        | :? HashMap<'tk, 'tv, 'teq> as otherTyped -> this.Equals(otherTyped)
        | _ -> false
        
    interface IEquatable<HashMap<'tk, 'tv, 'teq>> with  member this.Equals(other) = this.Equals(other)

    override this.GetHashCode() =
        let inline combineHash x y = (x <<< 1) + y + 631
        let mutable res = 0
        for x in HashTrie.toSeq this.HashTrieRoot do
            res <- combineHash res (this.EqualityComparer.GetHashCode(keyExtractor x))
            // NOTE: This Unchecked.hash could result in perf penalities since it isn't statically determined I believe (inlined to caller site).
            // The key isn't affected by this issue since the equality comparer is pre-calc'ed for this.
            res <- combineHash res (Unchecked.hash x.Value)
        abs res

    member this.GetEnumerator() = (this.HashTrieRoot |> HashTrie.toSeq).GetEnumerator()
    interface IEnumerable<KeyValuePair<'tk, 'tv>> with
        member this.GetEnumerator() = this.GetEnumerator()
    interface IEnumerable with
        member this.GetEnumerator() = this.GetEnumerator() :> IEnumerator

    member this.Item key = 
        let inline tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption =
            HashTrie.tryFind keyExtractor valueExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot
        
        
        match this |> tryFind key with | ValueSome(v) -> v | ValueNone -> raise (KeyNotFoundException())

    interface IReadOnlyDictionary<'tk, 'tv> with
        member this.Item with get(key) = this.Item key
        member this.Keys = 
            let this = this
            seq { for kvp in this do yield kvp.Key }
        member this.TryGetValue(key, value:byref<'tv>) =
            let inline tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption =
                HashTrie.tryFind keyExtractor valueExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot
            match this |> tryFind key with
            | ValueSome(v) -> value <- v; true
            | ValueNone -> false
        member this.Values = 
            let this = this
            seq { for kvp in this do yield kvp.Value }
        member this.ContainsKey key = 
            let inline tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption =
                HashTrie.tryFind keyExtractor valueExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot
            this |> tryFind key |> ValueOption.isSome 
        member this.Count = HashTrie.count this.HashTrieRoot

/// Immutable hash map with default structural comparison.
type HashMap<'tk, 'tv> = HashMap<'tk, 'tv, IEqualityComparer<'tk>>

module HashMap =

    let inline internal keyExtractor (hme: KeyValuePair<_, _>) = hme.Key
    let inline internal valueExtractor (hme: KeyValuePair<_, _>) = hme.Value

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let tryFind (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) : 'tv voption =
        HashTrie.tryFind keyExtractor valueExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let add (k: 'tk) (v: 'tv) (hashMap: HashMap<'tk, 'tv, 'teq>) =
        HashMap<_, _, _>(
            HashTrie.add keyExtractor hashMap.EqualityComparer (KeyValuePair<_, _>(k, v)) hashMap.HashTrieRoot,
            hashMap.EqualityComparer)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let remove (k: 'tk) (hashMap: HashMap<'tk, 'tv, 'teq>) =
        HashMap<_, _, _>(
            HashTrie.remove keyExtractor hashMap.EqualityComparer k hashMap.HashTrieRoot,
            hashMap.EqualityComparer)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let count (h: HashMap<_, _, _>) = HashTrie.count h.HashTrieRoot

    let emptyWithComparer<'tk, 'tv, 'teq when 'teq :> IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashMap<'tk, 'tv, 'teq> =
        HashMap<_, _, _>(HashTrie.empty, new 'teq())

    let empty<'tk, 'tv when 'tk : equality> : HashMap<'tk, 'tv> =
        HashMap<_, _>(HashTrie.empty, HashIdentity.Structural)

    let toSeq (h: HashMap<'tk, 'tv, _>) : (struct ('tk * 'tv) seq) =
        seq { for i in h do yield struct (i.Key, i.Value) }

    let isEmpty (h: HashMap<_, _, _>) = h.HashTrieRoot |> HashTrie.isEmpty

    let ofSeq (s: #seq<KeyValuePair<'k, 'v>>) : HashMap<'k, 'v> =
        let eqComparer = HashIdentity.Structural
        HashMap<_, _>(
            HashTrie.ofSeq keyExtractor eqComparer s empty.HashTrieRoot,
            eqComparer)

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let containsKey k hm = hm |> tryFind k |> ValueOption.isSome

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let keys (hm: HashMap<_, _, _>) = seq { for kvp in hm do yield kvp.Key }

    [<MethodImpl(MethodImplOptions.AggressiveInlining)>]
    let values (hm: HashMap<_, _, _>) = seq { for kvp in hm do yield kvp.Value }

[<AutoOpen>]
module AlwaysOpenHashMap =
    let hashMap (s: #seq<'k * 'v>) = s |> Seq.map KeyValuePair.Create |> HashMap.ofSeq