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
            if isNull t.BaseType || TS.isBuildIn t.BaseType || t.IsEnum then String.Empty
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
        let usedPath = FilePathGenerator.generatePath t
        let relativePath = FilePathGenerator.getRelativePath currentPath usedPath 
        sprintf "import {%s} from '%s';" (Type.getName t) relativePath

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
        let usedTypes'Self = Type.getUsedTypes o.Type        

        let t = o.Type
        let typeName = generateExportType usedTypes'Self t
        let props = 
            t.GetProperties()
            |> Seq.filter (fun p -> p.DeclaringType = t)
            |> Seq.map (generateProp usedTypes'Self)
            |> String.concat Environment.NewLine

        let usedTypes = usedTypes'Self |> Seq.filter (fun u -> u <> t)
        let imports = 
            usedTypes            
            |> Seq.map (generateImport o.Path)
            |> String.concat Environment.NewLine
        String.concat Environment.NewLine [
            imports
            typeName
            props
            "}"
        ], usedTypes
