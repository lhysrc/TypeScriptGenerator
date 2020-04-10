﻿namespace TypeScriptGenerator
open System
open System.Reflection
open System.Collections.Generic

[<AutoOpen>]
module private ContentGenerator =
    let generateExportType (ts:Type HashSet) (t: Type) =
        let typeString = 
            match t with
            | t when t.IsInterface -> "interface"
            | t when (t.IsAbstract && not t.IsSealed) -> "abstract class"
            | t when t.IsEnum -> "enum"
            | _ ->  "class"
        
        let extendString = 
            if isNull t.BaseType || TS.isBuildIn t.BaseType || t.IsEnum then String.Empty
            else "extends " + TS.getTypeName ts t.BaseType
        
        let implString =
            if t.IsEnum then String.Empty
            else 
                let ifs = t.GetInterfaces()
                let baseIfs = if isNull t.BaseType then Array.empty<Type> else t.BaseType.GetInterfaces()
                let ifsString =
                    ifs
                    |> Seq.except baseIfs
                    |> Seq.map (TS.getTypeName ts)
                    |> String.concat ", "

                let key = if t.IsClass then "implements " else "extends "
                if String.IsNullOrEmpty ifsString then 
                    String.Empty 
                else 
                    key + ifsString
                

        String.concat " " [
            "export"
            typeString
            TS.getTypeName ts t
            extendString
            implString
            "{"
        ]

module internal EnumContentGenerator = 
    let generateContent (o: TypeOption) =
        let usedTypes = Type.getUsedTypes o.Type        
        let t = o.Type
        let typeName = generateExportType usedTypes t

        let fields = 
            Enum.GetValues(t) 
            |> unbox
            |> Seq.map (fun e -> sprintf "%s = %i" (Enum.GetName(t, e)) e)
            |> String.concat ("," + Environment.NewLine + TS.indent)


        String.concat Environment.NewLine [
            typeName
            TS.indent + fields
            "}"
        ]
        ,List.empty<Type>

module internal ConstContentGenerator =
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    let generateContent (t: TypeOption) =
        t.Type.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> sprintf "export const %s = %s;" fi.Name (getConstValue fi))
        |> String.concat Environment.NewLine
        ,List.empty<Type>


module internal ModelContentGenerator =   

    let generateImport (currentPath:string) (t:Type) =
        let usedPath = FilePathGenerator.generatePath t
        let relativePath = FilePathGenerator.getRelativePath currentPath usedPath 
        sprintf "import { %s } from '%s';" (Type.getName t) relativePath

    let generateProp (ts:Type HashSet) (p:PropertyInfo) =

        let name = p.Name |> String.toCamelCase
        let typeName = TS.getTypeName ts p.PropertyType

        String.concat "" [
            name
            "?: "
            typeName
            ";"
        ]

    let generateContent (o: TypeOption) =
        let ``usedTypes&Self`` = Type.getUsedTypes o.Type        

        let t = o.Type
        let typeName = generateExportType ``usedTypes&Self`` t
        let props = 
            t.GetProperties()
            |> Seq.filter (fun p -> p.DeclaringType = t)
            |> Seq.map (generateProp ``usedTypes&Self``)
            |> String.concat (Environment.NewLine + TS.indent)
        
        //printfn "%s" "-------------"
        //printfn "%s" o.Type.Name
        let usedTypes = ``usedTypes&Self`` |> Seq.filter (fun u -> u <> t) |> Seq.toList
        let imports = 
            usedTypes            
            |> Seq.map (generateImport o.Path)
            |> String.concat Environment.NewLine
        String.concat Environment.NewLine [
            imports
            typeName
            TS.indent + props
            "}"
        ], usedTypes
