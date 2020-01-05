﻿// ----------------------------------------------------------------------------------------------
// Copyright 2016 Mårten Rånge
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------------------

// Inspired by Clojure's Persistent Hash Map https://github.com/clojure/clojure/blob/master/src/jvm/clojure/lang/PersistentHashMap.java)
//  and Phil Bagwell's Ideal Hash Trie (http://lampwww.epfl.ch/papers/idealhashtrees.pdf)
//  and http://graphics.stanford.edu/~seander/bithacks.html#CountBitsSetParallel

namespace Persistent

open System.Runtime.InteropServices

open System.Collections.Generic

type [<AbstractClass>] PersistentHashMap<'K, 'V when 'K :> System.IEquatable<'K>> =
  class
    static member internal Empty : PersistentHashMap<'K, 'V>

#if PHM_TEST_BUILD
    member CheckInvariant : unit -> bool
#endif
    member IsEmpty        : bool
    member Visit          : v : ('K -> 'V -> bool) -> bool
    member Set            : k : 'K -> v : 'V -> PersistentHashMap<'K, 'V>
    member TryFind        : k : 'K*[<Out>] rv : byref<'V> -> bool
    member Unset          : k : 'K -> PersistentHashMap<'K, 'V>
    member MapValues      : m : ('K -> 'V -> 'U) -> PersistentHashMap<'K, 'U>

    interface IEnumerable<KeyValuePair<'K,'V>>

#if PHM_TEST_BUILD
    abstract internal DoCheckInvariant : uint32 -> int -> int -> bool
#endif
    // TODO: Why aren't these tagged as internal in the generated assembly
    abstract internal DoIsEmpty   : unit    -> bool
    abstract internal DoVisit     : OptimizedClosures.FSharpFunc<'K, 'V, bool> -> bool
    abstract internal DoSet       : uint32  -> int  -> KeyValueNode<'K, 'V> -> PersistentHashMap<'K, 'V>
    abstract internal DoTryFind   : uint32*int*'K*byref<'V> -> bool
    abstract internal DoUnset     : uint32  -> int  -> 'K -> PersistentHashMap<'K, 'V>
    abstract internal DoMapValues : FSharp.Core.OptimizedClosures.FSharpFunc<'K, 'V, 'U> -> PersistentHashMap<'K, 'U>
    abstract internal DoGetChild  : int*byref<PersistentHashMap<'K, 'V>> -> bool
  end
and [<Sealed>] internal KeyValueNode<'K, 'V when 'K :> System.IEquatable<'K>> =
  class
    inherit PersistentHashMap<'K, 'V>
  end
