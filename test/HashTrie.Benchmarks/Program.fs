// Learn more about F# at http://fsharp.org
module Program 

open System

open System.Collections.Generic
open System.Diagnostics
open HashTrie.FSharp
open System.Collections.Concurrent

let testSize = 5000000
let amountOfTimesToGetTest = 200

let preparedData = Array.init testSize id

let testHashTrie() = 
    
    printfn "Inserting into trie"    
    let insertSw = Stopwatch.StartNew()
    let mutable data = HashTrie.empty
    for d in preparedData do
        data <- data |> HashTrie.add d d
    
    insertSw.Stop()

    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    let readSw = Stopwatch.StartNew()
    for i = 0 to amountOfTimesToGetTest - 1 do
        for i = 0 to testSize - 1 do
            data |> HashTrie.tryFind i |> ignore
    readSw.Stop()
    printfn "Total time to read per get: %f" (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))

let testMap() = 
    
    let insertSw = Stopwatch.StartNew()
    let mutable data = Map.empty
    for d in preparedData do
        data <- data |> Map.add d d
    
    insertSw.Stop()
    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    let readSw = Stopwatch.StartNew()
    for i = 0 to amountOfTimesToGetTest - 1 do
        for i = 0 to testSize - 1 do
            data |> Map.tryFind i |> ignore
    readSw.Stop()
    printfn "Total time to read per get: %f" (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))

let testConcurrentDict() = 
    
    let insertSw = Stopwatch.StartNew()
    let mutable data = new ConcurrentDictionary<_, _>()
    for d in preparedData do
        data.[d] <- d
    
    insertSw.Stop()
    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    let readSw = Stopwatch.StartNew()
    for i = 0 to amountOfTimesToGetTest - 1 do
        for i = 0 to testSize - 1 do
            data.TryGetValue(i) |> ignore
    readSw.Stop()

    printfn "Total time to read per get: %f" (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))

[<EntryPoint>]
let main argv =
    testHashTrie()
    testMap()
    testConcurrentDict()
    0 // return an integer exit code
