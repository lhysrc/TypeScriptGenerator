namespace TypeScriptGenerator
open System
open System.Reflection

type ModelGenerateOptions (dest: string) = 
    member val Destination: string = dest with get
    member val TypeFilter: Func<Type, bool> = null with get, set
    member val CodeSnippets: Func<Type, string> = null with get, set

    member val PropertyFilter: Func<PropertyInfo, bool> = null with get, set
    member val PropertyConverter: Func<PropertyInfo, string> = null with get, set
    member val TypeConverter: Func<Type, string> = null with get, set

type internal TypeOptions = {
    Type : Type
    Path : string
    //PropertyFilter: PropertyInfo -> bool
    //PropertyConverter: PropertyInfo -> string option //todo
    //TypeConverter: Type -> string option //todo
    CodeSnippets: string list
}

type internal TSFile = {
    Type : Type
    FullPath : string
    Content :string
    ImportedTypes : Type list
}