namespace TypeScriptGenerator
open System
open System.IO
open System.Collections.Generic

module internal FileGenerator =

    let private cache = Dictionary<Type, TSFile>()

    let private generateFile' (opts:Options) (t:Type) =         
        let o :TypeOption = {
            Type = t
            Path = FilePathGenerator.generatePath t
            CodeSnippets = if isNull opts.CodeSnippets then None else Some (opts.CodeSnippets.Invoke t) //todo ignore null & empty
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