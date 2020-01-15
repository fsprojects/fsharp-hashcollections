module FSharp.HashCollections.Benchmarks.Program

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let summary = BenchmarkRunner.Run(typeof<AddBenchmark>.Assembly)
    0

