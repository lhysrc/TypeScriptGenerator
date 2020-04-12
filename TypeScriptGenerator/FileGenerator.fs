namespace TypeScriptGenerator
open System
open System.IO
open System.Collections.Generic

module internal FileGenerator =

    let private cache = Dictionary<Type, TSFile>()

    let private getCodeSnippets (x: Func<Type, string>) (t:Type) =
        if isNull x then List.empty
        else
           let snippets = x.Invoke t
           if isNull snippets then List.empty
           else
               snippets.Split([|'\r';'\n'|],StringSplitOptions.RemoveEmptyEntries)
               |> Array.map (fun line -> TS.indent + line.Trim())
               |> List.ofArray


    let private generateFile' (opts:Options) (t:Type) =         
        let o :TypeOption = {
            Type = t
            Path = FilePathGenerator.generatePath t
            CodeSnippets = getCodeSnippets opts.CodeSnippets t
            PropertyConverter = if isNull opts.PropertyConverter then None else Some (FuncConvert.FromFunc opts.PropertyConverter)
        }
        let gFunc =
            match o.Type with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent
        
        let (content, imports) = gFunc o

        Type.generatedTypes.Add t |> ignore

        {
            FullPath = Path.Combine(opts.Destination, o.Path + ".ts")
            Content = content
            ImportedTypes = imports
        }

    let generateFile (opts:Options) (t:Type) =       
        match cache.TryGetValue t with
        | true , v -> v
        | _ -> 
            let v = generateFile' opts t
            cache.[t]<-v
            v