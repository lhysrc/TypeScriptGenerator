namespace TypeScriptGenerator
open System
open System.Reflection

type Options (dest: string) = 
    member val Destination: string = dest with get
    member val TypeMatcher: Func<Type, bool> = null with get, set
    member val PropertyConverter: Func<PropertyInfo, string> = null with get, set
    member val CodeSnippets: Func<Type, string> = null with get, set

type internal TypeOption = {
    Type : Type
    Path : string
    PropertyConverter: (PropertyInfo -> string) option
    CodeSnippets: string option
}

type internal TSFile = {
    FullPath : string
    Content :string
    ImportedTypes : Type list
}