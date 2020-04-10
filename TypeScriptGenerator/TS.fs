module TS
open Type
open System
open System.Collections.Generic

type SystemType = {
    Key : Type
    Value :string
}

let private buildinTypes = [
    {Key = typeof<Void>;Value = "void"}
    {Key = typeof<obj>;Value = "any"}

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
let private cache = dict Seq.empty<Type * string>;
   
let isBuildIn (t:Type) =
    buildinTypes |> List.exists (fun i->i.Key = t)

let addUsedType (ts:Type HashSet) (t:Type) =
    let t' = (*unwrap*) t
    if (*isBuildIn t' ||*) t.IsGenericParameter then ()
    else if t.IsGenericType then ts.Add (t.GetGenericTypeDefinition()) |> ignore
    else ts.Add t' |> ignore


let (|TSBuildIn|_|) (t:Type) = 
    buildinTypes |> List.tryFind (fun i->i.Key = t)

let (|TSTuple|_|) (t:Type) =
    if Reflection.FSharpType.IsTuple t then Some t
    else None

let (|TSMap|_|) (t:Type) = 
    getMapType t
    
let (|TSArray|_|) (t:Type) = 
    getArrayType t

let rec getTypeName (useds:Type HashSet) (t:Type):string =    
    match (unwrap t) with
    | TSBuildIn t -> t.Value
    | TSTuple t -> "[]"
    | TSMap (k,v) -> 
        match k with
        | k when k = typeof<string> -> "{ [key:string]: " + (getTypeName useds v) + " }"
        | _ -> sprintf "Map<%s,%s>" (getTypeName useds k)(getTypeName useds v)
    | TSArray t -> (getTypeName useds t) + "[]"
    | t -> 
        addUsedType useds t
        match t.IsGenericType with
        | true ->   
            let args = 
                t.GetGenericArguments()
                |> Seq.map (getTypeName useds)
                |> String.concat ","
            String.concat "" [                    
                Type.getName t
                "<"
                args
                ">"
            ]
        | false -> Type.getName t

(*
let rec getTypeName (useds:Type HashSet) (t:Type):string =
    addUsedType useds t
    let tsType = buildinTypes |> List.tryFind (fun st -> st.Key = t)
    match tsType with
    | Some t -> t.Value
    | None -> 
        match getMapType t with
        | Some (k,v) -> sprintf "Map<%s,%s>" (getTypeName useds k)(getTypeName useds v)
        | None ->
            match getArrayType t with
            | Some t -> (getTypeName useds t) + "[]"
            | None -> 
                match t.IsGenericType with
                | true ->    
                    if t.GetGenericTypeDefinition() = typedefof<Nullable<_>> then 
                        Type.getName (t.GetGenericArguments().[0])
                    else
                        let args = 
                            t.GetGenericArguments()
                            |> Seq.map (getTypeName useds)
                            |> String.concat ","
                        String.concat "" [                    
                            Type.getName t
                            "<"
                            args
                            ">"
                        ]
                | false -> Type.getName t
                
*)