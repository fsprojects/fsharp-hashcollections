module HashTrie.FSharp.Unit.HashTrieTest

open Expecto
open FsCheck
open HashTrie.FSharp

// List of actions to generate
type KvAction<'k, 'v> = 
    | Add of k: 'k * v: 'v
    | Remove of k: 'k

let mapAndHashTrieAreTheSameAfterActions (actions: KvAction<'tk, 'tv> list) = 
    
    let mutable mapToTest = Map.empty
    let mutable hashTrieToTest = HashTrie.empty
    
    for action in actions do 
        match action with
        | Add(k, v) -> 
            mapToTest <- mapToTest |> Map.add k v
            hashTrieToTest <- hashTrieToTest |> HashTrie.add k v
        | Remove k ->
            mapToTest <- mapToTest |> Map.remove k
            hashTrieToTest <- hashTrieToTest |> HashTrie.remove k
        Expect.equal 
            (hashTrieToTest |> HashTrie.toSeq |> set) 
            (mapToTest |> Map.toSeq |> Seq.map (fun (x, y) -> struct (x, y)) |> set)
            "Hash Trie and Map don't contain same data"

let toVOption i = match i with | Some(x) -> ValueSome x | None -> ValueNone

let mapAndHashTrieHaveSameGetValue (actions: KvAction<'tk, 'tv> list) = 
    
    let mutable mapToTest = Map.empty
    let mutable hashTrieToTest = HashTrie.empty
    
    for action in actions do 
        let mutable key = Unchecked.defaultof<_>
        match action with
        | Add(k, v) -> 
            mapToTest <- mapToTest |> Map.add k v
            hashTrieToTest <- hashTrieToTest |> HashTrie.add k v
            key <- k
        | Remove k ->
            mapToTest <- mapToTest |> Map.remove k
            hashTrieToTest <- hashTrieToTest |> HashTrie.remove k
            key <- k
        let mapResult = mapToTest |> Map.tryFind key |> toVOption
        let hashTrieResult = hashTrieToTest |> HashTrie.tryFind key
        Expect.equal hashTrieResult mapResult "Key update did not hold"

let buildPropertyTest testName testFunction = 
    let config = { Config.QuickThrowOnFailure with StartSize = 0; EndSize = 100000; MaxTest = 1000 }    
    testCase testName <| fun () -> Check.One(config, testFunction)

let [<Tests>] tests = 
    testList 
        "Hash Trie Property Tests"
        [ 
          testCase
            "Adding 3 k-v pairs"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (32u,0L); Add (1u,0L); Add (0u,0L) ])

          testCase
            "Adding another close approximate 3 kv-pairs with a hash collision from 0 and -1 keys"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Add (-1L,0); Add (0L,0) ]) 
          
          testCase
            "Map contains keys of the same hash (Hash = 0 for both 0 and -1"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add(0L, 5); Add(-1L, 6) ] )

          testCase
            "Hash collision node in tree; then one is removed with a collision"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Add (-1L,0); Add (0L,0); Remove 0L ])

          testCase
            "Add and remove value with same hash"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (0L,0); Remove 1L ])
        
          buildPropertyTest
            "Map and HashTrie behave the same on Add and Remove"
            (fun (x: KvAction<int64, int> list) -> mapAndHashTrieAreTheSameAfterActions x)

          buildPropertyTest
            "Map and HashTrie always have the same Get result"
            (fun (x: KvAction<int64, int> list) -> mapAndHashTrieHaveSameGetValue x)
        ]