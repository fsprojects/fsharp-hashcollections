# hash-trie-fsharp
Hash Trie implementation

Made for my own purposes the goal is to allow faster lookup table performance in F#. After testing some the built in F# Map and other collection implementations I decided to code my own for both fun and profit. This is the end result.

## Goals

1) More efficient persistent collection type where F#'s Map type isn't fast enough.
2) Remain idiomatic to F#; and use F# features.
3) Performance where it does not impede goal 2.
4) Maintainable to an average F# developer.
5) Allow a range of key types to be used even if provided by other libraries without sacrificing performance.

## Design decisions

1) Count is a O(1) operation. This requires an extra bit of space per tree and minor overhead during insert and removal.

2) TrieNodes can grow up to 64 nodes in length vs the usual 32. Performance testing showed a huge performance benefit in doing so without much effect on insert speed whatsoever. Less depth of nodes on average means much better scaling when size of dictionary > 1 million elements.

3) NetCoreApp3.1 only or greater. This allows the use of .NET intrinsics assisting performance.

4) At this stage I'm using ADT's vs virtual dispatch of methods in the internal implementation. This does affect performance slightly of Get operations in particular and I may revisit this in the future if it does not make the implementation less maintainable.

5) Allowing custom equality. F#'s "hash" and "equals" functions often resulted in significant performance penalties when used as the default especially for lookup's.