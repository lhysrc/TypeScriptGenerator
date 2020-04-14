namespace TypeScriptGenerator

open System
open System.IO

module internal FilePathGenerator =
    let private nsToDir (ns:string) =
        String.toKebabCase ns

    let getFileName (t:Type) =   
        t
        |> Configuration.converteTypeName
        |> Option.defaultValue (Type.getName t)
        |> String.toKebabCase

    let generatePath (t:Type) =
        let dir = 
            t.Namespace.Split('.')
            |> Seq.map nsToDir
            |> String.concat (string Path.DirectorySeparatorChar)
        Path.Combine (dir, getFileName t)

    let getRelativePath relativeTo path =
        let uri = relativeTo |> Path.GetFullPath |> Uri
        let mutable rel = 
            path |> Path.GetFullPath |> Uri 
            |> uri.MakeRelativeUri |> string
            |> Uri.UnescapeDataString
        rel <- rel.Replace ('\\', '/')
        if not (rel.StartsWith (".")) then
            "./" + rel
        else
            rel
    