namespace TypeScriptGenerator
open System

type Options () = 
    member val TypeMatcher: Func<Type,bool> = null with get, set


type internal TypeScriptFile = {
    Name: string
    Type : Type
    Content:string
    FullPath: string
}
