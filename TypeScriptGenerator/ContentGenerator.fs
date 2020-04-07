namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection
open System.Text
[<AutoOpen>]
module internal Common =
    let generateExportType (t: Type) =
        let typeString = 
            match t with
            | t when t.IsInterface -> "interface"
            | t when (t.IsAbstract && not t.IsSealed) -> "abstract class"
            | t when t.IsEnum -> "enum"
            | _ ->  "class"
        
        String.concat " " [
            "export"
            typeString
            TS.getTypeName t
            "{"
        ]

module internal EnumContentGenerator = 
    let generateContent (t:Type) =
        let typeName = generateExportType t

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

module internal ConstContentGenerator =
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    let generateContent (t: Type) =
        t.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> sprintf "export const %s = %s;" fi.Name (getConstValue fi))
        |> String.concat Environment.NewLine


module internal ModelPropertyGenerator =    

    let generatorProp (p:PropertyInfo) =
        let name = p.Name |> String.toCamelCase
        let typeName = TS.getTypeName p.PropertyType

        String.concat "" [
            name
            "?: "
            typeName
            ";"
        ]

module internal ModelContentGenerator =   

    let generateContent (t: Type) =
        let typeName = generateExportType t
        let props = 
            t.GetProperties()
            |> Seq.map ModelPropertyGenerator.generatorProp
            |> String.concat Environment.NewLine
        String.concat Environment.NewLine [
            typeName
            props
            "}"
        ]

