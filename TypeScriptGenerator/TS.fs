module internal TS
open Type
open System
open System.Collections.Generic

type SystemType = {
    Key : Type
    Value :string
}
let indent = "  ";
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
//let private cache = dict Seq.empty<Type * string>;
   
let isBuildIn (t:Type) =
    buildinTypes |> List.exists (fun i->i.Key = t)

let addImportType (ts:Type HashSet) (t:Type) =
    let t' = (*unwrap*) t
    if (*isBuildIn t' ||*) t.IsGenericParameter then ()
    else if t.IsGenericType then ts.Add (t.GetGenericTypeDefinition()) |> ignore
    else ts.Add t' |> ignore


let (|TSBuildIn|_|) (t:Type) = 
    match Configuration.converteType t with
    | Some n -> Some n // tsBuildIns |> List.tryFind (fun i-> n = i)
    | None   -> 
    match buildinTypes |> List.tryFind (fun i->i.Key = t) with
    | Some st -> Some st.Value
    | None    -> None

let (|TSTuple|_|) (t:Type) =
    if Reflection.FSharpType.IsTuple t then Some (Reflection.FSharpType.GetTupleElements t)
    else None

let (|TSMap|_|) (t:Type) = 
    getMapType t
    
let (|TSArray|_|) (t:Type) = 
    getArrayType t

let getNameWithoutGeneric (t:Type) =
    match Configuration.converteTypeName t with
    | Some n -> n
    | None -> Type.getNameWithoutGeneric t

let rec getName (imports:Type HashSet) (t:Type):string =    
    match (unwrap t) with
    | TSBuildIn n -> n
    | TSTuple ts -> 
        "[" + (
            ts 
            |> Array.map (getName imports)
            |> String.concat ", "
        ) + "]"
    | TSMap (k,v) -> 
        match k with
        | k when k = typeof<string> -> "{ [key: string]: " + (getName imports v) + " }"
        | _ -> sprintf "Map<%s,%s>" (getName imports k)(getName imports v)
    | TSArray t -> (getName imports t) + "[]"
    | t -> 
        addImportType imports t
        match t.IsGenericType with
        | true ->   
            let args = 
                t.GetGenericArguments()
                |> Seq.map (getName imports)
                |> String.concat ", "
            String.concat "" [                    
                getNameWithoutGeneric t
                "<"
                args
                ">"
            ]
        | false -> getNameWithoutGeneric t
