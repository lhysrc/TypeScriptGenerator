module internal Config

open System
open System.Reflection
open TypeScriptGenerator

let mutable propertyFilter: PropertyInfo -> bool = 
    fun _ -> true
let mutable propertyConverter: PropertyInfo -> string option = 
    fun _ -> None
let mutable typeConverter: Type -> string option = 
    fun _ -> None



let private getConverter (x: Func<'a, string>) (t:'a) =
    if isNull x then None
    else
       match x.Invoke t with
       | null -> None
       | result -> Some result

let setConfig (opts:ModelGenerateOptions) =
    propertyFilter <- if isNull opts.PropertyFilter then fun _ -> true else FuncConvert.FromFunc opts.PropertyFilter
    propertyConverter <- getConverter opts.PropertyConverter
    typeConverter <- getConverter opts.TypeConverter
