module internal Configuration

open System
open System.Reflection
open TypeScriptGenerator

let mutable filterProperty: PropertyInfo -> bool = 
    fun _ -> true
let mutable converteProperty: PropertyInfo -> string option = 
    fun _ -> None
let mutable converteType: Type -> string option = 
    fun _ -> None
let mutable converteTypeName: Type -> string option = 
    fun _ -> None


let private getConverter (x: Func<'a, string>) (t:'a) =
    if isNull x then None
    else
       match x.Invoke t with
       | null -> None
       | result -> Some result

let setConfig (opts:ModelGenerateOptions) =
    filterProperty <- if isNull opts.PropertyFilter then fun _ -> true else FuncConvert.FromFunc opts.PropertyFilter
    converteProperty <- getConverter opts.PropertyConverter
    converteType <- getConverter opts.TypeConverter
    converteTypeName <- getConverter opts.TypeNameConverter
