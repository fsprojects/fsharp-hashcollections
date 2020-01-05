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

namespace Persistent

module PersistentHashMap =

  open System.Collections.Generic

  let inline containsKey (key : 'K) (m : PersistentHashMap<'K, 'V>) : bool =
    match m.TryFind key with
    | true, _ -> true
    | _   , _ -> false

  [<GeneralizableValue>]
  let empty<'K, 'V when 'K :> System.IEquatable<'K>> : PersistentHashMap<_, _> = upcast PersistentHashMap<'K, 'V>.Empty

  let inline isEmpty (m : PersistentHashMap<'K, 'V>) : bool =
    m.IsEmpty

  let inline tryFind (key : 'K) (m : PersistentHashMap<'K, 'V>) : 'V option =
    match m.TryFind key with
    | true, v -> Some v
    | _   , _ -> None

  let inline set (key : 'K) (value : 'V) (m : PersistentHashMap<'K, 'V>) : PersistentHashMap<'K, 'V> =
    m.Set key value

  let inline unset (key : 'K) (m : PersistentHashMap<'K, 'V>) : PersistentHashMap<'K, 'V> =
    m.Unset key

  let inline visit (visitor : 'K -> 'V -> bool) (m : PersistentHashMap<'K, 'V>) : bool =
    m.Visit visitor

  let inline toArray (m : PersistentHashMap<'K, 'V>) : KeyValuePair<'K, 'V> [] =
    let ra = ResizeArray<_> 16
    visit (fun k v -> ra.Add (KeyValuePair (k, v)); true) m |> ignore
    ra.ToArray ()

  let inline toSeq (m : PersistentHashMap<'K, 'V>) : seq<KeyValuePair<'K, 'V>> =
    upcast m

  let inline length (m : PersistentHashMap<'K, 'V>) : int =
    let l = ref 0
    visit (fun k v -> incr l; true) m |> ignore
    !l

  let inline mapValues (f : 'K -> 'V -> 'U) (m : PersistentHashMap<'K, 'V>) : PersistentHashMap<'K, 'U> =
    m.MapValues f
