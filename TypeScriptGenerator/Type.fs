module internal Type
open System
open System.Collections.Generic

let isStatic (t:Type) =
    t.IsAbstract && t.IsSealed

/// 名字+泛型参数个数
let getName (t:Type) =
    if t.IsInterface && t.Name.StartsWith("I") && Char.IsUpper t.Name.[1] 
    then t.Name.Substring(1)
    else 
        t.Name

let getNameWithoutGeneric (t:Type) =    
    let trimGeneric (name:string) = 
        let num = name.IndexOf('`');
        if num > -1 then name.Substring(0, num) else name
   
    t |> getName |> trimGeneric
     
let getCollectionType (t:Type)=
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

let getDictionaryTypes (t:Type) =
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
    let under = Nullable.GetUnderlyingType t 
    if isNull under then t else under
