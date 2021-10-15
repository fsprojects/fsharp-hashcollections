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

```
// * Summary *

BenchmarkDotNet=v0.12.0, OS=arch
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                           Method | CollectionSize |        Mean |    Error |   StdDev |
|--------------------------------- |--------------- |------------:|---------:|---------:|
|                       GetHashMap |             10 |    11.07 ns | 0.071 ns | 0.063 ns |
|                GetImToolsHashMap |             10 |    28.52 ns | 0.203 ns | 0.190 ns |
|                     GetFSharpMap |             10 |    39.55 ns | 0.540 ns | 0.505 ns |
|                GetFSharpXHashMap |             10 |   103.17 ns | 1.195 ns | 1.118 ns |
| GetSystemCollectionsImmutableMap |             10 |    24.38 ns | 0.318 ns | 0.298 ns |
|         GetFSharpDataAdaptiveMap |             10 |    21.88 ns | 0.088 ns | 0.082 ns |
|                       GetHashMap |            100 |    15.80 ns | 0.027 ns | 0.025 ns |
|                GetImToolsHashMap |            100 |    42.50 ns | 0.831 ns | 0.816 ns |
|                     GetFSharpMap |            100 |    70.69 ns | 1.385 ns | 1.595 ns |
|                GetFSharpXHashMap |            100 |   114.97 ns | 1.520 ns | 1.422 ns |
| GetSystemCollectionsImmutableMap |            100 |    36.47 ns | 0.349 ns | 0.327 ns |
|         GetFSharpDataAdaptiveMap |            100 |    43.15 ns | 0.098 ns | 0.082 ns |
|                       GetHashMap |           1000 |    12.83 ns | 0.017 ns | 0.014 ns |
|                GetImToolsHashMap |           1000 |    63.37 ns | 0.473 ns | 0.442 ns |
|                     GetFSharpMap |           1000 |   107.45 ns | 0.750 ns | 0.702 ns |
|                GetFSharpXHashMap |           1000 |   121.31 ns | 2.338 ns | 2.187 ns |
| GetSystemCollectionsImmutableMap |           1000 |    55.16 ns | 0.103 ns | 0.097 ns |
|         GetFSharpDataAdaptiveMap |           1000 |    66.20 ns | 0.463 ns | 0.433 ns |
|                       GetHashMap |         100000 |    26.30 ns | 0.011 ns | 0.008 ns |
|                GetImToolsHashMap |         100000 |   197.11 ns | 0.222 ns | 0.196 ns |
|                     GetFSharpMap |         100000 |   192.56 ns | 2.011 ns | 1.680 ns |
|                GetFSharpXHashMap |         100000 |   144.00 ns | 0.673 ns | 0.630 ns |
| GetSystemCollectionsImmutableMap |         100000 |   141.04 ns | 0.067 ns | 0.062 ns |
|         GetFSharpDataAdaptiveMap |         100000 |   141.15 ns | 0.867 ns | 0.811 ns |
|                       GetHashMap |         500000 |    71.60 ns | 0.108 ns | 0.090 ns |
|                GetImToolsHashMap |         500000 |   502.08 ns | 1.257 ns | 1.114 ns |
|                     GetFSharpMap |         500000 |   305.92 ns | 3.317 ns | 3.103 ns |
|                GetFSharpXHashMap |         500000 |   235.47 ns | 1.090 ns | 0.966 ns |
| GetSystemCollectionsImmutableMap |         500000 |   314.52 ns | 0.540 ns | 0.505 ns |
|         GetFSharpDataAdaptiveMap |         500000 |   301.50 ns | 1.213 ns | 1.135 ns |
|                       GetHashMap |         750000 |   100.81 ns | 0.036 ns | 0.032 ns |
|                GetImToolsHashMap |         750000 |   629.57 ns | 0.300 ns | 0.266 ns |
|                     GetFSharpMap |         750000 |   391.61 ns | 7.643 ns | 7.149 ns |
|                GetFSharpXHashMap |         750000 |   356.89 ns | 0.542 ns | 0.507 ns |
| GetSystemCollectionsImmutableMap |         750000 |   391.42 ns | 0.764 ns | 0.715 ns |
|         GetFSharpDataAdaptiveMap |         750000 |   354.98 ns | 0.241 ns | 0.225 ns |
|                       GetHashMap |        1000000 |   113.54 ns | 0.356 ns | 0.333 ns |
|                GetImToolsHashMap |        1000000 |   692.17 ns | 1.626 ns | 1.521 ns |
|                     GetFSharpMap |        1000000 |   469.54 ns | 2.890 ns | 2.703 ns |
|                GetFSharpXHashMap |        1000000 |   349.94 ns | 1.032 ns | 0.965 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   484.46 ns | 0.480 ns | 0.425 ns |
|         GetFSharpDataAdaptiveMap |        1000000 |   387.18 ns | 0.320 ns | 0.284 ns |
|                       GetHashMap |        5000000 |   142.23 ns | 0.050 ns | 0.047 ns |
|                GetImToolsHashMap |        5000000 | 1,282.78 ns | 0.596 ns | 0.558 ns |
|                     GetFSharpMap |        5000000 |   851.10 ns | 1.426 ns | 1.334 ns |
|                GetFSharpXHashMap |        5000000 |   375.16 ns | 0.812 ns | 0.719 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   850.68 ns | 0.405 ns | 0.359 ns |
|         GetFSharpDataAdaptiveMap |        5000000 |   608.75 ns | 0.250 ns | 0.233 ns |
|                       GetHashMap |       10000000 |   158.64 ns | 0.110 ns | 0.097 ns |
|                GetImToolsHashMap |       10000000 | 1,565.47 ns | 1.311 ns | 1.227 ns |
|                     GetFSharpMap |       10000000 | 1,041.04 ns | 1.536 ns | 1.436 ns |
|                GetFSharpXHashMap |       10000000 |   406.87 ns | 0.664 ns | 0.621 ns |
| GetSystemCollectionsImmutableMap |       10000000 | 1,016.19 ns | 2.372 ns | 2.218 ns |
|         GetFSharpDataAdaptiveMap |       10000000 |   702.30 ns | 2.080 ns | 1.946 ns |
```

### Add on HashMap

Scenario: Adding 50 elements on top of a pre-defined collection with a collection size as specified, average time of each of the 50 inserts:

```
// * Summary *

BenchmarkDotNet=v0.12.0, OS=arch
AMD Ryzen 7 3700X, 1 CPU, 16 logical and 8 physical cores
.NET Core SDK=3.1.100
  [Host]     : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT DEBUG
  DefaultJob : .NET Core 3.1.0 (CoreCLR 4.700.19.56402, CoreFX 4.700.19.56404), X64 RyuJIT


|                             Method | CollectionSize |       Mean |    Error |   StdDev |
|----------------------------------- |--------------- |-----------:|---------:|---------:|
|                         AddHashMap |           1000 |   274.3 ns |  4.49 ns |  4.20 ns |
|                     AddToFSharpMap |           1000 |   508.6 ns |  4.53 ns |  4.24 ns |
|             AddToFSharpAdaptiveMap |           1000 |   198.6 ns |  1.24 ns |  1.16 ns |
|                    AddToFSharpXMap |           1000 |   480.0 ns |  2.62 ns |  2.45 ns |
| AddToSystemCollectionsImmutableMap |           1000 |   682.4 ns |  9.43 ns |  8.36 ns |
|                         AddHashMap |         100000 |   402.1 ns |  7.97 ns |  9.48 ns |
|                     AddToFSharpMap |         100000 |   832.2 ns | 15.93 ns | 18.35 ns |
|             AddToFSharpAdaptiveMap |         100000 |   334.8 ns |  4.24 ns |  3.97 ns |
|                    AddToFSharpXMap |         100000 |   699.2 ns | 12.49 ns | 10.43 ns |
| AddToSystemCollectionsImmutableMap |         100000 | 1,149.2 ns | 11.48 ns | 10.74 ns |
|                         AddHashMap |         500000 |   485.1 ns |  5.18 ns |  4.60 ns |
|                     AddToFSharpMap |         500000 |   938.2 ns |  5.87 ns |  5.49 ns |
|             AddToFSharpAdaptiveMap |         500000 |   373.1 ns |  3.66 ns |  3.43 ns |
|                    AddToFSharpXMap |         500000 |   746.4 ns |  3.40 ns |  3.18 ns |
| AddToSystemCollectionsImmutableMap |         500000 | 1,254.3 ns |  2.30 ns |  2.15 ns |
|                         AddHashMap |         750000 |   461.4 ns |  3.86 ns |  3.61 ns |
|                     AddToFSharpMap |         750000 |   974.3 ns |  3.36 ns |  3.14 ns |
|             AddToFSharpAdaptiveMap |         750000 |   385.2 ns |  0.82 ns |  0.77 ns |
|                    AddToFSharpXMap |         750000 |   759.4 ns |  2.40 ns |  2.13 ns |
| AddToSystemCollectionsImmutableMap |         750000 | 1,302.9 ns |  4.51 ns |  3.99 ns |
|                         AddHashMap |        1000000 |   503.4 ns |  8.52 ns |  7.97 ns |
|                     AddToFSharpMap |        1000000 |   991.9 ns | 10.22 ns |  9.56 ns |
|             AddToFSharpAdaptiveMap |        1000000 |   412.3 ns |  8.11 ns | 11.63 ns |
|                    AddToFSharpXMap |        1000000 |   810.3 ns |  5.40 ns |  5.05 ns |
| AddToSystemCollectionsImmutableMap |        1000000 | 1,313.9 ns |  3.29 ns |  3.07 ns |
|                         AddHashMap |        5000000 |   511.3 ns |  2.47 ns |  2.31 ns |
|                     AddToFSharpMap |        5000000 | 1,096.8 ns |  2.31 ns |  2.16 ns |
|             AddToFSharpAdaptiveMap |        5000000 |   481.6 ns |  3.11 ns |  2.75 ns |
|                    AddToFSharpXMap |        5000000 |   815.0 ns |  5.84 ns |  5.46 ns |
| AddToSystemCollectionsImmutableMap |        5000000 | 1,516.8 ns |  2.19 ns |  2.05 ns |
|                         AddHashMap |       10000000 |   528.6 ns |  3.53 ns |  3.30 ns |
|                     AddToFSharpMap |       10000000 | 1,156.6 ns |  2.61 ns |  2.44 ns |
|             AddToFSharpAdaptiveMap |       10000000 |   503.4 ns |  4.18 ns |  3.91 ns |
|                    AddToFSharpXMap |       10000000 |   875.8 ns | 12.69 ns | 11.87 ns |
| AddToSystemCollectionsImmutableMap |       10000000 | 1,611.7 ns |  6.18 ns |  5.78 ns |
```

## An outside .NET ecosystem comparison.

This section is simply a guide to give a ballpark comparison figure on performance with implementations from other languages that have standard HAMT implementations for my own technical selection evaluation.

**TL;DR**: The "Get" method where performance is significantly better as the collection scales up. For example at 10,000,000 FSharp collection is approx 3.59 faster, and 1.73x faster at building the initial hashmap. 

| Lang | Operation | TestSize | 100 | 1000 | 10000 | 100000 | 500000 | 1000000 | 5000000 | 10000000 | 50000000 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
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
