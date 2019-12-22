// Learn more about F# at http://fsharp.org
module Program 

open System

open System.Collections.Generic
open System.Diagnostics
open HashTrie.FSharp

let testSize = 10000000
let preparedData = Array.init testSize id

// let insertSw = Stopwatch.StartNew()
// let mutable data = HashTrie.empty
// for d in preparedData do
//     data <- data |> HashTrie.add d d

// insertSw.Stop()
// printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

// let testReadHashTrie() =
//     printfn "Starting read test"
//     let readSw = Stopwatch.StartNew()
//     for i = 0 to 20 do
//     for i = 0 to testSize - 1 do
//         data |> HashTrie.get i |> ignore
//     readSw.Stop()

//     printfn "Total time to read: %i" readSw.ElapsedMilliseconds


let testHashTrie() = 
    
    printfn "Inserting into trie"    
    let insertSw = Stopwatch.StartNew()
    let mutable data = HashTrie.empty
    for d in preparedData do
        data <- data |> HashTrie.add d d
    
    insertSw.Stop()
    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    for i = 0 to 10 do
        let readSw = Stopwatch.StartNew()
        for i = 0 to testSize - 1 do
            data |> HashTrie.get i |> ignore
        readSw.Stop()
        printfn "Total time to read: %i" readSw.ElapsedMilliseconds

let testMap() = 
    
    let insertSw = Stopwatch.StartNew()
    let mutable data = Map.empty
    for d in preparedData do
        data <- data |> Map.add d d
    
    insertSw.Stop()
    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    for i = 0 to 20 do
        let readSw = Stopwatch.StartNew()
        for i = 0 to testSize - 1 do
            data |> Map.tryFind i |> ignore
        readSw.Stop()
        printfn "Total time to read: %i" readSw.ElapsedMilliseconds

let testDict() = 
    
    let insertSw = Stopwatch.StartNew()
    let mutable data = new Dictionary<_, _>()
    for d in preparedData do
        data.[d] <- d
    
    insertSw.Stop()
    printfn "Total time to insert: %i" insertSw.ElapsedMilliseconds

    let readSw = Stopwatch.StartNew()
    for i = 0 to testSize - 1 do
        data.TryGetValue(i) |> ignore
    readSw.Stop()

    printfn "Total time to read: %i" readSw.ElapsedMilliseconds

[<EntryPoint>]
let main argv =
    testHashTrie()
    //testMap()
    0 // return an integer exit code
