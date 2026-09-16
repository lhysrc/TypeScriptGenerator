namespace TypeScriptGenerator

module internal Comment

open System
open System.Reflection
open XmlDoc
open Configuration

let private formatLines (indent:string) (text:string) =
    text.Split([|'\r';'\n'|], StringSplitOptions.RemoveEmptyEntries)
    |> Array.map (fun l -> indent + " * " + l.Trim())
    |> Array.toList

let generate (indent:string) (m:MemberInfo) =
    if not (Configuration.isXmlDocEnabled()) then
        []
    else
        let lines = ResizeArray<string>()
        match XmlDoc.getSummary m with
        | Some s when not (String.IsNullOrWhiteSpace s) ->
            lines.AddRange(formatLines indent s)
        | _ -> ()
        let obs = m.GetCustomAttribute<ObsoleteAttribute>()
        if not (isNull obs) then
            let msg = if String.IsNullOrWhiteSpace obs.Message then "" else " " + obs.Message
            lines.Add(indent + " * @deprecated" + msg)
        if lines.Count = 0 then []
        else
            List.concat [ [indent + "/**" ]; List.ofSeq lines; [indent + " */"] ]


