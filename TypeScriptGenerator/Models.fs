namespace TypeScriptGenerator
open System

type Options () = 
    member val TypeMatcher: Func<Type,bool> = null with get, set

type TypeOption = {
    Type : Type
    UsedTypes : Type[]
}