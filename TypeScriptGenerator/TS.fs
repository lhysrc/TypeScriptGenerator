module internal TS
open Type
open System
open System.Collections.Generic

type TSBuildInType = {
    TypeName :string
    InitValue:string option
}

let indent = "  ";

let private buildinTypes = readOnlyDict [
    typeof<Void>,           {TypeName = "void";InitValue = None}
    typeof<obj>,            {TypeName = "any"; InitValue = None}

    typeof<String>,         {TypeName = "string";InitValue = None}
    typeof<Char>,           {TypeName = "string";InitValue = None}
    typeof<Guid>,           {TypeName = "string";InitValue = None}
    //typeof<Nullable<Char>>, {TypeName = "string";InitValue = None}
    //typeof<Nullable<Guid>>, {TypeName = "string";InitValue = None}
    
    typeof<Uri>,            {TypeName = "string";InitValue = None}

    typeof<int8>,           {TypeName = "number";InitValue = Some "0"}
    typeof<uint8>,          {TypeName = "number";InitValue = Some "0"}
    typeof<int16>,          {TypeName = "number";InitValue = Some "0"}
    typeof<uint16>,         {TypeName = "number";InitValue = Some "0"}
    typeof<int32>,          {TypeName = "number";InitValue = Some "0"}
    typeof<uint32>,         {TypeName = "number";InitValue = Some "0"}
    typeof<int64>,          {TypeName = "number";InitValue = Some "0"}
    typeof<uint64>,         {TypeName = "number";InitValue = Some "0"}
    //typeof<Nullable<int32>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<uint32>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<int8>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<uint8>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<int16>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<uint16>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<int64>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<uint64>>,{TypeName = "number";InitValue = None}

    typeof<float>,          {TypeName = "number";InitValue = Some "0"}
    typeof<float32>,        {TypeName = "number";InitValue = Some "0"}
    typeof<decimal>,        {TypeName = "number";InitValue = Some "0"}
    //typeof<Nullable<float>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<float32>>,{TypeName = "number";InitValue = None}
    //typeof<Nullable<decimal>>,{TypeName = "number";InitValue = None}

    typeof<bool>,           {TypeName = "boolean";InitValue = Some "false"}
    //typeof<Nullable<bool>>,{TypeName = "boolean";InitValue = None}

    typeof<DateTime>,       {TypeName = "Date";InitValue = Some "new Date()"}
    typeof<DateTimeOffset>, {TypeName = "Date";InitValue = Some "new Date()"}
    //typeof<Nullable<DateTime>>,{TypeName = "Date";InitValue = None}
    //typeof<Nullable<DateTimeOffset>>,{TypeName = "Date";InitValue = None}
]
   
let isBuildIn (t:Type) =
    buildinTypes.ContainsKey t

let addImportType (ts:Type HashSet) (t:Type) =
    if t.IsGenericParameter then ()
    else if t.IsGenericType then ts.Add (t.GetGenericTypeDefinition()) |> ignore
    else ts.Add t |> ignore

let (|TSBuildIn|_|) (t:Type) = 
    match buildinTypes.TryGetValue t with
    | true, v -> Some v.TypeName
    | false,_ -> None

let (|TSTuple|_|) (t:Type) =
    if Reflection.FSharpType.IsTuple t then Some (Reflection.FSharpType.GetTupleElements t)
    else None

let (|TSMap|_|) (t:Type) = 
    getDictionaryTypes t
    
let (|TSArray|_|) (t:Type) = 
    getCollectionType t

let getNameWithoutGeneric (t:Type) =
    t 
    |> Configuration.converteTypeName
    |> Option.defaultValue (Type.getNameWithoutGeneric t)

let rec getName (imports:Type HashSet) (t':Type):string =    
    let t = t' |> unwrap
    match t |> Configuration.converteType |> Option.defaultValue t with
    | TSBuildIn n -> n
    | TSTuple ts -> 
        "[" + (
            ts 
            |> Array.map (getName imports)
            |> String.concat ", "
        ) + "]"
    | TSMap (k,v) -> 
        match k with
        | k when k = typeof<string> -> "{ [key: string]: " + (getName imports v) + " }" //todo number & guid etc.
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
