// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open FSharp.HashCollections
open System.Diagnostics
open System.Collections.Generic

// Define a function to construct a message to print
let runTest testSize toPrint =
    let sourceData = Array.init testSize id

    let populateSw = Stopwatch.StartNew()
    let map = HashMap.ofSeq (sourceData |> Seq.map (fun x -> KeyValuePair.Create(int64 x, int64 x)))
    populateSw.Stop()

    let resultArray : int64 voption[] = Array.zeroCreate testSize
    let sw = Stopwatch.StartNew()

    for i = 0 to resultArray.Length - 1 do
        resultArray.[i] <- map |> HashMap.tryFind (int64 i)

    sw.Stop()

    if toPrint
    then printfn $"| {testSize} | {sw.ElapsedMilliseconds} | {populateSw.ElapsedMilliseconds} |"

    (sw.ElapsedMilliseconds, populateSw.ElapsedMilliseconds)

[<EntryPoint>]
let main argv =
    let testSizes = [
        100
        1000
        10000
        100000
        500000
        1000000
        5000000
        10000000
        50000000
    ]

    runTest 100 false |> ignore // Warmup
    
    let testHeader = String.Join(" | ", testSizes)
    printfn $"| Lang | Operation | TestSize | {testHeader} |"
    
    let testResults = [ 
        for testSize in testSizes do 
            yield runTest testSize false ]

    printfn "| F# | TryFind | %s |" (String.Join(" | ", (testResults |> Seq.map fst)))
    printfn "| F# | OfSeq | %s |" (String.Join(" | ", (testResults |> Seq.map snd)))
    
    0