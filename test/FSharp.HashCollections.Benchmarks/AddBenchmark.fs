namespace FSharp.HashCollections.Benchmarks

open System
open FSharp.HashCollections
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open FSharpx.Collections
open System.Collections.Generic
open ImTools

module private AddBenchmarkConstants = 
    let [<Literal>] OperationsPerInvoke = 50
open AddBenchmarkConstants

type AddBenchmark() = 

    let mutable hashMap = FSharp.HashCollections.HashMap.empty
    let mutable fsharpMap = Map.empty
    let mutable fsharpDataAdaptiveMap = FSharp.Data.Adaptive.HashMap.Empty
    let mutable fsharpXHashMap = FSharpx.Collections.PersistentHashMap.empty
    let mutable systemImmutableMap = System.Collections.Immutable.ImmutableDictionary.Empty
    let mutable fsharpXChampMap = FSharpx.Collections.Experimental.ChampHashMap<int, int>()
    let mutable preppedData = Array.zeroCreate 0

    let elementsToAdd = 
        let a = Array.zeroCreate OperationsPerInvoke
        let r = Random()
        for i = 0 to a.Length - 1 do
            a.[i] <- KeyValuePair<_, _>(r.Next(), r.Next())
        a

    [<Params(1000, 100_000, 500_000, 750_000, 1_000_000, 5_000_000, 10_000_000)>]
    member val public CollectionSize = 0 with get, set

    member this.PrepData() = 
        if preppedData.Length <> this.CollectionSize
        then
            let r = Random()
            preppedData <- Array.zeroCreate this.CollectionSize
            
            let mutable c = 0
            let mutable s = Set.empty
            while c < preppedData.Length do
                let i = r.Next()
                if s |> Set.contains i
                then ()
                else
                    s <- s |> Set.add i
                    c <- c + 1

            if s.Count <> this.CollectionSize then failwithf "Bug in startup data generation"
            use e = (Set.toSeq s).GetEnumerator()
            for i = 0 to preppedData.Length - 1 do
                e.MoveNext() |> ignore
                preppedData.[i] <- KeyValuePair<_, _>(e.Current, e.Current)

    [<GlobalSetup(Target = "AddHashMap")>] 
    member this.SetupAddHashMap() = 
        this.PrepData()
        hashMap <- FSharp.HashCollections.HashMap.ofSeq preppedData
        if hashMap |> HashMap.count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddHashMap() =
        let mutable hashMap = hashMap
        for i in elementsToAdd do hashMap <- hashMap |> FSharp.HashCollections.HashMap.add i.Key i.Value

    [<GlobalSetup(Target = "AddToFSharpMap")>]  
    member this.SetupAddToFSharpMap() = 
        this.PrepData()
        fsharpMap <- preppedData |> Seq.fold (fun s (KeyValue(k, v)) -> s |> Map.add k v) Map.empty
        if fsharpMap.Count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddToFSharpMap() =
        let mutable fsharpMap = fsharpMap
        for i in elementsToAdd do fsharpMap <- fsharpMap |> Map.add i.Key i.Value

    [<GlobalSetup(Target = "AddToFSharpAdaptiveMap")>] 
    member this.SetupAddToFSharpAdaptiveMap() = 
        this.PrepData()
        this.OfSeqFSharpAdaptiveMap()
        if fsharpDataAdaptiveMap.Count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddToFSharpAdaptiveMap() =
        let mutable fsharpDataAdaptiveMap = fsharpDataAdaptiveMap
        for i in elementsToAdd do fsharpDataAdaptiveMap <- fsharpDataAdaptiveMap |> FSharp.Data.Adaptive.HashMap.add i.Key i.Value

    [<GlobalSetup(Target = "AddToFSharpXMap")>] 
    member this.SetupAddToFSharpXMap() = 
        this.PrepData()
        this.OfSeqFSharpXMap()
        if fsharpXHashMap.Count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddToFSharpXMap() =
        let mutable fsharpXHashMap = fsharpXHashMap
        for i in elementsToAdd do fsharpXHashMap <- fsharpXHashMap |> FSharpx.Collections.PersistentHashMap.add i.Key i.Value

    [<GlobalSetup(Target = "AddToSystemCollectionsImmutableMap")>]
    member this.SetupAddToSystemCollectionsImmutableMap() = 
        this.PrepData()
        this.OfSeqSystemCollectionsImmutableMap()
        if systemImmutableMap.Count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddToSystemCollectionsImmutableMap() =
        let mutable systemImmutableMap = systemImmutableMap
        for i in elementsToAdd do systemImmutableMap <- systemImmutableMap.Add(i.Key, i.Value)

    [<GlobalSetup(Target = "AddToFsharpxChampMap")>]
    member this.SetupAddToFsharpxChampMap() = 
        this.PrepData()
        this.OfSeqSystemCollectionsImmutableMap()
        if systemImmutableMap.Count <> this.CollectionSize then failwithf "Not properly initialised"

    [<Benchmark(OperationsPerInvoke = OperationsPerInvoke)>]
    member this.AddToFsharpxChampMap() =
        let mutable fsharpXChampMap = fsharpXChampMap
        for i in elementsToAdd do fsharpXChampMap <- FSharpx.Collections.Experimental.ChampHashMap.add fsharpXChampMap i.Key i.Value

    // OfSeq helpers
    member this.OfSeqHashMap() = hashMap <- FSharp.HashCollections.HashMap.ofSeq preppedData

    member this.OfSeqFSharpXMap() =
        fsharpXHashMap <- FSharpx.Collections.PersistentHashMap.ofSeq (preppedData |> Seq.map (fun (KeyValue(kv)) -> kv))

    member this.OfSeqFSharpAdaptiveMap() =
        fsharpDataAdaptiveMap <- FSharp.Data.Adaptive.HashMap.ofSeq (preppedData |> Seq.map (fun (KeyValue(kv)) -> kv))    

    member this.OfSeqSystemCollectionsImmutableMap() = systemImmutableMap <- System.Collections.Immutable.ImmutableDictionary.CreateRange(preppedData)

    member this.OfSeqFsharpxChampMap() = fsharpXChampMap <- preppedData |> FSharpx.Collections.Experimental.ChampHashMap.ofSeq (fun x -> x.Key) (fun x -> x.Value)