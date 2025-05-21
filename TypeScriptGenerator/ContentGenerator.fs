namespace TypeScriptGenerator

open System
open System.Reflection
open System.Collections.Generic
open TypeScriptGenerator.XmlDocParser // Added
open TypeScriptGenerator.Models // Ensure TypeOptions is accessible

[<AutoOpen>]
module private ContentGenerator =
    let getProperties (t:Type) =
        t.GetProperties()
        |> Seq.filter (fun p -> p.DeclaringType = t)
        |> Seq.filter Configuration.filterProperty

    let getPropertyName p =
        match Configuration.converteProperty p with
        | Some n -> n
        | None   -> p.Name |> String.toCamelCase

    // generateExportType now takes TypeOptions (o) to access o.XmlDocs
    let generateExportType (o: TypeOptions) (imports: TypeHashSet) (t: Type) =
        let memberName = XmlDocParser.GetXmlMemberNameForType t
        let summary = o.XmlDocs |> Map.tryFind memberName |> Option.flatten
        let docLines = XmlDocParser.formatJsDoc summary "" // No indent for type declaration block

        let typeString =             
            match t with
            | t when t.IsInterface -> "interface"
            | t when (t.IsAbstract && not t.IsSealed) -> "abstract class"
            | t when t.IsEnum -> "enum"
            | _ ->  "class"
        
        let extendString = 
            if isNull t.BaseType || TS.isBuildIn t.BaseType || t.IsEnum then None
            else Some ("extends " + TS.getName imports t.BaseType)
        
        let implString =
            if not t.IsInterface then None
            else 
                let ifs = t.GetInterfaces()
                let baseIfs = if isNull t.BaseType then Array.empty<Type> else t.BaseType.GetInterfaces()
                let ifsString =
                    ifs
                    |> Seq.except baseIfs
                    |> Seq.map (TS.getName imports)
                    |> String.concat ", "

                let key = if t.IsClass then "implements " else "extends "
                if String.IsNullOrEmpty ifsString then None
                else Some(key + ifsString)                

        let typeDef =
            [   Some "export"
                Some typeString
                Some (TS.getName imports t)
                extendString
                implString
                Some "{" ] 
            |> List.choose id
            |> String.concat " " 

        if List.isEmpty docLines then typeDef
        else (String.concat Environment.NewLine docLines) + Environment.NewLine + typeDef

module internal EnumContentGenerator = 
    let generateContent (o: TypeOptions) = // o contains XmlDocs
        let imports = Cache.getImportTypes o.Type        
        let t = o.Type
        // Pass o to generateExportType
        let typeNameLine = generateExportType o imports t 

        let fields = 
            Enum.GetValues(t) 
            |> unbox
            |> Seq.map (fun eValue -> 
                let enumName = Enum.GetName(t, eValue)
                let fieldInfo = t.GetField(enumName)
                let memberName = XmlDocParser.GetXmlMemberNameForField fieldInfo
                let summary = o.XmlDocs |> Map.tryFind memberName |> Option.flatten
                let docLines = XmlDocParser.formatJsDoc summary TS.indent
                
                let fieldDef = sprintf "%s%s = %i" TS.indent enumName (unbox eValue)
                if List.isEmpty docLines then fieldDef
                else (String.concat Environment.NewLine docLines) + Environment.NewLine + fieldDef)
            |> String.concat ("," + Environment.NewLine)


        String.concat Environment.NewLine [
            typeNameLine
            fields // Already includes indent and docs
            yield! o.CodeSnippets // These are already indented
            "}"
        ]
        ,List.empty<Type>

module internal ConstContentGenerator =
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    // generateFields now takes TypeOptions (o) for xmlDocs
    let private generateFields (o: TypeOptions) (indent:string) (t:Type) = 
        t.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> 
            let memberName = XmlDocParser.GetXmlMemberNameForField fi
            let summary = o.XmlDocs |> Map.tryFind memberName |> Option.flatten
            let docLines = XmlDocParser.formatJsDoc summary indent

            let fieldDef = sprintf "%sexport const %s = %s;" indent fi.Name (getConstValue fi)
            if List.isEmpty docLines then fieldDef
            else (String.concat Environment.NewLine docLines) + Environment.NewLine + fieldDef)
        |> String.concat Environment.NewLine

    // generateNests now takes TypeOptions (oParent) for xmlDocs, and tCurrent for the current type being processed
    let rec private generateNests (oParent: TypeOptions) (indent:string) (tCurrent:Type) =
        let typeMemberName = XmlDocParser.GetXmlMemberNameForType tCurrent
        let typeSummary = oParent.XmlDocs |> Map.tryFind typeMemberName |> Option.flatten // Use parent's XmlDoc map for lookup
        let typeDocLines = XmlDocParser.formatJsDoc typeSummary indent

        let exportLine = sprintf "%sexport module %s {" indent (TS.getNameWithoutGeneric tCurrent)
        let exportLineWithDocs = 
            if List.isEmpty typeDocLines then exportLine
            else (String.concat Environment.NewLine typeDocLines) + Environment.NewLine + exportLine
        
        // Create a TypeOptions for the current nested type, using its own type and parent's XmlDocs
        let currentOpts = { oParent with Type = tCurrent }

        let fields = generateFields currentOpts (indent + TS.indent) tCurrent
        let nests =
            tCurrent.GetNestedTypes()
            |> List.ofArray
            // Pass oParent for XmlDocs consistency, but recurse with nt (next type)
            |> List.map (fun nt -> generateNests oParent (indent + TS.indent) nt) 
        
        String.concat Environment.NewLine [
            yield String.Empty 
            yield exportLineWithDocs
            yield fields
            yield! nests
            yield indent + "}"
        ]

    let generateContent (o: TypeOptions) = // o contains XmlDocs
        let t = o.Type
        let fields = generateFields o String.Empty t 
        // For initial call to generateNests, pass o (which contains the top-level type and its XmlDocs)
        // and the nested type 'nt'
        let nests = t.GetNestedTypes() |> Array.map (fun nt -> generateNests o String.Empty nt) |> Array.toList

        let content =
            if String.IsNullOrEmpty fields && List.isEmpty nests then String.Empty
            else String.concat Environment.NewLine (fields :: nests @ o.CodeSnippets)
        content, List.empty<Type>


module internal ModelContentGenerator =   
    let generateImports (currentPath:string) (importedTypes:Type seq) =
        let generateImport (t:Type) =
            let importPath = FilePathGenerator.generatePath t
            let relativePath = FilePathGenerator.getRelativePath currentPath importPath 
            sprintf "import { %s } from '%s';" (TS.getNameWithoutGeneric t) relativePath
        
        (importedTypes            
        |> Seq.map generateImport
        |> String.concat Environment.NewLine
        )
        + Environment.NewLine
    
    // generateProperty now takes TypeOptions (o) for xmlDocs
    let generateProperty (o: TypeOptions) (ts:Type HashSet) (p:PropertyInfo) =
        let name = getPropertyName p
        let typeName = TS.getName ts p.PropertyType

        let memberName = XmlDocParser.GetXmlMemberNameForProperty p
        let summary = o.XmlDocs |> Map.tryFind memberName |> Option.flatten
        let docLines = XmlDocParser.formatJsDoc summary TS.indent

        let propDef = 
            String.concat "" [
                TS.indent
                name
                "?: " 
                typeName
                ";"
            ]
        if List.isEmpty docLines then propDef
        else (String.concat Environment.NewLine docLines) + Environment.NewLine + propDef
         
    let generateContent (o: TypeOptions) = // o contains XmlDocs
        let ``importedTypes&this`` = Cache.getImportTypes o.Type        
        let t = o.Type
        // Pass o to generateExportType
        let typeNameLine = generateExportType o ``importedTypes&this`` t 
        
        let props = 
            t
            |> getProperties
            // Pass o to generateProperty
            |> Seq.map (generateProperty o ``importedTypes&this``) 
            |> String.concat Environment.NewLine
        
        let importedTypes = ``importedTypes&this`` |> Seq.filter (fun u -> u <> t) |> Seq.toList

        String.concat Environment.NewLine [
            if not importedTypes.IsEmpty then yield generateImports o.Path importedTypes
            yield typeNameLine
            yield if String.IsNullOrEmpty props then String.Empty else props
            yield! o.CodeSnippets 
            yield "}"
        ], importedTypes
```
