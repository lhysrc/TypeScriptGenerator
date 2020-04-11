namespace TypeScriptGenerator
open System

type Options () = 
    member val TypeMatcher: Func<Type,bool> = null with get, set

type internal TypeOption = {
    Type : Type
    Path : string
}

type internal TSFile = {
    FullPath : string
    Content :string
    ImportedTypes : Type list
}