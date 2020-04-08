namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection
open System.Text
open System.Collections.Generic

[<AutoOpen>]
module internal Common =
    let generateExportType (ts:Type HashSet) (t: Type) =
        let typeString = 
            match t with
            | t when t.IsInterface -> "interface"
            | t when (t.IsAbstract && not t.IsSealed) -> "abstract class"
            | t when t.IsEnum -> "enum"
            | _ ->  "class"
        
        let extendString = 
            if isNull t.BaseType || TS.isBuildIn t.BaseType then String.Empty
            else "extends " + TS.getTypeName ts t.BaseType

        String.concat " " [
            "export"
            typeString
            TS.getTypeName ts t
            extendString
            "{"
        ]

module internal EnumContentGenerator = 
    let generateContent (o: TypeOption) =
        let usedTypes = Type.getUsedTypes o.Type        
        let t = o.Type
        let typeName = generateExportType usedTypes t

        let props = 
            Enum.GetValues(t) 
            |> unbox
            |> Seq.map (fun e -> sprintf "%s = %i" (Enum.GetName(t, e)) e)
            |> String.concat ("," + Environment.NewLine)


        String.concat Environment.NewLine [
            typeName
            props
            "}"
        ]
        ,Seq.empty<Type>

module internal ConstContentGenerator =
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    let generateContent (t: TypeOption) =
        t.Type.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> sprintf "export const %s = %s;" fi.Name (getConstValue fi))
        |> String.concat Environment.NewLine
        ,Seq.empty<Type>


module internal ModelContentGenerator =   

    let generateImport (currentPath:string) (t:Type) =
        sprintf "import {%s} from '%s';" (Type.getName t) "."

    let generateProp (ts:Type HashSet) (p:PropertyInfo) =
        TS.addUsedType ts p.PropertyType

        let name = p.Name |> String.toCamelCase
        let typeName = TS.getTypeName ts p.PropertyType

        String.concat "" [
            name
            "?: "
            typeName
            ";"
        ]

    let generateContent (o: TypeOption) =
        let usedTypes = Type.getUsedTypes o.Type        

        let t = o.Type
        let typeName = generateExportType usedTypes t
        let props = 
            t.GetProperties()
            |> Seq.map (generateProp usedTypes)
            |> String.concat Environment.NewLine

        let imports =
            usedTypes
            |> Seq.filter (fun u -> u <> t)
            |> Seq.map (generateImport "")
            |> String.concat Environment.NewLine
        String.concat Environment.NewLine [
            imports
            typeName
            props
            "}"
        ], usedTypes :> Type seq
