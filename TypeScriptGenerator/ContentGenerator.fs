namespace TypeScriptGenerator
open System
open System.Reflection
open System.Collections.Generic
open DocGenerator // Added for JSDoc generation

[<AutoOpen>]
module private ContentGeneratorHelpers =
    // Helper to prepend JSDoc with optional indentation
    let prependJsDocIfAny (doc: string) (code: string) (indentation: string) =
        if String.IsNullOrWhiteSpace doc then 
            // If code itself needs indenting (e.g. a property or enum member line)
            if String.IsNullOrWhiteSpace indentation then code else indentation + code
        else 
            let docLines = doc.TrimEnd().Split([|Environment.NewLine|], StringSplitOptions.None)
            let indentedDoc =
                docLines
                |> Array.map (fun line -> indentation + line) // Indent each line of the JSDoc
                |> String.concat Environment.NewLine
            
            // Apply indentation to the code string if it's not empty and indentation is provided
            let indentedCode = 
                if String.IsNullOrWhiteSpace code then ""
                else if String.IsNullOrWhiteSpace indentation then code
                else indentation + code

            if String.IsNullOrWhiteSpace code then indentedDoc // Only JSDoc
            else sprintf "%s%s%s" indentedDoc Environment.NewLine indentedCode // JSDoc, newline, then indented code

    let getProperties (t:Type) =
        t.GetProperties()
        |> Seq.filter (fun p -> p.DeclaringType = t)
        |> Seq.filter Configuration.filterProperty

    let getPropertyName p =
        match Configuration.converteProperty p with
        | Some n -> n
        | None   -> p.Name |> String.toCamelCase

    let generateExportType (imports:Type HashSet) (t: Type) =
        // This function generates the "export class/interface/enum Name {" part.
        // JSDoc for the type itself will be prepended by the calling content generator.
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

        [   Some "export"
            Some typeString
            Some (TS.getName imports t) // This is the MyType<T> part
            extendString
            implString
            Some "{"        ] 
        |> List.choose id
        |> String.concat " " 

module internal EnumContentGenerator = 
    open ContentGeneratorHelpers

    let generateContent (o: TypeOptions) =
        let imports = Cache.getImportTypes o.Type        
        let t = o.Type
        
        let typeXmlKey = $"T:{t.FullName}"
        let typeDoc = DocGenerator.generateJsDoc typeXmlKey
        let typeNameDeclaration = ContentGeneratorHelpers.generateExportType imports t // Generates "export enum MyEnum {"
        // Prepend JSDoc to the type declaration, no extra indent for the declaration line itself.
        let typeOutput = prependJsDocIfAny typeDoc typeNameDeclaration "" 

        let fields = 
            Enum.GetValues(t) 
            |> unbox // Assuming this gives a sequence of enum values
            |> Seq.map (fun enumValue -> 
                let fieldName = Enum.GetName(t, enumValue)
                let fieldXmlKey = $"F:{t.FullName}.{fieldName}" // XML key for enum field
                let fieldDoc = DocGenerator.generateJsDoc fieldXmlKey
                let fieldCode = sprintf "%s = %i" fieldName (Convert.ToInt32 enumValue) // Generate "MemberName = 0"
                // Prepend JSDoc to the field code, with TS.indent for both JSDoc and the field code line.
                prependJsDocIfAny fieldDoc fieldCode TS.indent 
            )
            |> String.concat ("," + Environment.NewLine) // Join with comma and newline. Subsequent lines are already indented.

        String.concat Environment.NewLine [
            typeOutput // "/** JSDoc */\nexport enum MyEnum {"
            if not (String.IsNullOrWhiteSpace fields) then fields else "" // Add fields if any
            yield! o.CodeSnippets
            "}"
        ]
        ,List.empty<Type>

module internal ConstContentGenerator =
    open ContentGeneratorHelpers
    // Note: JSDoc for const fields is not explicitly requested in this subtask,
    // but could be added similarly if needed, likely using "F:Namespace.Type.ConstName"
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    let private generateFields (indent:string) (t:Type) =
        t.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> 
            let fieldXmlKey = $"F:{fi.DeclaringType.FullName}.{fi.Name}" // JSDoc key for const field
            let fieldDoc = DocGenerator.generateJsDoc fieldXmlKey
            let fieldCode = sprintf "export const %s = %s;" fi.Name (getConstValue fi)
            prependJsDocIfAny fieldDoc fieldCode indent)
        |> String.concat Environment.NewLine

    let rec private generateNests (indent:string) (t:Type) =
        let typeXmlKey = $"T:{t.FullName}" // Assuming nested types are also T:
        let typeDoc = DocGenerator.generateJsDoc typeXmlKey
        let exportString = sprintf "export module %s {" (TS.getNameWithoutGeneric t)
        // JSDoc for the module line itself, then its code, with specified indent
        let exportLine = prependJsDocIfAny typeDoc exportString indent


        let fields = generateFields (indent + TS.indent) t
        let nests =
            t.GetNestedTypes()
            |> List.ofArray
            |> List.map (generateNests (indent + TS.indent))
        
        // Construct the module block carefully
        let moduleBlockParts = System.Collections.Generic.List<string>()
        moduleBlockParts.Add(exportLine) // JSDoc + "export module Name {"
        if not (String.IsNullOrWhiteSpace fields) then moduleBlockParts.Add(fields)
        nests |> List.iter moduleBlockParts.Add
        moduleBlockParts.Add(indent + "}")

        // Add a preceding newline if this module block is not the very first thing.
        // This is tricky to get perfect without context, but often a blank line is added before a module.
        String.concat Environment.NewLine moduleBlockParts


    let generateContent (o: TypeOptions) =
        let t = o.Type
        // For a static class treated as a module of consts, the "T:Type.FullName" doc might apply to the implicit module.
        // However, the current structure generates loose consts and nested modules.
        // Let's assume top-level consts don't get a collective JSDoc block from the type itself here.
        // Individual consts get JSDoc via generateFields. Nested modules (types) get JSDoc via generateNests.

        let fields = generateFields String.Empty t // Top-level consts in a "static class"

        let nests = 
            t.GetNestedTypes() 
            |> Array.map (generateNests String.Empty) // Pass String.Empty for top-level indent
            |> Array.toList

        let allContent = List.filter (fun s -> not (String.IsNullOrWhiteSpace s)) (fields :: nests)
        
        let content =
            if List.isEmpty allContent then String.Empty
            else String.concat (Environment.NewLine + Environment.NewLine) allContent // Add extra newline between const/module blocks
        
        // Append CodeSnippets
        let finalContent = 
            if not (List.isEmpty o.CodeSnippets) then
                if String.IsNullOrWhiteSpace content then String.concat Environment.NewLine o.CodeSnippets
                else content + Environment.NewLine + (String.concat Environment.NewLine o.CodeSnippets)
            else content

        finalContent, List.empty<Type>


module internal ModelContentGenerator =   
    open ContentGeneratorHelpers

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

    let generateProperty (ts:Type HashSet) (p:PropertyInfo) =
        let name = ContentGeneratorHelpers.getPropertyName p // Use helper explicitly
        let typeName = TS.getName ts p.PropertyType

        let propertyXmlKey = $"P:{p.DeclaringType.FullName}.{p.Name}"
        let propertyDoc = DocGenerator.generateJsDoc propertyXmlKey
        
        let propertyCode = 
            String.concat "" [ // The code itself is not indented here, prependJsDocIfAny will add it.
                name
                "?: " // Assuming optional properties
                typeName
                ";"
            ]
        // Prepend JSDoc to property code, with TS.indent for both JSDoc and the property line.
        prependJsDocIfAny propertyDoc propertyCode TS.indent
         
    let generateContent (o: TypeOptions) =
        let ``importedTypes&this`` = Cache.getImportTypes o.Type        
        let t = o.Type
        
        let typeXmlKey = $"T:{t.FullName}"
        let typeDoc = DocGenerator.generateJsDoc typeXmlKey
        let typeNameDeclaration = ContentGeneratorHelpers.generateExportType ``importedTypes&this`` t // Generates "export class MyClass {"
        // Prepend JSDoc to the type declaration, no extra indent for the declaration line itself.
        let typeOutput = prependJsDocIfAny typeDoc typeNameDeclaration "" 

        let props = 
            t
            |> ContentGeneratorHelpers.getProperties // Use helper explicitly
            |> Seq.map (generateProperty ``importedTypes&this``) // generateProperty now returns full JSDoc + code line
            |> String.concat Environment.NewLine 
        
        let importedTypes = ``importedTypes&this`` |> Seq.filter (fun u -> u <> t) |> Seq.toList

        // Assemble the final output string
        let parts = System.Collections.Generic.List<string>()
        if not importedTypes.IsEmpty then parts.Add(generateImports o.Path importedTypes)
        parts.Add(typeOutput) // This is "/** JSDoc */\nexport class MyClass {"
        if not (String.IsNullOrWhiteSpace props) then parts.Add(props)
        o.CodeSnippets |> List.iter parts.Add
        parts.Add("}")

        String.concat Environment.NewLine parts, importedTypes
