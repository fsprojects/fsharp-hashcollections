# FSharp.HashCollections

Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations across the .NET ecosystem and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented using a standard HAMT (Hash Mapped Array Trie).

## Goals

1) More efficient persistent collection type where F#'s Map type isn't fast enough.
2) Provide an idiomatic API to F#. The library ideally should allow C# usage/interop if required.
3) Performance where it does not impede goal 2.
4) Maintainable to an average F# developer.
5) Allow a range of key types and equality logic to be used even if provided by consumers without sacrificing performance (e.g. no need for keys to be comparable).

## Collection Types Provided

All collections are persisted/immutable by nature so any Add/Remove operation produces a new collection instance.

- HashMap (Similar to F#'s Map in behaviour).

| Operation | Complexity |
| --- | --- |
| TryFind | O(log32n) or ~ O(1) |
| Add | O(log32n) or ~ O(1) |
| Remove | O(log32n) or ~ O(1) |
| Count | O(1) |

- HashSet (Similar to F#'s Set in behaviour).

| Operation | Complexity |
| --- | --- |
| Contains | O(log32n) or ~ O(1) |
| Add | O(log32n) or ~ O(1) |
| Remove | O(log32n) or ~ O(1) |
| Count | O(1) |

## Equality customisation

All collections allow custom equality to be assigned if required. The equality is encoded as a type on the collection so equality and hashing operations are consistent. Same collection types with different equality comparers can not be used interchangeably by operations provided.  To use:

```
// Uses the default equality template provided.
let defaultIntHashMap : HashMap<int, int64, StandardEqualityTemplate<int>> = HashMap.empty

// Uses a custom equality template provided by the type parameter.
let defaultIntHashMap : HashMap<int, int64, CustomEqualityComparer> = HashMap.emptyWithComparer
```

Any equality comparer specified in the type signature must:

- Have a parameterless public constructor.
- Implement IEqualityComparer<> for either the contents (HashSet) or the key (HashMap).
- (Optional): Be a struct type. This is recommended for performance as it triggers many JIT inlining optimisations (no virtual dispatch).

## Performance

### TryFind on HashMap

```
|                           Method | CollectionSize |        Mean |    Error |   StdDev |
|--------------------------------- |--------------- |------------:|---------:|---------:|
|                       GetHashMap |             10 |    11.12 ns | 0.097 ns | 0.091 ns |
|                     GetFSharpMap |             10 |    41.49 ns | 0.588 ns | 0.550 ns |
|                GetFSharpXHashMap |             10 |   108.50 ns | 2.135 ns | 1.997 ns |
| GetSystemCollectionsImmutableMap |             10 |    27.50 ns | 0.244 ns | 0.228 ns |
|                       GetHashMap |            100 |    13.02 ns | 0.019 ns | 0.018 ns |
|                     GetFSharpMap |            100 |    71.66 ns | 1.203 ns | 1.126 ns |
|                GetFSharpXHashMap |            100 |   113.71 ns | 1.624 ns | 1.519 ns |
| GetSystemCollectionsImmutableMap |            100 |    37.04 ns | 0.247 ns | 0.231 ns |
|                       GetHashMap |           1000 |    11.51 ns | 0.065 ns | 0.058 ns |
|                     GetFSharpMap |           1000 |   105.31 ns | 0.914 ns | 0.714 ns |
|                GetFSharpXHashMap |           1000 |   119.99 ns | 1.328 ns | 1.242 ns |
| GetSystemCollectionsImmutableMap |           1000 |    55.18 ns | 0.192 ns | 0.180 ns |
|                       GetHashMap |         100000 |    24.23 ns | 0.193 ns | 0.181 ns |
|                     GetFSharpMap |         100000 |   195.49 ns | 1.699 ns | 1.590 ns |
|                GetFSharpXHashMap |         100000 |   151.25 ns | 0.987 ns | 0.924 ns |
| GetSystemCollectionsImmutableMap |         100000 |   146.26 ns | 0.209 ns | 0.195 ns |
|                       GetHashMap |         500000 |    72.79 ns | 0.046 ns | 0.041 ns |
|                     GetFSharpMap |         500000 |   322.10 ns | 3.607 ns | 3.374 ns |
|                GetFSharpXHashMap |         500000 |   244.35 ns | 1.318 ns | 1.233 ns |
| GetSystemCollectionsImmutableMap |         500000 |   324.23 ns | 0.531 ns | 0.497 ns |
|                       GetHashMap |         750000 |   104.68 ns | 0.189 ns | 0.177 ns |
|                     GetFSharpMap |         750000 |   405.20 ns | 1.588 ns | 1.408 ns |
|                GetFSharpXHashMap |         750000 |   366.52 ns | 4.730 ns | 4.425 ns |
| GetSystemCollectionsImmutableMap |         750000 |   427.22 ns | 0.587 ns | 0.549 ns |
|                       GetHashMap |        1000000 |   111.72 ns | 0.339 ns | 0.283 ns |
|                     GetFSharpMap |        1000000 |   478.95 ns | 5.776 ns | 5.403 ns |
|                GetFSharpXHashMap |        1000000 |   340.21 ns | 0.775 ns | 0.725 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   503.40 ns | 1.028 ns | 0.961 ns |
|                       GetHashMap |        5000000 |   138.25 ns | 0.268 ns | 0.251 ns |
|                     GetFSharpMap |        5000000 |   855.74 ns | 3.871 ns | 3.621 ns |
|                GetFSharpXHashMap |        5000000 |   379.65 ns | 0.733 ns | 0.685 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   901.71 ns | 1.080 ns | 0.957 ns |
|                       GetHashMap |       10000000 |   154.81 ns | 1.315 ns | 1.230 ns |
|                     GetFSharpMap |       10000000 | 1,031.15 ns | 3.435 ns | 3.213 ns |
|                GetFSharpXHashMap |       10000000 |   420.84 ns | 4.111 ns | 3.845 ns |
| GetSystemCollectionsImmutableMap |       10000000 | 1,059.00 ns | 7.087 ns | 6.629 ns |```

## Design decisions that may affect consumers of this library

Any of these decisions may change in the future as I gain knowledge, change my mind, etc. It doesn't list all the tweaks, and changes caused by benchmarking just the things that affect consumers. Many besides equality checking shouldn't affect the API dramatically; and if they do it should remain easy to port code to the new API as appropriate.

1) Writing in F# vs C#
    - Performance tweaks found in my trial and error experiments (structs, inlining, etc) are easier to unlock and use in F# requiring less code. Inlining is used for algorithm sharing across collection types, avoiding struct copying with returned results, etc.

2) Count is a O(1) operation. This requires an extra bit of space per tree and minor overhead during insert and removal but allows other operations on the read side to be faster (e.g isEmpty, count, intersect, etc.).
    - ✅ Lower time complexity for existence and count operations.
    - ❌ Slightly more work required when inserting and removing elements keeping track of addition or removal success.

3) NetCoreApp3.1 only or greater. This allows the use of .NET intrinsics and other performance enhancements.
    - ✅ Faster implementation.
    - ❌ Only Net Core 3.1 compatible or greater.