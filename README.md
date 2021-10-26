# FSharp.HashCollections

Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations across the .NET ecosystem and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented using a standard HAMT (Hash Mapped Array Trie).

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
```
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
```
open FSharp.HashCollections
let hashMapResult = HashSet.empty |> HashSet.add k |> HashSet.contains k // Result is true
```

## Equality customisation

By default any "empty" seeded HashSet/Map uses F# HashIdentity.Structural comparison to determine equality and calculate hashcodes. This is in line with the F# collection types.

In addition all collection types allow custom equality to be assigned if required. The equality is encoded as a type on the collection so equality and hashing operations are consistent. Same collection types with different equality comparers can not be used interchangeably by operations provided.  To use:

```
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

```
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
.NET SDK=5.0.301
  [Host]     : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT DEBUG
  DefaultJob : .NET 5.0.7 (5.0.721.25508), X64 RyuJIT


```
|                           Method | CollectionSize |        Mean |    Error |   StdDev |
|--------------------------------- |--------------- |------------:|---------:|---------:|
|                       **GetHashMap** |             **10** |    **11.27 ns** | **0.059 ns** | **0.052 ns** |
|                GetImToolsHashMap |             10 |    30.78 ns | 0.573 ns | 0.508 ns |
|                     GetFSharpMap |             10 |    38.90 ns | 0.535 ns | 0.474 ns |
|                GetFSharpXHashMap |             10 |    99.11 ns | 1.295 ns | 1.211 ns |
| GetSystemCollectionsImmutableMap |             10 |    22.65 ns | 0.201 ns | 0.178 ns |
|         GetFSharpDataAdaptiveMap |             10 |    21.06 ns | 0.187 ns | 0.175 ns |
|               GetFSharpxChampMap |             10 |    75.58 ns | 1.474 ns | 1.514 ns |
|                       **GetHashMap** |            **100** |    **15.27 ns** | **0.106 ns** | **0.099 ns** |
|                GetImToolsHashMap |            100 |    44.76 ns | 0.770 ns | 0.824 ns |
|                     GetFSharpMap |            100 |    70.82 ns | 0.638 ns | 0.565 ns |
|                GetFSharpXHashMap |            100 |   110.33 ns | 1.877 ns | 1.664 ns |
| GetSystemCollectionsImmutableMap |            100 |    34.41 ns | 0.377 ns | 0.353 ns |
|         GetFSharpDataAdaptiveMap |            100 |    42.18 ns | 0.533 ns | 0.499 ns |
|               GetFSharpxChampMap |            100 |    95.72 ns | 1.423 ns | 1.261 ns |
|                       **GetHashMap** |           **1000** |    **12.27 ns** | **0.086 ns** | **0.081 ns** |
|                GetImToolsHashMap |           1000 |    66.92 ns | 0.676 ns | 0.565 ns |
|                     GetFSharpMap |           1000 |   110.07 ns | 2.190 ns | 2.689 ns |
|                GetFSharpXHashMap |           1000 |   119.27 ns | 0.937 ns | 0.877 ns |
| GetSystemCollectionsImmutableMap |           1000 |    51.20 ns | 0.512 ns | 0.479 ns |
|         GetFSharpDataAdaptiveMap |           1000 |    65.18 ns | 0.827 ns | 0.773 ns |
|               GetFSharpxChampMap |           1000 |    96.31 ns | 1.018 ns | 0.902 ns |
|                       **GetHashMap** |         **100000** |    **27.10 ns** | **0.261 ns** | **0.244 ns** |
|                GetImToolsHashMap |         100000 |   192.91 ns | 1.385 ns | 1.295 ns |
|                     GetFSharpMap |         100000 |   202.18 ns | 3.977 ns | 3.720 ns |
|                GetFSharpXHashMap |         100000 |   142.92 ns | 1.867 ns | 1.655 ns |
| GetSystemCollectionsImmutableMap |         100000 |   143.54 ns | 1.211 ns | 1.133 ns |
|         GetFSharpDataAdaptiveMap |         100000 |   138.09 ns | 1.176 ns | 1.100 ns |
|               GetFSharpxChampMap |         100000 |   161.99 ns | 2.703 ns | 2.529 ns |
|                       **GetHashMap** |         **500000** |    **75.27 ns** | **0.464 ns** | **0.434 ns** |
|                GetImToolsHashMap |         500000 |   503.84 ns | 4.429 ns | 3.926 ns |
|                     GetFSharpMap |         500000 |   319.49 ns | 6.054 ns | 5.663 ns |
|                GetFSharpXHashMap |         500000 |   237.83 ns | 2.523 ns | 2.360 ns |
| GetSystemCollectionsImmutableMap |         500000 |   320.95 ns | 3.004 ns | 2.810 ns |
|         GetFSharpDataAdaptiveMap |         500000 |   303.34 ns | 2.013 ns | 1.883 ns |
|               GetFSharpxChampMap |         500000 |   169.03 ns | 1.864 ns | 1.744 ns |
|                       **GetHashMap** |         **750000** |   **108.07 ns** | **0.666 ns** | **0.623 ns** |
|                GetImToolsHashMap |         750000 |   608.60 ns | 3.202 ns | 2.838 ns |
|                     GetFSharpMap |         750000 |   406.83 ns | 4.744 ns | 4.437 ns |
|                GetFSharpXHashMap |         750000 |   351.69 ns | 1.921 ns | 1.797 ns |
| GetSystemCollectionsImmutableMap |         750000 |   409.93 ns | 2.075 ns | 1.941 ns |
|         GetFSharpDataAdaptiveMap |         750000 |   351.97 ns | 1.888 ns | 1.766 ns |
|               GetFSharpxChampMap |         750000 |   173.13 ns | 2.644 ns | 2.473 ns |
|                       **GetHashMap** |        **1000000** |   **114.22 ns** | **0.264 ns** | **0.247 ns** |
|                GetImToolsHashMap |        1000000 |   678.73 ns | 5.430 ns | 5.080 ns |
|                     GetFSharpMap |        1000000 |   472.68 ns | 3.494 ns | 3.097 ns |
|                GetFSharpXHashMap |        1000000 |   340.27 ns | 1.778 ns | 1.663 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   468.81 ns | 4.030 ns | 3.770 ns |
|         GetFSharpDataAdaptiveMap |        1000000 |   392.71 ns | 2.035 ns | 1.903 ns |
|               GetFSharpxChampMap |        1000000 |   178.21 ns | 2.594 ns | 2.426 ns |
|                       **GetHashMap** |        **5000000** |   **146.63 ns** | **0.552 ns** | **0.516 ns** |
|                GetImToolsHashMap |        5000000 | 1,095.06 ns | 4.782 ns | 4.473 ns |
|                     GetFSharpMap |        5000000 |   803.83 ns | 4.079 ns | 3.816 ns |
|                GetFSharpXHashMap |        5000000 |   374.48 ns | 2.256 ns | 2.110 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   775.52 ns | 5.161 ns | 4.828 ns |
|         GetFSharpDataAdaptiveMap |        5000000 |   556.44 ns | 2.809 ns | 2.628 ns |
|               GetFSharpxChampMap |        5000000 |   357.66 ns | 2.265 ns | 2.119 ns |
|                       **GetHashMap** |       **10000000** |   **163.68 ns** | **1.010 ns** | **0.944 ns** |
|                GetImToolsHashMap |       10000000 | 1,317.78 ns | 6.694 ns | 5.934 ns |
|                     GetFSharpMap |       10000000 |   935.86 ns | 4.086 ns | 3.822 ns |
|                GetFSharpXHashMap |       10000000 |   401.53 ns | 2.404 ns | 2.248 ns |
| GetSystemCollectionsImmutableMap |       10000000 |   909.51 ns | 2.906 ns | 2.576 ns |
|         GetFSharpDataAdaptiveMap |       10000000 |   655.68 ns | 2.443 ns | 2.040 ns |
|               GetFSharpxChampMap |       10000000 |   358.91 ns | 1.482 ns | 1.314 ns |


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
