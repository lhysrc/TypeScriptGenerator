module internal Cache

open System
open System.Collections.Generic

let internal generatedTypes = HashSet<Type>()
let internal importedTypes = Dictionary<Type,HashSet<Type>>()

let getImportTypes (t:Type) =
    match importedTypes.TryGetValue t with
    | true,v -> v
    | false,_ ->
        let v = HashSet<Type>()
        importedTypes.[t] <- v
        v
