namespace TypeScriptGenerator
open System
open System.Reflection
open System.Collections.Generic

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

    let generateExportType (imports:Type HashSet) (t: Type) =
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
            //if t.IsEnum then None
            else 
                let ifs = 
                    t.GetInterfaces()
                    //|> Seq.filter (fun i -> Seq.forall (fun ip -> Seq.exists (fun tp -> getPropertyName ip = getPropertyName tp) (getProperties t)) (getProperties i))
                let baseIfs = if isNull t.BaseType then Array.empty<Type> else t.BaseType.GetInterfaces()
                let ifsString =
                    ifs
                    |> Seq.except baseIfs
                    |> Seq.map (TS.getName imports)
                    |> String.concat ", "

                let key = if t.IsClass then "implements " else "extends "
                if String.IsNullOrEmpty ifsString then 
                    None
                else 
                    Some(key + ifsString)                

        [   Some "export"
            Some typeString
            Some (TS.getName imports t)
            extendString
            implString
            Some "{"        ] 
        |> List.choose id
        |> String.concat " " 

module internal EnumContentGenerator = 
    let generateContent (o: TypeOptions) =
        let imports = Cache.getImportTypes o.Type        
        let t = o.Type
        let typeName = generateExportType imports t

        let fields = 
            Enum.GetValues(t) 
            |> unbox
            |> Seq.map (fun e -> sprintf "%s = %i" (Enum.GetName(t, e)) e)
            |> String.concat ("," + Environment.NewLine + TS.indent)


        String.concat Environment.NewLine [
            typeName
            TS.indent + fields
            yield! o.CodeSnippets
            "}"
        ]
        ,List.empty<Type>

module internal ConstContentGenerator =
    let private getConstValue (fi:FieldInfo) =
        sprintf "%A" (fi.GetRawConstantValue())

    let private generateFields (indent:string) (t:Type) =
        t.GetFields(BindingFlags.Public ||| BindingFlags.Static ||| BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> sprintf "%sexport const %s = %s;" indent fi.Name (getConstValue fi))
        |> String.concat Environment.NewLine

    let rec private generateNests (indent:string) (t:Type) =
        let export = sprintf "%sexport module %s {" indent (TS.getNameWithoutGeneric t)
        let fields = generateFields (indent + TS.indent) t
        let nests =
            t.GetNestedTypes()
            |> List.ofArray
            |> List.map (generateNests (indent + TS.indent))
        String.concat Environment.NewLine [
            yield String.Empty
            yield export
            yield fields
            yield! nests
            yield indent + "}"
        ]

    let generateContent (o: TypeOptions) =
        let t = o.Type
        let fields = generateFields String.Empty t

        let nests = t.GetNestedTypes() |> Array.map (generateNests String.Empty) |> Array.toList

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
        //todo 同名泛型类型引入？可通过TypeNameConverter避免
        
        (
        importedTypes            
        |> Seq.map generateImport
        |> String.concat Environment.NewLine
        )
        + Environment.NewLine

    let generateProperty (ts:Type HashSet) (p:PropertyInfo) =
        let name = getPropertyName p
        let typeName = TS.getName ts p.PropertyType

        String.concat "" [
            TS.indent
            name
            "?: "
            typeName
            ";"
        ]
         
    let generateContent (o: TypeOptions) =
        let ``importedTypes&this`` = Cache.getImportTypes o.Type        

        let t = o.Type
        let typeName = generateExportType ``importedTypes&this`` t
        let props = 
            t
            |> getProperties
            |> Seq.map (generateProperty ``importedTypes&this``)
            |> String.concat Environment.NewLine
        
        //printfn "%s" "-------------"
        //printfn "%s" o.Type.Name
        let importedTypes = ``importedTypes&this`` |> Seq.filter (fun u -> u <> t) |> Seq.toList

        String.concat Environment.NewLine [
            if not importedTypes.IsEmpty then yield generateImports o.Path importedTypes
            yield typeName
            yield if String.IsNullOrEmpty props then String.Empty else props
            yield! o.CodeSnippets
            yield "}"
        ], importedTypes
