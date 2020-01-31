# FSharp.HashCollections

Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations across the .NET ecosystem and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented using a standard HAMT (Hash Mapped Array Trie).

[![NuGet Badge](http://img.shields.io/nuget/v/FSharp.HashCollections.svg?style=flat)](https://www.nuget.org/packages/FSharp.HashCollections)

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
| TryFind | O(log32n) |
| Add | O(log32n) |
| Remove | O(log32n) |
| Count | O(1) |

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


|                           Method | CollectionSize |        Mean |    Error |   StdDev |      Median |
|--------------------------------- |--------------- |------------:|---------:|---------:|------------:|
|                       GetHashMap |             10 |    11.00 ns | 0.073 ns | 0.069 ns |    10.96 ns |
|                GetImToolsHashMap |             10 |    29.02 ns | 0.536 ns | 0.501 ns |    29.09 ns |
|                     GetFSharpMap |             10 |    41.20 ns | 0.801 ns | 0.823 ns |    41.37 ns |
|                GetFSharpXHashMap |             10 |   101.17 ns | 1.945 ns | 1.819 ns |   101.39 ns |
| GetSystemCollectionsImmutableMap |             10 |    24.58 ns | 0.071 ns | 0.055 ns |    24.60 ns |
|         GetFSharpDataAdaptiveMap |             10 |    21.83 ns | 0.020 ns | 0.019 ns |    21.82 ns |
|                       GetHashMap |            100 |    13.31 ns | 0.019 ns | 0.017 ns |    13.31 ns |
|                GetImToolsHashMap |            100 |    40.64 ns | 0.458 ns | 0.428 ns |    40.88 ns |
|                     GetFSharpMap |            100 |    73.72 ns | 1.434 ns | 1.342 ns |    73.72 ns |
|                GetFSharpXHashMap |            100 |   108.59 ns | 1.023 ns | 0.957 ns |   108.46 ns |
| GetSystemCollectionsImmutableMap |            100 |    36.85 ns | 0.251 ns | 0.235 ns |    36.93 ns |
|         GetFSharpDataAdaptiveMap |            100 |    43.33 ns | 0.010 ns | 0.009 ns |    43.33 ns |
|                       GetHashMap |           1000 |    11.32 ns | 0.038 ns | 0.035 ns |    11.30 ns |
|                GetImToolsHashMap |           1000 |    66.25 ns | 1.268 ns | 1.604 ns |    65.30 ns |
|                     GetFSharpMap |           1000 |   108.31 ns | 0.645 ns | 0.539 ns |   108.33 ns |
|                GetFSharpXHashMap |           1000 |   113.16 ns | 1.001 ns | 0.936 ns |   113.65 ns |
| GetSystemCollectionsImmutableMap |           1000 |    53.02 ns | 0.099 ns | 0.093 ns |    53.04 ns |
|         GetFSharpDataAdaptiveMap |           1000 |    65.17 ns | 0.326 ns | 0.305 ns |    65.31 ns |
|                       GetHashMap |         100000 |    23.24 ns | 0.035 ns | 0.033 ns |    23.24 ns |
|                GetImToolsHashMap |         100000 |   199.14 ns | 0.453 ns | 0.402 ns |   199.04 ns |
|                     GetFSharpMap |         100000 |   198.27 ns | 1.502 ns | 1.405 ns |   197.49 ns |
|                GetFSharpXHashMap |         100000 |   144.64 ns | 1.134 ns | 1.061 ns |   144.78 ns |
| GetSystemCollectionsImmutableMap |         100000 |   143.96 ns | 0.092 ns | 0.086 ns |   143.93 ns |
|         GetFSharpDataAdaptiveMap |         100000 |   141.12 ns | 0.051 ns | 0.043 ns |   141.11 ns |
|                       GetHashMap |         500000 |    69.74 ns | 0.181 ns | 0.160 ns |    69.80 ns |
|                GetImToolsHashMap |         500000 |   511.80 ns | 1.380 ns | 1.291 ns |   511.30 ns |
|                     GetFSharpMap |         500000 |   303.07 ns | 3.629 ns | 3.394 ns |   301.08 ns |
|                GetFSharpXHashMap |         500000 |   229.01 ns | 0.802 ns | 0.711 ns |   229.16 ns |
| GetSystemCollectionsImmutableMap |         500000 |   319.15 ns | 1.387 ns | 1.298 ns |   318.45 ns |
|         GetFSharpDataAdaptiveMap |         500000 |   303.75 ns | 0.156 ns | 0.146 ns |   303.77 ns |
|                       GetHashMap |         750000 |    99.19 ns | 0.189 ns | 0.167 ns |    99.19 ns |
|                GetImToolsHashMap |         750000 |   617.97 ns | 1.185 ns | 1.109 ns |   618.14 ns |
|                     GetFSharpMap |         750000 |   405.86 ns | 7.103 ns | 6.644 ns |   404.11 ns |
|                GetFSharpXHashMap |         750000 |   351.62 ns | 0.796 ns | 0.745 ns |   351.69 ns |
| GetSystemCollectionsImmutableMap |         750000 |   407.29 ns | 0.302 ns | 0.283 ns |   407.26 ns |
|         GetFSharpDataAdaptiveMap |         750000 |   348.77 ns | 0.198 ns | 0.175 ns |   348.73 ns |
|                       GetHashMap |        1000000 |   106.21 ns | 0.156 ns | 0.130 ns |   106.17 ns |
|                GetImToolsHashMap |        1000000 |   716.39 ns | 0.497 ns | 0.415 ns |   716.42 ns |
|                     GetFSharpMap |        1000000 |   465.08 ns | 8.363 ns | 7.823 ns |   468.86 ns |
|                GetFSharpXHashMap |        1000000 |   346.15 ns | 0.614 ns | 0.545 ns |   346.10 ns |
| GetSystemCollectionsImmutableMap |        1000000 |   472.67 ns | 2.598 ns | 2.430 ns |   471.20 ns |
|         GetFSharpDataAdaptiveMap |        1000000 |   392.68 ns | 1.090 ns | 0.966 ns |   392.89 ns |
|                       GetHashMap |        5000000 |   134.10 ns | 0.031 ns | 0.026 ns |   134.10 ns |
|                GetImToolsHashMap |        5000000 | 1,263.54 ns | 0.581 ns | 0.515 ns | 1,263.49 ns |
|                     GetFSharpMap |        5000000 |   839.73 ns | 0.659 ns | 0.617 ns |   839.72 ns |
|                GetFSharpXHashMap |        5000000 |   379.43 ns | 1.143 ns | 1.070 ns |   379.46 ns |
| GetSystemCollectionsImmutableMap |        5000000 |   880.08 ns | 0.462 ns | 0.409 ns |   880.12 ns |
|         GetFSharpDataAdaptiveMap |        5000000 |   610.11 ns | 0.837 ns | 0.783 ns |   609.90 ns |
|                       GetHashMap |       10000000 |   149.55 ns | 0.029 ns | 0.026 ns |   149.54 ns |
|                GetImToolsHashMap |       10000000 | 1,538.42 ns | 0.990 ns | 0.877 ns | 1,538.37 ns |
|                     GetFSharpMap |       10000000 | 1,024.58 ns | 1.056 ns | 0.988 ns | 1,024.54 ns |
|                GetFSharpXHashMap |       10000000 |   384.30 ns | 0.744 ns | 0.696 ns |   384.38 ns |
| GetSystemCollectionsImmutableMap |       10000000 | 1,031.06 ns | 0.547 ns | 0.511 ns | 1,031.02 ns |
|         GetFSharpDataAdaptiveMap |       10000000 |   705.51 ns | 0.449 ns | 0.398 ns |   705.42 ns |
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
