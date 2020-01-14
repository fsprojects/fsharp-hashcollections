module FSharp.HashCollections.HashSetTest

open Expecto
open FsCheck
open FSharp.HashCollections

// List of actions to generate
type SetAction<'k> = 
    | Add of k: 'k 
    | Remove of k: 'k

let inline setAndHashSetAreTheSameAfterActions (actions: SetAction<'tk> list) = 
    
    let mutable mapToTest = Set.empty
    let mutable hashTrieToTest = HashSet.empty
    
    for action in actions do 
        match action with
        | Add(k) -> 
            mapToTest <- mapToTest |> Set.add k
            hashTrieToTest <- hashTrieToTest |> HashSet.add k
        | Remove k ->
            mapToTest <- mapToTest |> Set.remove k
            hashTrieToTest <- hashTrieToTest |> HashSet.remove k
        Expect.equal 
            (hashTrieToTest |> HashSet.toSeq |> set) 
            (mapToTest |> Set.toSeq |> set)
            "Hash Trie and Map don't contain same data"

let inline setAndHashSetHaveSameContainsValue (actions: SetAction<'tk> list) = 
    
    let mutable mapToTest = Set.empty
    let mutable hashTrieToTest = HashSet.empty
    
    for action in actions do 
        let mutable key = Unchecked.defaultof<'tk>
        match action with
        | Add(k) -> 
            mapToTest <- mapToTest |> Set.add k
            hashTrieToTest <- hashTrieToTest |> HashSet.add k
            key <- k
        | Remove k ->
            mapToTest <- mapToTest |> Set.remove k
            hashTrieToTest <- hashTrieToTest |> HashSet.remove k
            key <- k
        let mapResult = mapToTest |> Set.contains key
        let hashTrieResult = hashTrieToTest |> HashSet.contains key
        Expect.equal hashTrieResult mapResult "Key update did not hold"

let inline setAndHashSetHaveSameCountAtAllTimes (actions: SetAction<'tk> list) = 
    let mutable mapToTest = Set.empty
    let mutable hashTrieToTest = HashSet.empty
    
    for action in actions do 
        let mutable key = Unchecked.defaultof<'tk>
        match action with
        | Add(k) -> 
            mapToTest <- mapToTest |> Set.add k
            hashTrieToTest <- hashTrieToTest |> HashSet.add k
            key <- k
        | Remove k ->
            mapToTest <- mapToTest |> Set.remove k
            hashTrieToTest <- hashTrieToTest |> HashSet.remove k
            key <- k
        let mapResult = mapToTest |> Set.count
        let hashTrieResult = hashTrieToTest |> HashSet.count
        Expect.equal hashTrieResult mapResult "Count isn't equal"

let buildPropertyTest testName (testFunction: SetAction<int64> list -> _) = 
    let config = { Config.QuickThrowOnFailure with StartSize = 0; EndSize = 100000; MaxTest = 100 }    
    testCase testName <| fun () -> Check.One(config, testFunction)

let inline generateLargeSizeMapTest() =
  testCase
    "Large map test of more than one depth"
    (fun () -> 
      let testData = Array.init 100000 id
      let result = testData |> Array.fold (fun s t -> s |> HashMap.add t t) HashMap.empty
      for i = 0 to testData.Length - 1 do
        let testLookup = result |> HashMap.tryFind i
        Expect.equal testLookup (ValueSome i) "Not equal to what's expected")

let [<Tests>] tests = 
    testList 
        "Set Property Tests"
        [ 
          testCase
            "Adding 3 items"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 32u; Add 1u; Add 0u ])

          testCase
            "Adding another close approximate 3 items with a hash collision from 0 and -1 keys"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 1L; Add -1L; Add 0L ]) 

          testCase
            "Adding another close approximate 3 items with a hash collision from 0 and -1 keys and then removing one of them"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 1L; Add -1L; Add 0L; Remove 0L ]) 
          
          testCase
            "Set contains keys of the same hash (Hash = 0 for both 0 and -1"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 0L; Add -1L ] )

          testCase
            "Hash collision node in tree; then one is removed with a collision"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 1L; Add -1L; Add 0L; Remove 0L ])

          testCase
            "Add and remove value with same hash"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 0L; Remove 1L ])

          testCase
            "Add and remove same key"
            (fun () -> setAndHashSetAreTheSameAfterActions [ Add 1L; Remove 1L] )

          generateLargeSizeMapTest()

          buildPropertyTest
            "Set and HashSet behave the same on Add and Remove"
            setAndHashSetAreTheSameAfterActions

          buildPropertyTest
            "Set and HashSet always have the same Contains result"
            setAndHashSetHaveSameContainsValue

          buildPropertyTest
            "Set and HashSet always have the same Count result"
            setAndHashSetHaveSameCountAtAllTimes
        ]