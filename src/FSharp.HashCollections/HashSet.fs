namespace rec FSharp.HashCollections

open System
open System.Collections
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Text

[<AutoOpen>]
module internal HashSetInternalSettings =
    let inline internal keyExtractor i = i
    let inline internal valueExtractor i = i

/// Immutable hash set.
type [<Struct; IsReadOnly; CustomEquality; NoComparison>] HashSet<'tk, 'teq when 'teq :> IEqualityComparer<'tk>> =
    val internal HashTrieRoot: HashTrieRoot<'tk>
    val internal EqualityComparer: 'teq
    internal new(d, eq) = { HashTrieRoot = d; EqualityComparer = eq }

    // Taken from https://github.com/dotnet/fsharp/blob/main/src/fsharp/FSharp.Core/set.fs#L788-L806 for consistency with idiomatic set. (MIT License - see https://github.com/dotnet/fsharp/blob/main/License.txt)
    override this.ToString() =
        match List.ofSeq (Seq.truncate 4 this) with
        | [] -> "set []"
        | [h1] ->
            let txt1 = HelperFunctions.anyToStringShowingNull h1
            StringBuilder().Append("hashSet [").Append(txt1).Append("]").ToString()
        | [h1; h2] ->
            let txt1 = HelperFunctions.anyToStringShowingNull h1
            let txt2 = HelperFunctions.anyToStringShowingNull h2
            StringBuilder().Append("hashSet [").Append(txt1).Append("; ").Append(txt2).Append("]").ToString()
        | [h1; h2; h3] ->
            let txt1 = HelperFunctions.anyToStringShowingNull h1
            let txt2 = HelperFunctions.anyToStringShowingNull h2
            let txt3 = HelperFunctions.anyToStringShowingNull h3
            StringBuilder().Append("hashSet [").Append(txt1).Append("; ").Append(txt2).Append("; ").Append(txt3).Append("]").ToString()
        | h1 :: h2 :: h3 :: _ ->
            let txt1 = HelperFunctions.anyToStringShowingNull h1
            let txt2 = HelperFunctions.anyToStringShowingNull h2
            let txt3 = HelperFunctions.anyToStringShowingNull h3
            StringBuilder().Append("hashSet [").Append(txt1).Append("; ").Append(txt2).Append("; ").Append(txt3).Append("; ... ]").ToString()

    member this.Equals(other: HashSet<_, _>) = HashTrie.equals this.EqualityComparer keyExtractor (fun _ _ -> true) this.EqualityComparer.GetHashCode this.HashTrieRoot other.HashTrieRoot

    override this.Equals(other: obj) = match other with | :? HashSet<'tk, 'teq> as otherTyped -> this.Equals(otherTyped) | _ -> false

    interface IEquatable<HashSet<'tk, 'teq>> with member this.Equals(other: HashSet<_, _>) = this.Equals(other)

    override this.GetHashCode() =
        // Shamelessly taken from the FSharp Set code for consistency (https://github.com/fsharp/fsharp/blob/master/src/fsharp/FSharp.Core/set.fs#L683-L688)
        let inline combineHash x y = (x <<< 1) + y + 631
        let mutable res = 0
        for x in HashTrie.toSeq this.HashTrieRoot do
            res <- combineHash res (this.EqualityComparer.GetHashCode(keyExtractor x))
        abs res

    member this.GetEnumerator() = (this.HashTrieRoot |> HashTrie.toSeq).GetEnumerator()
    interface IEnumerable<'tk> with
        member this.GetEnumerator() = this.GetEnumerator()
    interface IEnumerable with
        member this.GetEnumerator() = this.GetEnumerator() :> IEnumerator


/// Immutable hash map with default structural comparison.
type HashSet<'tk> = HashSet<'tk, IEqualityComparer<'tk>>

module HashSet =

    let contains (k: 'tk) (hashSet: HashSet<'tk, 'teq>) : bool =
        HashTrie.tryFind keyExtractor valueExtractor hashSet.EqualityComparer k hashSet.HashTrieRoot |> ValueOption.isSome

    let add (k: 'tk) (hashSet: HashSet<'tk, 'teq>) =
        HashSet<_, _>(HashTrie.add keyExtractor hashSet.EqualityComparer k hashSet.HashTrieRoot, hashSet.EqualityComparer)

    let remove (k: 'tk) (hashSet: HashSet<'tk, 'teq>) =
        HashSet<_, _>(HashTrie.remove keyExtractor hashSet.EqualityComparer k hashSet.HashTrieRoot, hashSet.EqualityComparer)

    let count (h: HashSet<_, _>) = HashTrie.count h.HashTrieRoot

    let emptyWithComparer<'tk, 'teq when 'teq :> System.Collections.Generic.IEqualityComparer<'tk> and 'teq : (new : unit -> 'teq)> : HashSet<'tk, 'teq> =
        let eqTemplate = new 'teq()
        HashSet<_, _>(HashTrie.empty, eqTemplate)

    let empty<'tk when 'tk : equality> : HashSet<'tk> =
        HashSet<'tk>(HashTrie.empty, HashIdentity.Structural)

    let toSeq (h: HashSet<'tk, _>) : 'tk seq = h :> seq<'tk>

    let isEmpty (h: HashSet<_, _>) = h.HashTrieRoot |> HashTrie.isEmpty

    let emptyWithSameComparer (h: HashSet<'tk, 'teq>) : HashSet<'tk, 'teq> = HashSet<'tk, 'teq>(HashTrie.empty, h.EqualityComparer)

    let inline private getSmallerAndLargerSet h1 h2 = if count h1 > count h2 then struct (h2, h1) else struct (h1, h2)

    let intersect (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) =
        let struct (smallerSet, largerSet) = getSmallerAndLargerSet h1 h2
        let mutable r = emptyWithSameComparer smallerSet
        for item in smallerSet |> toSeq do
            if largerSet |> contains item
            then r <- r |> add item
        r

    let fold folder state (h: HashSet<'t, 'teq>) = h |> toSeq |> Seq.fold folder state

    let difference (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) =
        h2 |> fold (fun s t -> s |> remove t) h1

    let union (h1: HashSet<'t, 'teq>) (h2: HashSet<'t, 'teq>) =
        let struct (smallerSet, largerSet) = getSmallerAndLargerSet h1 h2
        smallerSet |> fold (fun s t -> s |> add t) largerSet

    let filter predicate (h: HashSet<'t, 'teq>) =
        h |> fold (fun s t -> if predicate t then s |> add t else s) (emptyWithComparer<'t, 'teq>)

    let map mapping (h: HashSet<_, 'teq>) =
        h |> fold (fun s t -> s |> add (mapping t)) (emptyWithComparer<_, 'teq>)

    let ofSeq (s: #seq<'k>) : HashSet<'k> =
        let eqComparer = HashIdentity.Structural
        HashSet<_>(HashTrie.ofSeq keyExtractor eqComparer s empty.HashTrieRoot, eqComparer)

[<AutoOpen>]
module AlwaysOpenHashSet =
    let hashSet s = HashSet.ofSeq s