# FSharp.HashCollections

Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations across the .NET ecosystem and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented using a standard HAMT (Hash Mapped Array Trie).

## Goals

1) More efficient persistent collection type where F#'s Map type isn't fast enough.
2) Allow a range of key types and equality logic to be used even if provided by consumers without sacrificing performance (e.g. no need for keys to be comparable).
3) Provide an idiomatic API to F#. The library ideally should allow C# usage/interop if required.
4) Maintainable to an average F# developer.

## Collection Types Provided

All collections are persisted/immutable by nature so any Add/Remove operation produces a new collection instance. Most methods mirror the F# built in Map and Set module (e.g. Map.tryFind vs HashMap.tryFind) allowing in many cases this library to be used as a drop in replacement where appropriate.

- HashMap (Similar to F#'s Map in behaviour).

| Operation | Complexity |
| --- | --- |
| TryFind | O(log32n) or ~ O(1) |
| Add | O(log32n) or ~ O(1) |
| Remove | O(log32n) or ~ O(1) |
| Count | O(1) |

Example Usage:
```
open FSharp.HashCollections
let hashMapResult = HashMap.empty |> HashMap.add k v |> HashMay.tryFind k // Result is ValueSome(v)
```

- HashSet (Similar to F#'s Set in behaviour).

| Operation | Complexity |
| --- | --- |
| Contains | O(log32n) or ~ O(1) |
| Add | O(log32n) or ~ O(1) |
| Remove | O(log32n) or ~ O(1) |
| Count | O(1) |

Example Usage:
```
open FSharp.HashCollections
let hashMapResult = HashSet.empty |> HashSet.add k |> HashSet.contains k // Result is true
```

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
- (Optional): Be a struct type. This is recommended for performance as it produces more optimised equality checking code at runtime.

## Performance

### TryFind on HashMap

Keys are of type int32 where "GetHashMap" represents this library's HashMap collection.

```
// * Summary *

BenchmarkDotNet=v0.12.0, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                           Method | CollectionSize |        Mean |    Error |   StdDev |
|--------------------------------- |--------------- |------------:|---------:|---------:|
|                       GetHashMap |             10 |    11.29 ns | 0.018 ns | 0.017 ns |
|                GetImToolsHashMap |             10 |    28.15 ns | 0.192 ns | 0.180 ns |
|                     GetFSharpMap |             10 |    41.07 ns | 0.589 ns | 0.551 ns |
|                GetFSharpXHashMap |             10 |    99.36 ns | 1.614 ns | 1.510 ns |
| GetSystemCollectionsImmutableMap |             10 |    26.49 ns | 0.106 ns | 0.099 ns |
|                       GetHashMap |            100 |    13.04 ns | 0.014 ns | 0.013 ns |
|                GetImToolsHashMap |            100 |    42.63 ns | 0.841 ns | 1.233 ns |
|                     GetFSharpMap |            100 |    73.12 ns | 1.456 ns | 1.496 ns |
|                GetFSharpXHashMap |            100 |   112.69 ns | 1.623 ns | 1.518 ns |
| GetSystemCollectionsImmutableMap |            100 |    37.03 ns | 0.255 ns | 0.239 ns |
|                       GetHashMap |           1000 |    11.45 ns | 0.003 ns | 0.003 ns |
|                GetImToolsHashMap |           1000 |    64.82 ns | 0.234 ns | 0.219 ns |
|                     GetFSharpMap |           1000 |   102.40 ns | 2.037 ns | 2.501 ns |
|                GetFSharpXHashMap |           1000 |   117.86 ns | 1.863 ns | 1.743 ns |
| GetSystemCollectionsImmutableMap |           1000 |    53.08 ns | 0.141 ns | 0.132 ns |
|                       GetHashMap |         100000 |    23.63 ns | 0.024 ns | 0.023 ns |
|                GetImToolsHashMap |         100000 |   200.21 ns | 0.206 ns | 0.193 ns |
|                     GetFSharpMap |         100000 |   194.06 ns | 1.847 ns | 1.728 ns |
|                GetFSharpXHashMap |         100000 |   146.31 ns | 1.354 ns | 1.267 ns |
| GetSystemCollectionsImmutableMap |         100000 |   142.89 ns | 0.068 ns | 0.063 ns |
|                       GetHashMap |         500000 |    70.05 ns | 0.044 ns | 0.039 ns |
|                GetImToolsHashMap |         500000 |   509.51 ns | 0.317 ns | 0.281 ns |
|                     GetFSharpMap |         500000 |   328.15 ns | 1.467 ns | 1.372 ns |
|                GetFSharpXHashMap |         500000 |   233.56 ns | 1.059 ns | 0.990 ns |
| GetSystemCollectionsImmutableMap |         500000 |   322.57 ns | 0.777 ns | 0.726 ns |
|                       GetHashMap |         750000 |    96.93 ns | 0.360 ns | 0.337 ns |
|                GetImToolsHashMap |         750000 |   629.03 ns | 0.378 ns | 0.315 ns |
|                     GetFSharpMap |         750000 |   413.14 ns | 3.738 ns | 3.496 ns |
|                GetFSharpXHashMap |         750000 |   355.23 ns | 0.783 ns | 0.694 ns |
| GetSystemCollectionsImmutableMap |         750000 |   411.91 ns | 0.292 ns | 0.244 ns |
|                       GetHashMap |        1000000 |   105.36 ns | 0.053 ns | 0.047 ns |
|                GetImToolsHashMap |        1000000 |   694.33 ns | 0.368 ns | 0.344 ns |
|                     GetFSharpMap |        1000000 |   470.99 ns | 4.005 ns | 3.127 ns |
|                GetFSharpXHashMap |        1000000 |   333.83 ns | 0.449 ns | 0.420 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   480.34 ns | 1.495 ns | 1.325 ns |
|                       GetHashMap |        5000000 |   134.20 ns | 0.136 ns | 0.127 ns |
|                GetImToolsHashMap |        5000000 | 1,286.32 ns | 3.662 ns | 3.426 ns |
|                     GetFSharpMap |        5000000 |   834.56 ns | 5.437 ns | 5.086 ns |
|                GetFSharpXHashMap |        5000000 |   377.85 ns | 3.815 ns | 3.186 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   872.64 ns | 7.665 ns | 7.170 ns |
|                       GetHashMap |       10000000 |   155.14 ns | 1.077 ns | 1.007 ns |
|                GetImToolsHashMap |       10000000 | 1,573.52 ns | 4.160 ns | 3.891 ns |
|                     GetFSharpMap |       10000000 | 1,042.60 ns | 5.229 ns | 4.891 ns |
|                GetFSharpXHashMap |       10000000 |   410.99 ns | 1.255 ns | 1.174 ns |
| GetSystemCollectionsImmutableMap |       10000000 | 1,011.88 ns | 4.027 ns | 3.363 ns |
```

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
