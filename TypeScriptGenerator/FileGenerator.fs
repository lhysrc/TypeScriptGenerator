namespace TypeScriptGenerator
open System
open System.IO

module internal FileGenerator =
    let generateFile (root:string) (t:Type) = 
        let gFunc =
            match t with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent

        {|
            Name = t.Name
            Content = gFunc t
            Type  = t
            FullPath = Path.Combine(root, FilePathGenerator.generatePath(t) + ".ts")
        |}