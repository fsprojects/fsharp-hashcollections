namespace HashTrie.FSharp

open System.Runtime.Intrinsics
open System
open System.Reflection.Emit
open System.Runtime.CompilerServices

/// A fixed 32-bit array like structure. Only allocates as many entries as required to store elements with a maximum of 32 elements.
/// WARNING: There is no bounds checking on the indexes passed into the above for performance.
type [<IsReadOnly; Struct>] Compressed32PosArray<'t> = { BitMap: uint64; Content: 't array }

module ArrayUtils = 

  // type MethodGenerator<'t>() = 
  //   static member val ArrayCreate : int -> 't array = 
  //     printfn "Generating method"
  //     let il = DynamicMethod("GenerateArrayUnchecked", typeof<'t array>, [| typeof<int> |])
  //     let gen = il.GetILGenerator()
  //     gen.Emit(OpCodes.Ldarg_0)
  //     gen.Emit(OpCodes.Newarr, typeof<'t>)
  //     gen.Emit(OpCodes.Ret)
  //     let d = il.CreateDelegate(typeof<Func<int, 't array>>) :?> Func<int, 't array>
  //     d.Invoke

  let inline arrayCreate<'t> length = 
    Array.zeroCreate<'t> length
    //MethodGenerator<'t>.ArrayCreate length
  
  let inline copyArray (sourceArray: 'T []) : 'T [] =
      let result = arrayCreate sourceArray.Length
      System.Array.Copy (sourceArray, result, sourceArray.Length)
      result

  let inline copyArrayInsertInMiddle (holeIndex : int) (itemToInsert: 'T) (sourceArray: 'T []) : 'T [] =
    let result = arrayCreate (sourceArray.Length + 1)
    System.Array.Copy (sourceArray, result, holeIndex)
    System.Array.Copy (sourceArray, holeIndex, result, holeIndex + 1, sourceArray.Length - holeIndex)
    result.[holeIndex] <- itemToInsert
    result

open ArrayUtils

module Compressed32PosArray =

    let [<Literal>] LeastSigBitSet : uint64 = 0b1UL
  
    let inline popCount (x: uint64) = X86.Popcnt.X64.PopCount x

    let [<GeneralizableValue>] empty<'t> : Compressed32PosArray<'t> = { BitMap = 0UL; Content = Array.zeroCreate 0 }

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
          let newContent = copyArray ca.Content
          newContent.[localIdx] <- value
          { BitMap = ca.BitMap; Content = newContent }
        else
          let newBitMap = ca.BitMap ||| bit
          let newContent = copyArrayInsertInMiddle localIdx value ca.Content
          { BitMap = newBitMap; Content = newContent }

    // NOTE: This function when non-inlined after testing can cause performance regressions.
    let inline get index ca = 
        let bitPos = getBitMapForIndex index
        if boundsCheckIfSetForBitMapIndex ca.BitMap bitPos // This checks if the bit was set in the first place.
        then ValueSome ca.Content.[getCompressedIndexForIndexBitmap ca.BitMap bitPos]
        else ValueNone 

    /// Get the amount of set positions in the compressed array.
    let count ca = ca.Content.Length

    /// Creates a new compressed array with the index unset.
    /// TODO: Use Array.Copy method above for improved performance here.
    let inline unset index ca = 
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

//Compressed32PosArray.empty |> Compressed32PosArray.set 37 "T" |> Compressed32PosArray.get 38

// type Uncompressed32PosArray<'t> = ('t voption) array

// module Uncompressed4PosArray = 

//   type private Generator<'t>() = 
//     static member val SizeOf : int = sizeof<'t voption>

//   let inline empty<'t> : Uncompressed32PosArray<'t> = Array.zeroCreate 4

//   let inline get index (ua: Uncompressed32PosArray<_>) = 
//     ua.[index]

//   let inline set index (value: 't) (ua: Uncompressed32PosArray<'t>) = 
//     let r = empty
//     Array.Copy(ua, r, 4)
//     r.[index] <- ValueSome value
//     r

//   let inline unset index (ua: Uncompressed32PosArray<'t>) = 
//     let r = empty
//     Array.Copy(ua, r, 4)
//     r.[index] <- ValueNone
//     r

//   let count (ua: Uncompressed32PosArray<_>) = ua |> Array.filter ValueOption.isSome |> Array.length