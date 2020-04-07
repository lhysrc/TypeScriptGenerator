﻿module TS

open System
open System.Collections.Generic

type SystemType = {
    Key : Type
    Value :string
}

let systemTypes = [
    {Key = typeof<Void>;Value = "void"}
    {Key = typeof<obj>;Value = "object"}

    {Key = typeof<String>;Value = "string"}
    {Key = typeof<Char>;Value = "string"}
    {Key = typeof<Nullable<Char>>;Value = "string"}
    {Key = typeof<Guid>;Value = "string"}
    {Key = typeof<Nullable<Guid>>;Value = "string"}
    {Key = typeof<Uri>;Value = "string"}


    {Key = typeof<int>;Value = "number"}
    {Key = typeof<Nullable<int>>;Value = "number"}
    {Key = typeof<uint32>;Value = "number"}
    {Key = typeof<Nullable<uint32>>;Value = "number"}
    {Key = typeof<int8>;Value = "number"}
    {Key = typeof<Nullable<int8>>;Value = "number"}
    {Key = typeof<uint8>;Value = "number"}
    {Key = typeof<Nullable<uint8>>;Value = "number"}
    {Key = typeof<int16>;Value = "number"}
    {Key = typeof<Nullable<int16>>;Value = "number"}
    {Key = typeof<uint16>;Value = "number"}
    {Key = typeof<Nullable<uint16>>;Value = "number"}
    {Key = typeof<int64>;Value = "number"}
    {Key = typeof<Nullable<int64>>;Value = "number"}
    {Key = typeof<uint64>;Value = "number"}
    {Key = typeof<Nullable<uint64>>;Value = "number"}

    {Key = typeof<float>;Value = "number"}
    {Key = typeof<Nullable<float>>;Value = "number"}
    {Key = typeof<float32>;Value = "number"}
    {Key = typeof<Nullable<float32>>;Value = "number"}
    {Key = typeof<decimal>;Value = "number"}
    {Key = typeof<Nullable<decimal>>;Value = "number"}

    {Key = typeof<bool>;Value = "boolean"}
    {Key = typeof<Nullable<bool>>;Value = "boolean"}

    {Key = typeof<DateTime>;Value = "Date"}
    {Key = typeof<Nullable<DateTime>>;Value = "Date"}
    {Key = typeof<DateTimeOffset>;Value = "Date"}
    {Key = typeof<Nullable<DateTimeOffset>>;Value = "Date"}
]

let rec isArray (t:Type)=
    if t.IsGenericType then
        t.GetInterfaces() 
        |> Array.append [| t |]
        |> Array.tryFind(fun i-> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)
        |> function 
        | Some i -> true,getTypeName i.GenericTypeArguments.[0]
        | None -> false, null
    else if t.IsArray && t.HasElementType then
        true, getTypeName (t.GetElementType())
    else
        false, null

and isMap (t:Type) = //todo 使用[key:string]:TypeName??
    printfn "%s" t.Name
    if t.IsGenericType then
        t.GetInterfaces() 
        |> Array.append [| t |]
        |> Array.tryFind(fun i-> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IDictionary<_,_>>)
        |> function 
        | Some i -> true,getTypeName i.GenericTypeArguments.[0],getTypeName i.GenericTypeArguments.[1]
        | None -> false, null, null
    else
        false, null, null
    
and getTypeName (t:Type)=   
    let tsType = systemTypes |> List.tryFind (fun st -> st.Key = t)
    match tsType with
    | Some t -> t.Value
    | None -> 
        match isMap t with
        | true,k,v -> sprintf "Map<%s,%s>" k v
        | false,_,_ ->
            match isArray t with
            | true, name -> name + "[]"
            | false, _ -> 
                match t.IsGenericType with
                | true ->                 
                    let args = 
                        t.GetGenericArguments()
                        |> Seq.map getTypeName
                        |> String.concat ","
                    String.concat "" [                    
                        getName t
                        "<"
                        args
                        ">"
                    ]
                | false -> getName t
                
     