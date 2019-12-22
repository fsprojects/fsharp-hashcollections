namespace HashTrie.FSharp

open System.Runtime.Intrinsics
open System

/// A fixed 32-bit array like structure. Only allocates as many entries as required to store elements with a maximum of 32 elements.
/// WARNING: There is no bounds checking on the indexes passed into the above for performance.
type [<Struct>] Compressed32PosArray<'t> = { BitMap: uint32; Content: 't array }

module ArrayUtils = 
  let inline copyArray (sourceArray: 'T []) : 'T [] =
      let result = Array.zeroCreate sourceArray.Length
      System.Array.Copy (sourceArray, result, sourceArray.Length)
      result

  let inline copyArrayMakeHoleLast (itemToInsert: 'T) (sourceArray: 'T []) : 'T [] =
    let result = Array.zeroCreate (sourceArray.Length + 1)
    System.Array.Copy (sourceArray, result, sourceArray.Length)
    result.[sourceArray.Length] <- itemToInsert
    result

  let inline copyArrayMakeHole (holeIndex : int) (itemToInsert: 'T) (sourceArray: 'T []) : 'T [] =
    let result = Array.zeroCreate (sourceArray.Length + 1)
    System.Array.Copy (sourceArray, result, holeIndex)
    System.Array.Copy (sourceArray, holeIndex, result, holeIndex + 1, sourceArray.Length - holeIndex)
    result.[holeIndex] <- itemToInsert
    result

  let inline copyArrayRemoveHole (holeIndex: int) (sourceArray: 'T []) : 'T [] =
    let result = Array.zeroCreate (sourceArray.Length - 1)
    System.Array.Copy (sourceArray, result, holeIndex)
    System.Array.Copy (sourceArray, holeIndex + 1, result, holeIndex, sourceArray.Length - holeIndex - 1)
    result

open ArrayUtils

module Compressed32PosArray =

    let [<Literal>] LeastSigBitSet = 0x1u
  
    //let inline leadingZeroCount x = X86.Lzcnt.LeadingZeroCount (uint32 x)
    let inline popCount x = 
        X86.Popcnt.PopCount (uint32 x) //20% faster approx
        // let mutable v = x
        // v <- v - ((v >>> 1) &&& 0x55555555u)
        // v <- (v &&& 0x33333333u) + ((v >>> 2) &&& 0x33333333u)
        // ((v + (v >>> 4) &&& 0xF0F0F0Fu) * 0x1010101u) >>> 24

    let [<GeneralizableValue>] empty<'t> : Compressed32PosArray<'t> = { BitMap = 0u; Content = Array.zeroCreate 0 }

    //let inline firstUncompressedIndexSet bitMap = leadingZeroCount bitMap

    let inline getBitMapForIndex index = LeastSigBitSet <<< index

    let inline boundsCheckIfSet bitMap index = (getBitMapForIndex index &&& bitMap) > 0u

    let inline getCompressedIndex bitMap index = 
       let bitPos = getBitMapForIndex index // e.g. 00010000
       (bitMap &&& (bitPos - 1u)) |> popCount |> int// e.g 00001111 then mask that against bitmap and count

    let inline set index value ca = 
        let bit = getBitMapForIndex index
        let localIdx = getCompressedIndex ca.BitMap index |> int
        if (bit &&& ca.BitMap) <> 0u then
          let newContent = copyArray ca.Content
          newContent.[localIdx] <- value
          { BitMap = ca.BitMap; Content = newContent }
        else
          let newBitMap = ca.BitMap ||| bit
          let newContent = copyArrayMakeHole localIdx value ca.Content
          { BitMap = newBitMap; Content = newContent }

    // NOTE: This function when non-inlined after testing can cause performance regressions.
    let inline get index ca = 
        if boundsCheckIfSet ca.BitMap index // This checks if the bit was set in the first place.
        then
            let compressedIndex = getCompressedIndex ca.BitMap index |> int
            ValueSome ca.Content.[compressedIndex]
        else ValueNone

    /// Get the amount of set positions in the compressed array.
    let count ca = ca.Content.Length

    /// Creates a new compressed array with the index unset.
    /// TODO: Use Array.Copy method above for improved performance here.
    let unset index ca = 
        if boundsCheckIfSet ca.BitMap index
        then
            let bitToUnsetMask = getBitMapForIndex index
            let mutable oldCompressedIndexToSkip = getCompressedIndex ca.BitMap index |> int32 |> ValueSome
            let newBitMap = ca.BitMap ^^^ bitToUnsetMask
            let result = Array.zeroCreate (popCount newBitMap |> int32) // Can be the same length or lower (i.e. if thing was already unset same length)
            let mutable oldIndexUpTo = 0
            let mutable newIndex = 0
            while (newIndex < result.Length) do
                if ValueSome(newIndex) = oldCompressedIndexToSkip 
                then
                    oldIndexUpTo <- oldIndexUpTo + 1
                    oldCompressedIndexToSkip <- ValueNone    
                else
                    result.[newIndex] <- ca.Content.[oldIndexUpTo]
                    newIndex <- newIndex + 1
                    oldIndexUpTo <- oldIndexUpTo + 1
            { BitMap = newBitMap; Content = result }
        else ca // Do nothing; not set.
