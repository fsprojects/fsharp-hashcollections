module FSharp.HashCollections.HashSetTest

open Expecto
open FsCheck
open FSharp.HashCollections
open System
open System.Collections.Generic

// List of actions to generate
type SetAction<'k> =
    | Add of k: 'k
    | Remove of k: 'k

let generateHashSetFromActions (actions: SetAction<'tk> list) =
    let mutable hashTrieToTest = HashSet.empty

    for action in actions do
        match action with
        | Add(k) -> hashTrieToTest <- hashTrieToTest |> HashSet.add k
        | Remove k -> hashTrieToTest <- hashTrieToTest |> HashSet.remove k

    hashTrieToTest

let generateSetFromActions (actions: SetAction<'tk> list) =
    let mutable s = Set.empty

    for action in actions do
        match action with
        | Add(k) -> s <- s |> Set.add k
        | Remove k -> s <- s |> Set.remove k

    s

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

let buildGenericPropertyTest testName (testFunction: _ -> _) =
    let config = { Config.QuickThrowOnFailure with StartSize = 0; EndSize = 100000; MaxTest = 100 }
    testCase testName <| fun () -> Check.One(config, testFunction)

let inline generateLargeSizeMapTest() =
  testCase
    "Large set test of more than one depth"
    (fun () ->
      let testData = Array.init 100000 id
      let result = testData |> Array.fold (fun s t -> s |> HashSet.add t) HashSet.empty
      for i = 0 to testData.Length - 1 do
        let testLookup = result |> HashSet.contains i
        Expect.isTrue testLookup "Data not in set as expected")

let generateLargeSizeMapOfSeqTest() =
  testCase
    "Large set test of more than one depth can be converted to sequence"
    (fun () ->
      let testData = Array.init 100000 id
      let resultSet = testData |> Array.fold (fun s t -> s |> HashSet.add t) HashSet.empty
      let resultSeq = resultSet |> HashSet.toSeq |> Seq.toArray |> Array.sort
      Expect.equal resultSeq testData "Array data not the same")

let intersectionEquilvalentToReference (hsOne: list<Guid>) (hsTwo: list<Guid>) =
  let referenceSet = System.Collections.Generic.HashSet(hsOne)
  referenceSet.IntersectWith(hsTwo)
  let referenceSetResults = referenceSet |> Seq.sort |> Seq.toArray
  let setUnderTest = HashSet.ofSeq hsOne |> HashSet.intersect (HashSet.ofSeq hsTwo) |> HashSet.toSeq |> Seq.sort |> Seq.toArray
  referenceSetResults = setUnderTest

let intersectionSupersetWithSubsetEqualToSubset (actions: SetAction<'tk> list) =
  let subSet = generateSetFromActions actions
  let fullSet = generateSetFromActions (actions |> List.filter (fun x -> match x with | Add _ -> true | _ -> false))

  let hashSubSet = generateHashSetFromActions actions
  let hashFullSet = generateHashSetFromActions (actions |> List.filter (fun x -> match x with | Add _ -> true | _ -> false))

  let resultSeq x = x |> Seq.toArray |> Array.sort

  Expect.equal (Set.intersect subSet fullSet |> resultSeq) (HashSet.intersect hashSubSet hashFullSet |> resultSeq) "Intersect not expected"

let assertEqualsTheSame actions =
  let hashSet = generateHashSetFromActions actions
  let freshSet = hashSet |> HashSet.toSeq |> HashSet.ofSeq
  Expect.equal (hashSet.GetHashCode()) (freshSet.GetHashCode()) "Hash codes not equal"
  Expect.equal hashSet freshSet "Set equality not working"

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

          testCase
            "Empty Set returns IsEmpty"
            (fun () -> Expect.isTrue (HashSet.isEmpty (HashSet.empty |> HashSet.add 1 |> HashSet.remove 1)) "HashSet not empty")

          testCase
            "Not equal set by values returns not equal"
            (fun () -> Expect.notEqual (HashSet.empty |> HashSet.add 1 |> HashSet.add 2) (HashSet.empty |> HashSet.add 3 |> HashSet.add 2) "Equal Hash Sets when shouldn't be.")

          testCase
            "Equal set by values returns equal"
            (fun () -> Expect.equal (HashSet.empty |> HashSet.add 1 |> HashSet.add 2) (HashSet.empty |> HashSet.add 2 |> HashSet.add 1) "Not equal hash sets when should be.")

          testCase
            "Nested set by values returns equal"
            (fun () ->
              let buildHashSet() = hashSet [ (1, hashSet [ (2, 3) ]); (2, hashSet [ (5, 6) ]) ]
              Expect.equal (buildHashSet()) (buildHashSet()) "Not equal when should be")

          testCase
            "HashSet should be equal despite hash collisions being inserted in different order"
            (fun () -> 
              let set1 = HashSet.empty |> HashSet.add 1UL |> HashSet.add 0x200000003UL
              let set2 = HashSet.empty |> HashSet.add 0x200000003UL |> HashSet.add 1UL
              Expect.equal set1 set2 "Sets not equal when they should be")

          testCase
            "Nested set by different nested value returns not equal"
            (fun () ->
              let buildHashSet v = hashSet [ (1, hashSet [ (2, v) ]); (2, hashSet [ (5, 6) ]) ]
              Expect.notEqual (buildHashSet 5) (buildHashSet 7) "equal when should not be")

          testCase
            "ToString output is expected"
            (fun () ->
              let testHashSet = hashSet [ hashSet [1; 2]; hashSet [3; 4] ]
              Expect.equal
                (testHashSet.ToString())
                "hashSet [hashSet [1; 2]; hashSet [3; 4]]"
                "toString not valid" )

          generateLargeSizeMapTest()

          generateLargeSizeMapOfSeqTest()

          buildPropertyTest
            "Set and HashSet behave the same on Add and Remove"
            setAndHashSetAreTheSameAfterActions

          buildPropertyTest
            "Set and HashSet always have the same Contains result"
            setAndHashSetHaveSameContainsValue

          buildPropertyTest
            "Set and HashSet always have the same Count result"
            setAndHashSetHaveSameCountAtAllTimes

          buildPropertyTest
            "Equals from actions and fresh set the same"
            assertEqualsTheSame

          buildGenericPropertyTest
            "Set and HashSet always have the same intersection result"
            intersectionEquilvalentToReference

          buildPropertyTest
            "Set and HashSet intersect removes all elements not removed in superset"
            intersectionSupersetWithSubsetEqualToSubset
        ]