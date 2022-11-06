module FSharp.HashCollections.Benchmarks.Program

open BenchmarkDotNet.Running

[<EntryPoint>]
let main argv =
    let summary = BenchmarkSwitcher.FromAssembly(typeof<ReadBenchmarks>.Assembly).Run(argv)
    0

