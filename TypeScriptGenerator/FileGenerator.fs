namespace TypeScriptGenerator
open System
open System.IO
open System.Collections.Generic

module internal FileGenerator =

    //let rec private getUsedTypes' (loadeds:Type seq) (t:Type) =
    //    let useds = t |> Type.getUsedTypes loadeds
    //    useds 
    //    |> Seq.filter (not << TS.isBuildIn) //todo µÝ¹é»ñÈ¡
    //    //|> Seq.collect (getUsedTypes' loadeds)
    //    //|> Seq.append useds
    //    //|> Seq.distinct

    let private cache = Dictionary<Type, TSFile>()

    let private generateFile' (root:string) (t:Type) =         
        let o :TypeOption = {
            Type = t
            //UsedTypes = getUsedTypes' allTypes t
            Path = FilePathGenerator.generatePath t
        }
        let gFunc =
            match o.Type with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent
        
        let (content, useds) = gFunc o

        {
            FullPath = Path.Combine(root, o.Path + ".ts")
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