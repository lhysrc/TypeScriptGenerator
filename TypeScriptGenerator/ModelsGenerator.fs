namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection

open FileGenerator

module ModelsGenerator =
    let private writeFile path content = 
        let file = FileInfo(path)
        if not file.Directory.Exists then file.Directory.Create();
        File.WriteAllText(path,content)

        
    let create (assemblies : Assembly seq, 
                destinationPath:string, 
                optionAction:Action<Options>) =
        let opts = Options()
        optionAction.Invoke opts

        assemblies 
        |> Seq.collect (fun a -> a.ExportedTypes)
        |> Seq.filter (if opts.TypeMatcher |> isNull then fun _ -> true else FuncConvert.FromFunc opts.TypeMatcher)
        |> Seq.map (generateFile destinationPath)
        |> Seq.iter (fun f -> (writeFile f.FullPath f.Content))
