module internal Configuration

open System
open System.Reflection
open TypeScriptGenerator

let mutable filterProperty: PropertyInfo -> bool = 
    fun _ -> true
let mutable converteProperty: PropertyInfo -> string option = 
    fun _ -> None
let mutable converteType: Type -> Type option =
    fun _ -> None
let mutable converteTypeName: Type -> string option = 
    fun _ -> None

let mutable currentEnableJsDoc = false // Added to store EnableJsDoc flag, defaults to false

let private getConverter (x: Func<'a, 'result>) (t:'a) =
    if isNull x then None
    else
       match x.Invoke t with
       | null -> None
       | result -> Some result

let setOptions (opts:ModelGenerateOptions) =
    filterProperty <- if isNull opts.PropertyFilter then fun _ -> true else FuncConvert.FromFunc opts.PropertyFilter
    converteProperty <- getConverter opts.PropertyConverter
    converteType <- getConverter opts.TypeConverter
    converteTypeName <- getConverter opts.TypeNameConverter
    currentEnableJsDoc <- opts.EnableJsDoc // Set the flag based on options

let xmlDocs = System.Collections.Generic.Dictionary<string, string>()

let addXmlDoc (key: string) (doc: string) =
    if not (xmlDocs.ContainsKey key) then
        xmlDocs.Add(key, doc)
