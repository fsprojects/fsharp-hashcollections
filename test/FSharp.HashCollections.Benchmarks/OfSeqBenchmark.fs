namespace FSharp.HashCollections.Benchmarks

open System
open FSharp.HashCollections
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open FSharpx.Collections
open System.Collections.Generic
open ImTools

type OfSeqBenchmark() = 
    
    let mutable hashMap = FSharp.HashCollections.HashMap.empty
    let mutable fsharpMap = Map.empty
    let mutable fsharpDataAdaptiveMap = FSharp.Data.Adaptive.HashMap.Empty
    let mutable fsharpXHashMap = FSharpx.Collections.PersistentHashMap.empty
    let mutable systemImmutableMap = System.Collections.Immutable.ImmutableDictionary.Empty
    let mutable fsharpXChampMap = FSharpx.Collections.Experimental.ChampHashMap<int, int>()
    let mutable preppedData = Array.zeroCreate 0

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

    [<GlobalSetup>]
    member this.OfSeqSetup() = this.PrepData()

    [<Benchmark>]
    member this.OfSeqHashMap() = hashMap <- FSharp.HashCollections.HashMap.ofSeq preppedData

    [<Benchmark>]
    member this.OfSeqFSharpXMap() =
        fsharpXHashMap <- FSharpx.Collections.PersistentHashMap.ofSeq (preppedData |> Seq.map (fun (KeyValue(kv)) -> kv))

    [<Benchmark>]
    member this.OfSeqFSharpAdaptiveMap() =
        fsharpDataAdaptiveMap <- FSharp.Data.Adaptive.HashMap.ofSeq (preppedData |> Seq.map (fun (KeyValue(kv)) -> kv))    

    [<Benchmark>]
    member this.OfSeqSystemCollectionsImmutableMap() = systemImmutableMap <- System.Collections.Immutable.ImmutableDictionary.CreateRange(preppedData)

    [<Benchmark>]
    member this.OfSeqFSharpxChampMap() = fsharpXChampMap <- preppedData |>  FSharpx.Collections.Experimental.ChampHashMap.ofSeq (fun x -> x.Key) (fun x -> x.Value)

    [<Benchmark>]
    member this.OfSeqLangExtMap() = LanguageExt.HashMap.Empty.AddRange(preppedData)