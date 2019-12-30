# hash-map-fsharp
Persistent hash based map implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations and not being totally satisfied with either the performance and/or the API compromises exposed I decided to code my own for both fun and profit. This is the end result implemented as a standard HAMT.

## Goals

1) More efficient persistent collection type where F#'s Map type isn't fast enough.
2) Remain idiomatic to F#; and use F# features.
3) Performance where it does not impede goal 2; even at the cost of cross language compatibility (still achievable with small wrappers consumer side).
4) Maintainable to an average F# developer.
5) Allow a range of key types to be used even if provided by other libraries without sacrificing performance.

## Design decisions

Any of these decisions may change in the future as I gain knowledge, change my mind, etc. Many besides equality checking shouldn't affect the API dramatically; and if they do it should remain easy to port code to the new API as appropriate.

1) Writing in F# vs C#
    - ✅Performance tweaks found in my trial and error experiments (structs, inlining, etc) are easier to unlock and use requiring less code.
    - ❌F# is required for consumers to realise full performance benefits even if just a simple wrapper project around the typical get and change functions (add, tryFind, remove).

1) Count is a O(1) operation. This requires an extra bit of space per tree and minor overhead during insert and removal but allows other operations on the read side to be faster (e.g isEmpty, count, etc.).
    - ✅ Lower time complexity for existence and count operations.
    - ❌ Slightly more work required when inserting and removing elements keeping track of addition or removal success.

2) NetCoreApp3.1 only or greater. This allows the use of .NET intrinsics and other performance enhancements.
    - ✅ Faster implementation
    - ❌ Only Net Core 3.1 compatible or greater.

3) At this stage I'm using ADT's vs virtual dispatch of methods in the internal implementation.
    - ✅ IMO Easier to read and more maintainable.
    - ✅ Freedom to experiment in the future with different node encoding strategies.
    - ❌ 20% slower performance from my testing for lookup's than virtual dispatch for the Try

5) Allowing custom equality via inlining. 
    - ✅ Allows fast equality checking particuarly impacting lookup's for much greater performance. 
      - F#'s "hash" and "=" functions often resulted in significant performance penalties when used as the default especially for lookup's. Since this is targeted towards F# users I've used F# inlining over equality comparer's as it showed a demostratable impact on lookup performance (tested both).
      - Equals and HashCode are can be baked into the algorithm for greater performance.
      - Faster than attaching a IEqualityComparer implementation into the Trie in my trail and error testing.
    - ✅ Encoding the equality logic in the type system also means that instances of the map with different comparer's won't be accidentaly be used together.
    - ✅ Types can still use ": equality" by default; instead of IEquatable<_> or defining an IEqualityComparer<_> and still get decent default performance.
    - ❌ Array keys don't work by default; you need to define your own structural comparer for this.
    - ❌ Limits the use of this structure to F# only.


## Performance

As of 30/12/2019; more details coming soon.

For 50,000 elements

```
Running test [TestSize: 50000, AmountOfGetRetries: 200]
Trie
Total time to insert: 51, time per insert op: 0.001020
Total time to read per get: 0.000016, CallsPerMillisecond: 62397.901434
F# Map
Total time to insert: 75, time per insert op: 0.001500
Total time to read per get: 0.000118, CallsPerMillisecond: 8467.528847
```

For 500,000 elements

```
Running test [TestSize: 500000, AmountOfGetRetries: 200]
Trie
Total time to insert: 880, time per insert op: 0.001760
Total time to read per get: 0.000031, CallsPerMillisecond: 32267.821889
F# Map
Total time to insert: 672, time per insert op: 0.001344
Total time to read per get: 0.000158, CallsPerMillisecond: 6327.621326
````

For 1,000,000 elements

```
Running test [TestSize: 1000000, AmountOfGetRetries: 200]
Trie
Total time to insert: 1716, time per insert op: 0.001716
Total time to read per get: 0.000033, CallsPerMillisecond: 30217.897329
F# Map
Total time to insert: 1221, time per insert op: 0.001221
Total time to read per get: 0.000173, CallsPerMillisecond: 5774.162392
```

For 5,000,000 elements.

```
Running test [TestSize: 5000000, AmountOfGetRetries: 200]
Trie
Total time to insert: 13184, time per insert op: 0.002637
Total time to read per get: 0.000050, CallsPerMillisecond: 20175.682988
F# Map
Total time to insert: 7102, time per insert op: 0.001420
Total time to read per get: 0.000193, CallsPerMillisecond: 5192.341713
```