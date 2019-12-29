// Learn more about F# at http://fsharp.org
module Program 

open System

open System.Collections.Generic
open System.Diagnostics
open HashTrie.FSharp
open System.Collections.Concurrent

let testSize = 5_000_000
let amountOfTimesToGetTest = 200

let preparedData = Array.init testSize id

let testShift() = 
    let size = 65535
    let resultUint16 = Array.zeroCreate size
    let resultUint32 = Array.zeroCreate size

    for i = 0 to resultUint16.Length - 1 do
        resultUint16.[i] <- uint16 i
    
    for i = 0 to resultUint32.Length - 1 do
        resultUint32.[i] <- uint32 i

    let uint16Sw = Stopwatch.StartNew()
    
    for i = 0 to amountOfTimesToGetTest * 20 - 1 do
        for i = 0 to resultUint16.Length - 1 do
            resultUint16.[i] <- resultUint16.[i] >>> 1
    
    uint16Sw.Stop()

    let uint32Sw = Stopwatch.StartNew()
    
    for i = 0 to amountOfTimesToGetTest * 20 - 1 do
        for i = 0 to resultUint32.Length - 1 do
            resultUint32.[i] <- resultUint32.[i] >>> 1
    
    uint32Sw.Stop()
    
    printfn "Time taken [Uint16: %i; Uint32 : %i]" uint16Sw.ElapsedMilliseconds uint32Sw.ElapsedMilliseconds

type Comparer = 
    static member inline GetHashCode (o: int32) = o
    static member inline CheckEquality (o1: int32, o2: int32) = o1.Equals(o2)

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
    printfn "Total time to read per get: %f, PerCall: %f" 
        (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))
        (float (amountOfTimesToGetTest * testSize) / readSw.Elapsed.TotalMilliseconds)

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
    printfn "Total time to read per get: %f" 
        (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))

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

    printfn "Total time to read per get: %f" 
        (readSw.Elapsed.TotalMilliseconds / float (testSize * amountOfTimesToGetTest))

[<EntryPoint>]
let main argv =
    testHashTrie()
    // testMap()
    // testConcurrentDict()
    0 // return an integer exit code
