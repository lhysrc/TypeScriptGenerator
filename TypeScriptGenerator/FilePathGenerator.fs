namespace TypeScriptGenerator

open System
open System.IO

module internal FilePathGenerator =
    let getDirName (name:string) =
        String.toKebabCase name

    let getFileName (t:Type) =        
        Type.getName'n t
        |> String.toKebabCase

    let generatePath (t:Type) =
        let dir = 
            t.Namespace.Split('.')
            |> Seq.map getDirName
            |> String.concat (string Path.DirectorySeparatorChar)
        Path.Combine (dir, getFileName t)

    let getRelativePath relativeTo path =
        let uri = Uri (relativeTo |> Path.GetFullPath);
        let mutable rel = 
            uri.MakeRelativeUri (Uri(path |> Path.GetFullPath))
            |> string
            |> Uri.UnescapeDataString
        rel <- rel.Replace ('\\', '/')
        if not (rel.StartsWith (".")) then
            "./" + rel
        else
            rel
    