module Type
open System
open System.Collections.Generic
let getName (t:Type) =    
    if t.IsInterface && t.Name.StartsWith("I") && Char.IsUpper t.Name.[1] 
    then t.Name.Substring(1)
    else 
        let name = t.Name
        let num = name.IndexOf('`');
        if num > -1 then name.Substring(0, num) else name
        
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

let getUsedTypes (t:Type) =
    HashSet<Type> (seq {
        yield! t.GetProperties() |> Seq.map (fun p->p.PropertyType)
        yield! t.GetInterfaces()
        yield! t.GenericTypeArguments
        yield t.BaseType
    }) |> Seq.map unwrap