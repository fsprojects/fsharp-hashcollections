// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open FSharp.HashCollections
open System.Diagnostics
open System.Collections.Generic

// Define a function to construct a message to print
let runTest testSize =
    let sourceData = Array.init testSize id

    let populateSw = Stopwatch.StartNew()
    let map = HashMap.ofSeq (sourceData |> Seq.map (fun x -> KeyValuePair.Create(int64 x, int64 x)))
    populateSw.Stop()

    let resultArray : int64 voption[] = Array.zeroCreate testSize
    let sw = Stopwatch.StartNew()

    for i = 0 to resultArray.Length - 1 do
        resultArray.[i] <- map |> HashMap.tryFind (int64 i)

    sw.Stop()

    printfn $"FSharp Result [TestSize: {testSize}, GetTime: {sw.ElapsedMilliseconds}, FromSeqTime: {populateSw.ElapsedMilliseconds}]"

[<EntryPoint>]
let main argv =
    runTest 100
    runTest 1000
    runTest 10000
    runTest 100000
    runTest 500000
    runTest 1000000
    runTest 5000000
    runTest 10000000
    0