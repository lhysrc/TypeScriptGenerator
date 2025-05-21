module TypeScriptGenerator.XmlDocParser

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open System.Linq

let private getXmlMemberName (memberInfo: MemberInfo) =
    let t = memberInfo.DeclaringType
    let typeName =
        match t with
        | null -> ""
        | t when t.IsGenericType -> t.FullName.Split('`').[0] + "`" + t.GetGenericArguments().Length.ToString()
        | t -> t.FullName.Replace("+", ".")
    
    match memberInfo.MemberType with
    | MemberTypes.Property -> sprintf "P:%s.%s" typeName memberInfo.Name
    | MemberTypes.Field -> sprintf "F:%s.%s" typeName memberInfo.Name
    | MemberTypes.NestedType -> sprintf "T:%s.%s" typeName memberInfo.Name // This is for nested types, top-level types are handled by GetXmlMemberNameForType
    | _ -> failwithf "Unsupported member type: %A" memberInfo.MemberType

let GetXmlMemberNameForType (t: Type) =
    let baseName = if t.IsNested then t.FullName.Replace("+", ".") else t.FullName
    if t.IsGenericType then
        sprintf "T:%s`%d" (baseName.Split('`').[0]) (t.GetGenericArguments().Length)
    else
        sprintf "T:%s" baseName

let GetXmlMemberNameForProperty (p: PropertyInfo) =
    let typeName = 
        match p.DeclaringType with
        | null -> ""
        | dt when dt.IsGenericType -> dt.FullName.Split('`').[0] + "`" + dt.GetGenericArguments().Length.ToString()
        | dt -> dt.FullName.Replace("+", ".")
    sprintf "P:%s.%s" typeName p.Name

let GetXmlMemberNameForField (f: FieldInfo) =
    let typeName =
        match f.DeclaringType with
        | null -> ""
        | dt when dt.IsGenericType -> dt.FullName.Split('`').[0] + "`" + dt.GetGenericArguments().Length.ToString()
        | dt -> dt.FullName.Replace("+", ".")
    sprintf "F:%s.%s" typeName f.Name

let loadXmlDocs (assemblyPath: string) : Map<string, string option> =
    let xmlDocPath = Path.ChangeExtension(assemblyPath, ".xml")
    if File.Exists xmlDocPath then
        try
            let doc = XDocument.Load(xmlDocPath)
            doc.Descendants("member")
            |> Seq.choose (fun memberEl ->
                let nameAttr = memberEl.Attribute(XName.Get "name")
                match nameAttr with
                | null -> None
                | attr ->
                    let summaryEl = memberEl.Element(XName.Get "summary")
                    let summary =
                        match summaryEl with
                        | null -> None
                        | el -> Some (el.Value.Trim())
                    Some (attr.Value, summary)
            )
            |> Map.ofSeq
        with
        | ex -> 
            eprintfn "Warning: Could not parse XML documentation file '%s': %s" xmlDocPath ex.Message
            Map.empty
    else
        Map.empty

let formatJsDoc (summary: string option) (indent: string) : string list =
    match summary with
    | None -> []
    | Some summaryText ->
        let lines = summaryText.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
        [ indent + "/**" ]
        @ (lines |> List.ofArray |> List.map (fun line -> indent + " * " + line))
        @ [ indent + " */" ]
