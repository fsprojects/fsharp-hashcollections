namespace FSharp.HashCollections.Benchmarks

open System
open FSharp.HashCollections
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open FSharpx.Collections
open ImTools

module private AddBenchmarkConstants = 
    let [<Literal>] OperationsPerInvoke = 20
open AddBenchmarkConstants

type AddBenchmark() = 

    let mutable hashMap = FSharp.HashCollections.HashMap.empty
    let mutable fsharpMap = Map.empty
    let mutable fsharpDataAdaptiveMap = FSharp.Data.Adaptive.HashMap.Empty
    let mutable fsharpXHashMap = FSharpx.Collections.PersistentHashMap.empty
    let mutable systemImmutableMap = System.Collections.Immutable.ImmutableDictionary.Empty

    [<Params(1000, 100_000, 500_000, 750_000, 1_000_000, 5_000_000, 10_000_000)>]
    member val public CollectionSize = 0 with get, set

    // [<GlobalSetup(Target = "AddToHashMap")>]
    // member this.SetupHashMap() = 
    //     hashMap <- FSharp.HashCollections.HashMap.empty
    //     for i = 0 to this.CollectionSize - 1 do
    //         hashMap <- hashMap |> FSharp.HashCollections.HashMap.add i i

    [<Benchmark>]
    member this.AddToHashMap() =
        for i = 0 to this.CollectionSize - 1 do
            hashMap <- hashMap |> FSharp.HashCollections.HashMap.add i i

    // [<GlobalSetup(Target = "AddToFSharpMap")>]
    // member this.SetupFSharpMap() = 
    //     fsharpMap <- Map.empty
    //     for i = 0 to this.CollectionSize - 1 do
    //         fsharpMap <- fsharpMap |> Map.add i i

    [<Benchmark>]
    member this.AddToFSharpMap() =
        for i = 0 to this.CollectionSize - 1 do
            fsharpMap <- fsharpMap |> Map.add i i

    // [<GlobalSetup(Target = "AddToFSharpAdaptiveMap")>]
    // member this.SetupFSharpAdaptiveMap() = 
    //     fsharpDataAdaptiveMap <- FSharp.Data.Adaptive.HashMap.Empty
    //     for i = 0 to this.CollectionSize - 1 do
    //         fsharpDataAdaptiveMap <- fsharpDataAdaptiveMap |> FSharp.Data.Adaptive.HashMap.add i i

    [<Benchmark>]
    member this.AddToFSharpAdaptiveMap() =
        for i = 0 to this.CollectionSize - 1 do
            fsharpDataAdaptiveMap <- fsharpDataAdaptiveMap |> FSharp.Data.Adaptive.HashMap.add i i

    // [<GlobalSetup(Target = "AddToFSharpXMap")>]
    // member this.SetupFSharpXMap() = 
    //     fsharpXHashMap <- FSharpx.Collections.PersistentHashMap.empty
    //     for i = 0 to this.CollectionSize - 1 do
    //         fsharpXHashMap <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.add i i

    [<Benchmark>]
    member this.AddToFSharpXMap() =
        for i = 0 to this.CollectionSize - 1 do
            fsharpXHashMap <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.add i i

    // [<GlobalSetup(Target = "AddToSystemCollectionsImmutableMap")>]
    // member this.SetupSystemCollectionsImmutableMap() = 
    //     systemImmutableMap <- System.Collections.Immutable.ImmutableDictionary.Empty
    //     for i = 0 to this.CollectionSize - 1 do
    //         systemImmutableMap <- systemImmutableMap.Add(i, i)

    [<Benchmark>]
    member this.AddToSystemCollectionsImmutableMap() =
        for i = 0 to this.CollectionSize - 1 do
            systemImmutableMap <- systemImmutableMap.Add(i, i)
        