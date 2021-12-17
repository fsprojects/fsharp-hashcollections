module FSharp.HashCollections.Unit.HashTrieTest

open Expecto
open FsCheck
open FSharp.HashCollections
open System.Collections.Generic

// List of actions to generate
type KvAction<'k, 'v> =
    | Add of k: 'k * v: 'v
    | Remove of k: 'k

let inline mapAndHashTrieAreTheSameAfterActions (actions: KvAction<'tk, 'tv> list) =

    let mutable mapToTest = Map.empty
    let mutable hashTrieToTest = HashMap.empty

    for action in actions do
        match action with
        | Add(k, v) ->
            mapToTest <- mapToTest |> Map.add k v
            hashTrieToTest <- hashTrieToTest |> HashMap.add k v
        | Remove k ->
            mapToTest <- mapToTest |> Map.remove k
            hashTrieToTest <- hashTrieToTest |> HashMap.remove k
        Expect.equal
            (hashTrieToTest |> HashMap.toSeq |> set)
            (mapToTest |> Map.toSeq |> Seq.map (fun (x, y) -> struct (x, y)) |> set)
            "Hash Trie and Map don't contain same data"

let generateHashMapFromActions (actions: KvAction<'tk, 'tv> list) =
    let mutable hashTrieToTest = HashMap.empty

    for action in actions do
        match action with
        | Add(k, v) -> hashTrieToTest <- hashTrieToTest |> HashMap.add k v
        | Remove k -> hashTrieToTest <- hashTrieToTest |> HashMap.remove k
    hashTrieToTest

let toVOption i = match i with | Some(x) -> ValueSome x | None -> ValueNone

let inline mapAndHashTrieHaveSameGetValue (actions: KvAction<'tk, 'tv> list) =

    let mutable mapToTest = Map.empty
    let mutable hashTrieToTest = HashMap.empty

    for action in actions do
        let mutable key = Unchecked.defaultof<'tk>
        match action with
        | Add(k, v) ->
            mapToTest <- mapToTest |> Map.add k v
            hashTrieToTest <- hashTrieToTest |> HashMap.add k v
            key <- k
        | Remove k ->
            mapToTest <- mapToTest |> Map.remove k
            hashTrieToTest <- hashTrieToTest |> HashMap.remove k
            key <- k
        let mapResult = mapToTest |> Map.tryFind key |> toVOption
        let hashTrieResult = hashTrieToTest |> HashMap.tryFind key
        Expect.equal hashTrieResult mapResult "Key update did not hold"

let inline mapAndHashMapHaveSameCountAtAllTimes (actions: KvAction<'tk, 'tv> list) =
    let mutable mapToTest = Map.empty
    let mutable hashTrieToTest = HashMap.empty

    for action in actions do
        let mutable key = Unchecked.defaultof<'tk>
        match action with
        | Add(k, v) ->
            mapToTest <- mapToTest |> Map.add k v
            hashTrieToTest <- hashTrieToTest |> HashMap.add k v
            key <- k
        | Remove k ->
            mapToTest <- mapToTest |> Map.remove k
            hashTrieToTest <- hashTrieToTest |> HashMap.remove k
            key <- k
        let mapResult = mapToTest |> Map.count
        let hashTrieResult = hashTrieToTest |> HashMap.count
        Expect.equal hashTrieResult mapResult "Count isn't the same"

let buildPropertyTest testName (testFunction: KvAction<int64, int> list -> _) =
    let config = { Config.QuickThrowOnFailure with StartSize = 0; EndSize = 100000; MaxTest = 100 }
    testCase testName <| fun () -> Check.One(config, testFunction)

let generateLargeSizeMapTest() =
  testCase
    "Large map test of more than one depth"
    (fun () ->
      let testData = Array.init 100000 id
      let result = testData |> Array.fold (fun s t -> s |> HashMap.add t t) HashMap.empty
      for i = 0 to testData.Length - 1 do
        let testLookup = result |> HashMap.tryFind i
        Expect.equal testLookup (ValueSome i) "Not equal to what's expected")

let generateLargeSizeMapOfSeqTest() =
  testCase
    "Large map test of more than one depth can be converted to sequence"
    (fun () ->
      let testData = Array.init 100000 id
      let resultMap = testData |> Array.fold (fun s t -> s |> HashMap.add t t) HashMap.empty
      let resultSeq = resultMap |> HashMap.toSeq |> Seq.map (fun struct (k, v) -> k) |> Seq.toArray |> Array.sort
      Expect.equal resultSeq testData "Array data not the same")

let assertEqualsTheSame actions =
  let hashMap = generateHashMapFromActions actions
  let freshMap = hashMap |> HashMap.toSeq |> Seq.map (fun struct (k, v) -> KeyValuePair.Create(k, v)) |> HashMap.ofSeq
  Expect.equal (hashMap.GetHashCode()) (freshMap.GetHashCode()) "Hash codes not equal"
  Expect.equal hashMap freshMap "Map equality not working"

let [<Tests>] tests =
    testList
        "Hash Map Property Tests"
        [
          testCase
            "Adding 3 k-v pairs"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (32u,0L); Add (1u,0L); Add (0u,0L) ])

          testCase
            "Adding another close approximate 3 kv-pairs with a hash collision from 0 and -1 keys"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Add (-1L,0); Add (0L,0) ])

          testCase
            "Adding another close approximate 3 kv-pairs with a hash collision from 0 and -1 keys and then removing one of them"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Add (-1L,0); Add (0L,0); Remove 0L ])

          testCase
            "Map contains keys of the same hash (Hash = 0 for both 0 and -1"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add(0L, 5); Add(-1L, 6) ] )

          testCase
            "Hash collision node in tree; then one is removed with a collision"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Add (-1L,0); Add (0L,0); Remove 0L ])

          testCase
            "Add and remove value with same hash"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (0L,0); Remove 1L ])

          testCase
            "Add and remove same key"
            (fun () -> mapAndHashTrieAreTheSameAfterActions [ Add (1L,0); Remove 1L] )

          testCase
            "Empty Map returns IsEmpty"
            (fun () -> Expect.isTrue (HashMap.isEmpty (HashMap.empty |> HashMap.add 1 1 |> HashMap.remove 1)) "HashSet not empty")

          testCase
            "Not equal map by values returns not equal"
            (fun () -> Expect.notEqual (HashMap.empty |> HashMap.add 1 1 |> HashMap.add 2 1) (HashMap.empty |> HashMap.add 1 2 |> HashMap.add 2 2) "Equal Hash maps when shouldn't be.")

          testCase
            "Equal map by values returns equal"
            (fun () -> Expect.equal (HashMap.empty |> HashMap.add 1 1 |> HashMap.add 2 2) (HashMap.empty |> HashMap.add 1 1 |> HashMap.add 2 2) "Not Equal Hash maps when should be.")

          testCase
            "Nested map by values returns equal"
            (fun () ->
              let buildHashMap() = hashMap [ (1, hashMap [ (2, 3) ]); (4, hashMap [ (5, 6) ]) ]
              Expect.equal (buildHashMap()) (buildHashMap()) "Not equal when should be")

          testCase
            "Nested map not equal by values returns not equal"
            (fun () ->
              let buildHashMap v = hashMap [ (1, hashMap [ (2, v) ]); (4, hashMap [ (5, 6) ]) ]
              Expect.notEqual (buildHashMap 5) (buildHashMap 7) "Not equal when should be")

          testCase
            "ToString output is expected"
            (fun () ->
              let testHashMap = hashMap [ (1, hashMap [ (3, 4)]); (5, hashMap [(6, 7)] ) ]
              Expect.equal
                (testHashMap.ToString())
                "hashMap [(1, hashMap [(3, 4)]); (5, hashMap [(6, 7)])]"
                "toString not valid" )

          generateLargeSizeMapOfSeqTest()

          generateLargeSizeMapTest()

          buildPropertyTest
            "Map and HashTrie behave the same on Add and Remove"
            mapAndHashTrieAreTheSameAfterActions

          buildPropertyTest
            "Map and HashTrie always have the same Get result"
            mapAndHashTrieHaveSameGetValue

          buildPropertyTest
            "Map and HashTrie always have the same Count result"
            mapAndHashMapHaveSameCountAtAllTimes

          buildPropertyTest
            "Equals from actions and fresh map the same"
            assertEqualsTheSame
        ]