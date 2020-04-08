namespace TypeScriptGenerator
open System
open System.IO

module internal FileGenerator =

    //let rec private getUsedTypes' (loadeds:Type seq) (t:Type) =
    //    let useds = t |> Type.getUsedTypes loadeds
    //    useds 
    //    |> Seq.filter (not << TS.isBuildIn) //todo µÝ¹é»ñÈ¡
    //    //|> Seq.collect (getUsedTypes' loadeds)
    //    //|> Seq.append useds
    //    //|> Seq.distinct

    let generateFile (root:string) (allTypes:Type seq) (t:Type) = 
        let o :TypeOption = {
            Type = t
            //UsedTypes = getUsedTypes' allTypes t
        }
        let gFunc =
            match o.Type with
            | t when t.IsEnum -> EnumContentGenerator.generateContent
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent
        
        let (content, useds) = gFunc o

        {|
            // Name = o.Name
            Content = content
            Type  = o.Type
            FullPath = Path.Combine(root, FilePathGenerator.generatePath(o.Type) + ".ts")
            UsedTypes = useds |> Seq.filter Type.loadedTypes.Contains |> Seq.filter (not << TS.isBuildIn)
        |}