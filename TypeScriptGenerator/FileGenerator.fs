namespace TypeScriptGenerator
open System
open System.IO
open System.Collections.Generic

module internal FileGenerator =

    let private cache = Dictionary<Type, TSFile>()

    let private generateFile' (rootDir:string) (t:Type) =         
        let o :TypeOption = {
            Type = t
            Path = FilePathGenerator.generatePath t
        }
        let gFunc =
            match o.Type with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent
        
        let (content, useds) = gFunc o

        Type.generatedTypes.Add t |> ignore

        {
            FullPath = Path.Combine(rootDir, o.Path + ".ts")
            Content = content
            UsedTypes = useds
        }

    let generateFile (root:string) (t:Type) =       
        match cache.TryGetValue t with
        | true , v -> v
        | _ -> 
            let v = generateFile' root t
            cache.[t]<-v
            v