namespace HashTrie.FSharp

open System.Runtime.Intrinsics
open System
open System.Runtime.CompilerServices

/// A fixed 32-bit array like structure. Only allocates as many entries as required to store elements with a maximum of 32 elements.
/// WARNING: There is no bounds checking on the indexes passed into the above for performance.
type [<IsReadOnly; Struct>] CompressedArray<'t> = { BitMap: uint64; Content: 't array }

module internal ArrayHelpers =

  let inline arrayCreate<'t> length = Array.zeroCreate<'t> length

  let inline copyArray (sourceArray: 't[]) : 't[] =
      let result = arrayCreate sourceArray.Length
      System.Array.Copy (sourceArray, result, sourceArray.Length)
      result

  let inline copyArrayInsertInMiddle (insertPos : int) (itemToInsert: 't) (sourceArray: 't[]) : 't[] =
    let result = arrayCreate (sourceArray.Length + 1)
    System.Array.Copy (sourceArray, result, insertPos)
    System.Array.Copy (sourceArray, insertPos, result, insertPos + 1, sourceArray.Length - insertPos)
    result.[insertPos] <- itemToInsert
    result

  let inline copyArrayWithIndexRemoved (indexToRemove: int) (sourceArray: 't[]) : 't[] =
    let result = arrayCreate (sourceArray.Length - 1)
    System.Array.Copy (sourceArray, result, indexToRemove)
    System.Array.Copy (sourceArray, indexToRemove + 1, result, indexToRemove, sourceArray.Length - indexToRemove - 1)
    result

open ArrayHelpers
open System.Collections.Generic

/// Module for handling fixed compressed bitmap array. 
/// Many operations in this module aren't checked and if not used properly could lead to data corruption. Use with caution.
module CompressedArray =

    let [<Literal>] MaxSize = 64
    let [<Literal>] LeastSigBitSet : uint64 = 0b1UL
    let [<Literal>] AllNodesSetBitMap = UInt64.MaxValue

    /// Has a software fallback if not supported built inside with an IF statement.
    /// TODO: Check if any performance difference.
    let inline popCount (x: uint64) = System.Numerics.BitOperations.PopCount x

    let [<GeneralizableValue>] empty<'t> : CompressedArray<'t> = { BitMap = 0UL; Content = Array.zeroCreate 0 }

    let inline getBitMapForIndex index = LeastSigBitSet <<< (int index)

    let inline boundsCheckIfSet bitMap index = (getBitMapForIndex index &&& bitMap) > 0UL
    let inline boundsCheckIfSetForBitMapIndex bitMap indexBitMap = (indexBitMap &&& bitMap) > 0UL

    let inline getCompressedIndex bitMap index =
       let bitPos = getBitMapForIndex index // e.g. 00010000
       (bitMap &&& (bitPos - 1UL)) |> popCount |> int// e.g 00001111 then mask that against bitmap and count

    let inline getCompressedIndexForIndexBitmap bitMap bitMapIndex =
       (bitMap &&& (bitMapIndex - 1UL)) |> popCount |> int

    let inline set index value ca =
        let bit = getBitMapForIndex index
        let localIdx = getCompressedIndex ca.BitMap index |> int
        if (bit &&& ca.BitMap) <> 0UL then
          // Replace existing entry
          let newContent = copyArray ca.Content
          newContent.[localIdx] <- value
          { BitMap = ca.BitMap; Content = newContent }
        else
          // Add new entry
          let newBitMap = ca.BitMap ||| bit
          let newContent = copyArrayInsertInMiddle localIdx value ca.Content
          { BitMap = newBitMap; Content = newContent }

    // NOTE: This function when non-inlined after testing can cause performance regressions due to struct copying.
    let inline get index ca =
        let bitPos = getBitMapForIndex index
        if boundsCheckIfSetForBitMapIndex ca.BitMap bitPos // This checks if the bit was set in the first place.
        then ValueSome ca.Content.[getCompressedIndexForIndexBitmap ca.BitMap bitPos]
        else ValueNone

    /// Get the amount of set positions in the compressed array.
    let count ca = ca.Content.Length

    /// Creates a new compressed array with the index unset.
    let inline unset index ca =
        if boundsCheckIfSet ca.BitMap index
        then
            let compressedIndex = getCompressedIndex ca.BitMap index
            let bitToUnsetMask = getBitMapForIndex index
            let newBitMap = ca.BitMap ^^^ bitToUnsetMask
            let newContent = copyArrayWithIndexRemoved compressedIndex ca.Content
            { BitMap = newBitMap; Content = newContent }
        else ca // Do nothing; not set.

    let ofArray (a: _ array) =
      let mutable cArray = empty
      for i = 0 to a.Length - 1 do
        cArray <- cArray |> set i a.[i]
      cArray

    /// Given an array of MaxSize in length (not checked) produces a CompressedArray in O(1) time.
    let inline ofFullArrayAsTransient (a: _ array) =
      //if a.Length <> MaxSize then failwithf "Only works for full array conversions"
      { BitMap = AllNodesSetBitMap; Content = a }

    /// Creates a compressed array of length = 1 with the supplied element in the index specified.
    let inline ofSingleElement index element =
      let bitMap = 0b1UL <<< int index
      { BitMap = bitMap; Content = [| element |] }