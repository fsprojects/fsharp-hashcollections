namespace FSharp.HashCollections

open System.Runtime.Intrinsics
open System
open System.Runtime.CompilerServices

/// A fixed length array like structure. Only allocates as many entries as required to store elements with a maximum of sizeof(BitMap) elements.
type [<IsReadOnly; Struct>] internal CompressedArray<'t> = { BitMap: uint64; Content: 't array }

module internal ArrayHelpers =

  let inline arrayCreate<'t> length = Array.zeroCreate<'t> length

  let copyArray (sourceArray: 't[]) : 't[] =
      let result = arrayCreate sourceArray.Length
      System.Array.Copy (sourceArray, result, sourceArray.Length)
      result

  let copyArrayInsertInMiddle (insertPos : int) (itemToInsert: 't) (sourceArray: 't[]) : 't[] =
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

/// Module for handling fixed compressed bitmap array. 
/// Many operations in this module aren't checked and if not used properly could lead to data corruption. Use with caution.
module internal CompressedArray =

    let [<Literal>] MaxSize = 64
    let [<Literal>] AllNodesSetBitMap = UInt64.MaxValue
    let [<Literal>] Zero = 0UL
    let [<Literal>] One = 1UL
    let [<Literal>] LeastSigBitSet = One

    /// Has a software fallback if not supported built inside with an IF statement.
    let inline popCount x = System.Numerics.BitOperations.PopCount (uint64 x)

    let [<GeneralizableValue>] empty<'t> : CompressedArray<'t> = { BitMap = Zero; Content = Array.zeroCreate 0 }

    let inline getBitMapForIndex index = LeastSigBitSet <<< index

    let inline boundsCheckIfSet bitMap index = (getBitMapForIndex index &&& bitMap) > Zero
    let inline boundsCheckIfSetForBitMapIndex bitMap indexBitMap = (indexBitMap &&& bitMap) > Zero

    let inline getCompressedIndex bitMap index =
       let bitPos = getBitMapForIndex index
       (bitMap &&& (bitPos - One)) |> popCount

    let inline getCompressedIndexForIndexBitmap bitMap bitMapIndex =
       (bitMap &&& (bitMapIndex - One)) |> popCount

    let inline getCompressedIndexChecked bitMap index =
       let bitPos = getBitMapForIndex index
       if (bitPos &&& bitMap) <> Zero
       then (bitMap &&& (bitPos - One)) |> popCount |> ValueSome
       else ValueNone

    let inline replaceNoCheck compressedIndex value ca = 
      let newContent = copyArray ca.Content
      newContent.[compressedIndex] <- value
      { BitMap = ca.BitMap; Content = newContent }

    let set index value ca =
        let bit = getBitMapForIndex index
        let compressedIndex = getCompressedIndex ca.BitMap index
        if (bit &&& ca.BitMap) <> Zero 
        then replaceNoCheck compressedIndex value ca
        else
          // Add new entry
          let newBitMap = ca.BitMap ||| bit
          let newContent = copyArrayInsertInMiddle compressedIndex value ca.Content
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
      let mutable r = empty
      for i = 0 to a.Length - 1 do
        r <- r |> set i a.[i]
      r

    /// Given an array of MaxSize in length (not checked) produces a CompressedArray in O(1) time.
    /// Not a checked operation.
    let inline ofFullArrayAsTransient (a: _ array) = { BitMap = AllNodesSetBitMap; Content = a }

    /// Creates a compressed array of length = 1 with the supplied element in the index specified.
    let inline ofSingleElement index element = { BitMap = LeastSigBitSet <<< index; Content = [| element |] }

    /// Creates a compressed array of two elements.
    let inline ofTwoElements index1 element1 index2 element2 =
      let a = if index1 < index2 then [| element1; element2 |] else [| element2; element1 |]
      let newBitMap = Zero ||| (getBitMapForIndex index1) ||| (getBitMapForIndex index2)
      { BitMap = newBitMap; Content = a }