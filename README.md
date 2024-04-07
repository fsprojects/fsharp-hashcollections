# FSharp.HashCollections

Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations across the .NET ecosystem and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented as a customised/optimised HAMT (Hash Mapped Array Trie) for the .NET runtime.

[![NuGet Badge](http://img.shields.io/nuget/v/FSharp.HashCollections.svg?style=flat)](https://www.nuget.org/packages/FSharp.HashCollections)

## Goals

1) More efficient persistent collection type where F#'s Map type isn't fast enough.
    - At time of writing this was the fastest immutable collections library in the .NET ecosystem I could find (including BCL classes). See [Performance Benchmarks](#performance-benchmarks) for details.
    - Tailor the algorithms used to the .NET ecosystem to achieve better performance.
2) Allow a range of key types and equality logic to be used even if provided by consumers without sacrificing performance (e.g. no need for keys to be comparable).
    - Custom equality comparers can be provided, unlike the standard F# Map/Set data types.
3) Provide an idiomatic API to F#. The library ideally should allow C# usage/interop if required.
    - HashMap and HashSet static classes are usable from C# as wrappers. Performance optimisations (e.g. inlining) are applied at compile time where possible.
4) Maintainable to an average F# developer.
    - For example minimising inheritance/object hierarchies and casting (unlike some other impl's I've seen), performant whilst still idiomatic code, etc.
    - Use F# strengths to increase performance further (e.g. inlining + DU's to avoid method calls and copying overhead affecting struct key performance).
5) Performance at least at the same scale as other languages of the same class/abstraction level (e.g JVM, etc).

**TLDR; Benefits of immutable collections while minimising the cost (e.g. performance, maintainable code, idiomatic code, etc).**

## Use Cases

- Large collections at acceptable performance (e.g. 500,000+ elements).
- Immutability of large/deep object graphs without the associated performance cost of changing data deep in the hierarchy.
  - A common pattern when changing data deep in nested records. Instead of using the record copy syntax to change these flatten out of object and use HashMaps instead joining by key. Often useful to store a large hierarchy of state and update it in an atomic fashion.
- Where the key type of the Map would otherwise not work with standard F# collections since it does not implement IComparable.

## Collection Types Provided

All collections are persisted/immutable by nature so any Add/Remove operation produces a new collection instance. Most methods mirror the F# built in Map and Set module (e.g. Map.tryFind vs HashMap.tryFind) allowing in many cases this library to be used as a drop in replacement where appropriate.

- HashMap (Similar to F#'s Map in behaviour).

| Operation | Complexity |
| --- | --- |
| TryFind | O(log32n) |
| Add | O(log32n) |
| Remove | O(log32n) |
| Count | O(1) |
| Equals | O(n) |

Example Usage:
```fs
open FSharp.HashCollections
let hashMapResult = HashMap.empty |> HashMap.add k v |> HashMay.tryFind k // Result is ValueSome(v)
```

- HashSet (Similar to F#'s Set in behaviour).

| Operation | Complexity |
| --- | --- |
| Contains | O(log32n) |
| Add | O(log32n) |
| Remove | O(log32n) |
| Count | O(1) |
| Equals | O(n) |

Example Usage:
```fs
open FSharp.HashCollections
let hashMapResult = HashSet.empty |> HashSet.add k |> HashSet.contains k // Result is true
```

## Equality customisation

By default any "empty" seeded HashSet/Map uses F# HashIdentity.Structural comparison to determine equality and calculate hashcodes. This is in line with the F# collection types.

In addition all collection types allow custom equality to be assigned if required. The equality is encoded as a type on the collection so equality and hashing operations are consistent. Same collection types with different equality comparers can not be used interchangeably by operations provided.  To use:

```fs
// Uses the default equality template provided.
let defaultIntHashMap : HashMap<int, int64> = HashMap.empty

// Uses a custom equality template provided by the type parameter.
let customEqIntHashMap : HashMap<int, int64, CustomEqualityComparer> = HashMap.emptyWithComparer
```

Any equality comparer specified in the type signature must:

- Have a parameterless public constructor.
- Implement IEqualityComparer<> for either the contents (HashSet) or the key (HashMap).
- (Optional): Be a struct type. This is recommended for performance as it produces more optimised equality checking code at runtime.

An example with the type "Int" for the custom equality comparer (which in testing exhibits slightly faster perf than the default):

```fs
type IntEqualityTemplate =
    struct end
    interface System.Collections.Generic.IEqualityComparer<int> with
        member __.Equals(x: int, y: int): bool = x = y
        member __.GetHashCode(obj: int): int = hash obj // Or just obj

module Usage =
    // Type is HashMap<int, int64, IntEqualityTemplate>
    let empty = HashMap.emptyWithComparer<_, int64, IntEqualityTemplate>
```

## Performance Benchmarks

### TryFind on HashMap

Keys are of type int32 where "GetHashMap" represents this library's HashMap collection.

All are using F# HashIdentity.Structural comparison.

See [ReadBenchmark](benchmarks/FSharp.HashCollections.Benchmarks.ReadBenchmarks-report-github.md)

``` ini

BenchmarkDotNet=v0.13.1, OS=arch 
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET SDK=6.0.403
  [Host]     : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT DEBUG
  DefaultJob : .NET 6.0.11 (6.0.1122.52304), X64 RyuJIT


```
|                           Method | CollectionSize |        Mean |     Error |    StdDev |
|--------------------------------- |--------------- |------------:|----------:|----------:|
|                       **GetHashMap** |             **10** |    **11.49 ns** |  **0.091 ns** |  **0.081 ns** |
|                GetImToolsHashMap |             10 |    31.58 ns |  0.190 ns |  0.169 ns |
|                     GetFSharpMap |             10 |    26.97 ns |  0.208 ns |  0.194 ns |
|                GetFSharpXHashMap |             10 |   129.21 ns |  0.806 ns |  0.754 ns |
| GetSystemCollectionsImmutableMap |             10 |    21.80 ns |  0.325 ns |  0.304 ns |
|         GetFSharpDataAdaptiveMap |             10 |    22.95 ns |  0.234 ns |  0.219 ns |
|               GetFSharpxChampMap |             10 |    72.75 ns |  0.439 ns |  0.367 ns |
|                    GetLangExtMap |             10 |    24.83 ns |  0.218 ns |  0.193 ns |
|                       **GetHashMap** |            **100** |    **11.75 ns** |  **0.120 ns** |  **0.112 ns** |
|                GetImToolsHashMap |            100 |    44.99 ns |  0.358 ns |  0.317 ns |
|                     GetFSharpMap |            100 |    47.45 ns |  0.418 ns |  0.391 ns |
|                GetFSharpXHashMap |            100 |   148.55 ns |  0.919 ns |  0.815 ns |
| GetSystemCollectionsImmutableMap |            100 |    33.26 ns |  0.246 ns |  0.230 ns |
|         GetFSharpDataAdaptiveMap |            100 |    44.84 ns |  0.400 ns |  0.374 ns |
|               GetFSharpxChampMap |            100 |    83.54 ns |  1.017 ns |  0.951 ns |
|                    GetLangExtMap |            100 |    31.26 ns |  0.264 ns |  0.247 ns |
|                       **GetHashMap** |           **1000** |    **11.67 ns** |  **0.090 ns** |  **0.084 ns** |
|                GetImToolsHashMap |           1000 |    69.63 ns |  0.974 ns |  0.911 ns |
|                     GetFSharpMap |           1000 |    71.16 ns |  0.762 ns |  0.713 ns |
|                GetFSharpXHashMap |           1000 |   161.89 ns |  0.539 ns |  0.450 ns |
| GetSystemCollectionsImmutableMap |           1000 |    49.93 ns |  0.489 ns |  0.458 ns |
|         GetFSharpDataAdaptiveMap |           1000 |    67.05 ns |  0.725 ns |  0.678 ns |
|               GetFSharpxChampMap |           1000 |    84.44 ns |  1.061 ns |  0.992 ns |
|                    GetLangExtMap |           1000 |    31.00 ns |  0.312 ns |  0.292 ns |
|                       **GetHashMap** |         **100000** |    **33.47 ns** |  **0.376 ns** |  **0.314 ns** |
|                GetImToolsHashMap |         100000 |   216.79 ns |  4.285 ns |  4.009 ns |
|                     GetFSharpMap |         100000 |   177.11 ns |  2.074 ns |  1.940 ns |
|                GetFSharpXHashMap |         100000 |   231.84 ns |  4.178 ns |  6.627 ns |
| GetSystemCollectionsImmutableMap |         100000 |   162.46 ns |  2.537 ns |  2.374 ns |
|         GetFSharpDataAdaptiveMap |         100000 |   154.96 ns |  2.743 ns |  2.566 ns |
|               GetFSharpxChampMap |         100000 |   114.73 ns |  1.243 ns |  1.163 ns |
|                    GetLangExtMap |         100000 |    61.52 ns |  0.609 ns |  0.570 ns |
|                       **GetHashMap** |         **500000** |    **35.67 ns** |  **0.622 ns** |  **0.582 ns** |
|                GetImToolsHashMap |         500000 |   528.33 ns | 10.475 ns | 12.063 ns |
|                     GetFSharpMap |         500000 |   327.39 ns |  6.410 ns | 11.881 ns |
|                GetFSharpXHashMap |         500000 |   336.89 ns |  5.012 ns |  4.688 ns |
| GetSystemCollectionsImmutableMap |         500000 |   359.41 ns |  7.174 ns | 11.787 ns |
|         GetFSharpDataAdaptiveMap |         500000 |   324.83 ns |  6.091 ns |  5.982 ns |
|               GetFSharpxChampMap |         500000 |   122.62 ns |  1.997 ns |  2.137 ns |
|                    GetLangExtMap |         500000 |    67.00 ns |  1.128 ns |  1.055 ns |
|                       **GetHashMap** |         **750000** |    **38.42 ns** |  **0.513 ns** |  **0.428 ns** |
|                GetImToolsHashMap |         750000 |   639.63 ns | 12.341 ns | 14.691 ns |
|                     GetFSharpMap |         750000 |   429.89 ns |  8.344 ns | 10.247 ns |
|                GetFSharpXHashMap |         750000 |   446.19 ns |  5.108 ns |  4.778 ns |
| GetSystemCollectionsImmutableMap |         750000 |   440.76 ns |  8.714 ns | 10.035 ns |
|         GetFSharpDataAdaptiveMap |         750000 |   364.25 ns |  7.225 ns |  6.758 ns |
|               GetFSharpxChampMap |         750000 |   128.73 ns |  2.571 ns |  3.848 ns |
|                    GetLangExtMap |         750000 |    72.10 ns |  1.426 ns |  1.334 ns |
|                       **GetHashMap** |        **1000000** |    **41.58 ns** |  **0.804 ns** |  **0.752 ns** |
|                GetImToolsHashMap |        1000000 |   716.12 ns | 13.576 ns | 12.699 ns |
|                     GetFSharpMap |        1000000 |   478.64 ns |  9.391 ns | 11.877 ns |
|                GetFSharpXHashMap |        1000000 |   426.63 ns |  4.339 ns |  4.059 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   506.48 ns |  9.872 ns |  9.695 ns |
|         GetFSharpDataAdaptiveMap |        1000000 |   395.59 ns |  5.966 ns |  5.581 ns |
|               GetFSharpxChampMap |        1000000 |   132.05 ns |  2.585 ns |  4.319 ns |
|                    GetLangExtMap |        1000000 |    79.30 ns |  1.577 ns |  2.408 ns |
|                       **GetHashMap** |        **5000000** |   **155.94 ns** |  **0.957 ns** |  **0.799 ns** |
|                GetImToolsHashMap |        5000000 | 1,138.25 ns | 18.869 ns | 17.650 ns |
|                     GetFSharpMap |        5000000 |   823.41 ns | 10.984 ns | 10.274 ns |
|                GetFSharpXHashMap |        5000000 |   475.15 ns |  6.462 ns |  6.044 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   826.09 ns | 15.213 ns | 14.230 ns |
|         GetFSharpDataAdaptiveMap |        5000000 |   579.94 ns |  6.645 ns |  6.216 ns |
|               GetFSharpxChampMap |        5000000 |   285.59 ns |  3.235 ns |  3.026 ns |
|                    GetLangExtMap |        5000000 |   234.92 ns |  3.571 ns |  3.341 ns |
|                       **GetHashMap** |       **10000000** |   **159.87 ns** |  **2.199 ns** |  **1.950 ns** |
|                GetImToolsHashMap |       10000000 | 1,345.84 ns | 26.844 ns | 26.364 ns |
|                     GetFSharpMap |       10000000 |   957.82 ns | 14.825 ns | 13.868 ns |
|                GetFSharpXHashMap |       10000000 |   509.87 ns |  9.244 ns |  8.647 ns |
| GetSystemCollectionsImmutableMap |       10000000 |   960.39 ns | 18.921 ns | 17.699 ns |
|         GetFSharpDataAdaptiveMap |       10000000 |   672.11 ns |  9.232 ns |  8.636 ns |
|               GetFSharpxChampMap |       10000000 |   291.37 ns |  3.615 ns |  3.381 ns |
|                    GetLangExtMap |       10000000 |   237.09 ns |  3.173 ns |  2.968 ns |

### Add on HashMap

Scenario: Adding 50 elements on top of a pre-defined collection with a collection size as specified, average time of each of the 50 inserts:

See [AddBenchmark](benchmarks/FSharp.HashCollections.Benchmarks.AddBenchmark-report-github.md)

### OfSeq on HashMap

See [OfSeq Benchmark](benchmarks/FSharp.HashCollections.Benchmarks.OfSeqBenchmark-report-github.md)


## An outside .NET ecosystem comparison.

This section is simply a guide to give a ballpark comparison figure on performance with implementations from other languages that have standard HAMT implementations for my own technical selection evaluation.

**TL;DR**: The "Get" method where performance is significantly better as the collection scales up. For example at 10,000,000 FSharp collection is approx 3.59 faster, and 1.73x faster at building the initial hashmap. 

- TryFind: Measures fetching every key inside a collection of a given size (total milliseconds).
- OfSeq: Measures building a HashMap of the given collection size (total milliseconds).

| Lang | Operation | 100 | 1000 | 10000 | 100000 | 500000 | 1000000 | 5000000 | 10000000 | 50000000 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| F# | TryFind | 0 | 0 | 0 | 5 | 37 | 94 | 495 | 1142 | 9346 |
| Scala | TryFind | 0 | 0 | 3 | 17 | 111 | 256 | 1863 | 4111 | 32318 | 
| F# | OfSeq | 0 | 0 | 3 | 30 | 146 | 219 | 1321 | 3080 | 19160 |
| Scala | OfSeq | 0 | 0 | 5 | 49 | 163 | 387 | 2516 | 5347 | 43827 | 

Platforms tested: Dotnet version: 5.0.301, Scala code runner version 2.13.6-20210529-211702.

Note that most of the optimisation work I have done is around the "Get" method given my use case (lots of reads, fewer but still significant write load with large collections 500,000+ items).

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
