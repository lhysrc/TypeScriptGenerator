﻿module Type
open System
open System.Collections.Generic

let internal loadedTypes = HashSet<Type>()
let internal usedTypes = Dictionary<Type,HashSet<Type>>()
let getUsedTypes (t:Type) =
    match usedTypes.TryGetValue t with
    | true,v -> v
    | false,_ ->
        let v = HashSet<Type>()
        usedTypes.[t] <- v
        v

let isStatic (t:Type) =
    t.IsAbstract && t.IsSealed

let getName (t:Type) =    
    if t.IsInterface && t.Name.StartsWith("I") && Char.IsUpper t.Name.[1] 
    then t.Name.Substring(1)
    else 
        let name = t.Name
        let num = name.IndexOf('`');
        if num > -1 then name.Substring(0, num) else name
     
let getArrayType (t:Type)=
    if t.IsGenericType then
        t.GetInterfaces() 
        |> Array.append [| t |]
        |> Array.tryFind(fun i-> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)
        |> function 
        | Some i -> Some (i.GenericTypeArguments.[0])
        | None -> None
    else if t.IsArray && t.HasElementType then
        Some (t.GetElementType())
    else
        None

let getMapType (t:Type) = //todo 使用[key:string]:TypeName??
    if t.IsGenericType then
        t.GetInterfaces() 
        |> Array.append [| t |]
        |> Array.tryFind(fun i-> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IDictionary<_,_>>)
        |> function 
        | Some i -> Some(i.GenericTypeArguments.[0], i.GenericTypeArguments.[1])
        | None -> None
    else
        None
      

let unwrap (t:Type) =
    if t.IsGenericType then
        t.GetInterfaces() 
        |> Array.append [| t |]
        |> Array.tryFind(fun i-> i.IsGenericType && i.GetGenericTypeDefinition() = typedefof<IEnumerable<_>>)
        |> function 
               | Some i -> i.GenericTypeArguments.[0]
               | None -> t
    else if t.IsArray && t.HasElementType then
       t.GetElementType()
    else
       t

//let getUsedTypes (allTypes:Type seq) (t:Type)  =
//    if isStatic t then 
//        Seq.empty
//    else
//        // printfn "%s" t.Name
//        let ts =
//            (HashSet<Type> (seq {
//                yield! t.GetProperties() |> Seq.map (fun p->p.PropertyType)
//                yield! t.GetInterfaces()
//                yield! t.GenericTypeArguments
//                yield t.BaseType
//            }))
//            |> Seq.filter (fun u -> u <> t && not (isNull u) && Seq.contains t allTypes)
//            |> Seq.map unwrap
//        ts