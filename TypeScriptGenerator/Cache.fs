module internal Cache

open System
open System.Collections.Generic

let internal generatedTypes = HashSet<Type>()

let memoize fn =
  let cache = Dictionary<_,_> ()
  fun x ->
    match cache.TryGetValue x with
    | true,  v -> v
    | false, _ -> let v = fn x
                  cache.Add (x,v)
                  v

let getImportTypes :Type -> Type HashSet =
    fun _ -> HashSet<Type>()
    |> memoize
