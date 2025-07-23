namespace TypeScriptGenerator

module internal XmlDoc

open System
open System.IO
open System.Reflection
open System.Xml.Linq
open System.Collections.Generic

let private cache = Dictionary<Assembly, Dictionary<string,string>>()

let private loadDocs (asm:Assembly) =
    match cache.TryGetValue asm with
    | true, d -> d
    | _ ->
        let xmlPath = Path.ChangeExtension(asm.Location, "xml")
        let dict = Dictionary<string,string>()
        if File.Exists xmlPath then
            let doc = XDocument.Load xmlPath
            for m in doc.Descendants(XName.Get "member") do
                let nameAttr = m.Attribute(XName.Get "name")
                if not (isNull nameAttr) then
                    let summary =
                        match m.Element(XName.Get "summary") with
                        | null -> null
                        | s -> s.Value.Trim()
                    if not (String.IsNullOrWhiteSpace summary) then
                        dict.[nameAttr.Value] <- summary
        cache.[asm] <- dict
        dict

let private typeName (t:Type) =
    let name = t.FullName.Replace('+', '.')
    if t.IsGenericType then sprintf "%s`%d" name (t.GetGenericArguments().Length)
    else name

let private memberId (m:MemberInfo) =
    match m with
    | :? Type as t -> sprintf "T:%s" (typeName t)
    | :? PropertyInfo as p -> sprintf "P:%s.%s" (typeName p.DeclaringType) p.Name
    | :? FieldInfo as f -> sprintf "F:%s.%s" (typeName f.DeclaringType) f.Name
    | _ -> ""

let getSummary (m:MemberInfo) =
    let asm = m.Module.Assembly
    let dict = loadDocs asm
    match dict.TryGetValue(memberId m) with
    | true, v -> Some v
    | _ -> None
