namespace TypeScriptGenerator
open System
open System.IO

module internal FileGenerator =

    let private getCodeSnippets (x: Func<Type, string>) (t:Type) =
        if isNull x then List.empty
        else
           let snippets = x.Invoke t
           if isNull snippets then List.empty
           else
               snippets.Split([|'\r';'\n'|],StringSplitOptions.RemoveEmptyEntries)
               |> Array.map (fun line -> TS.indent + line.Trim())
               |> List.ofArray

    let private generateFile' (opts:ModelGenerateOptions) (t':Type) =         
        let t = t' |> Configuration.converteType |> Option.defaultValue t'
        let o :TypeOptions = {
            Type = t
            Path = FilePathGenerator.generatePath t
            CodeSnippets = getCodeSnippets opts.CodeSnippets t
        }
        let gFunc =
            match o.Type with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent
        
        let (content, imports) = gFunc o

        Cache.generatedTypes.Add t |> ignore

        {
            Type = t
            FullPath = Path.Combine(opts.Destination, o.Path + ".ts")
            Content = content
            ImportedTypes = imports
        }

    let generateFile (opts:ModelGenerateOptions) =       
        Cache.memoize (generateFile' opts)
