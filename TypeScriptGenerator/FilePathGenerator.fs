namespace TypeScriptGenerator

open System
open System.IO

module internal FilePathGenerator =
    let getDirName (name:string) =
        String.toKebabCase name

    let getFileName (t:Type) =        
        Type.getName t
        |> String.toKebabCase

    let generatePath (t:Type) =
        let dir = 
            t.Namespace.Split('.')
            |> Seq.map getDirName
            |> String.concat (string Path.DirectorySeparatorChar)
        Path.Combine (dir, getFileName t)
