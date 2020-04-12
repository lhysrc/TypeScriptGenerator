﻿namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection

open FileGenerator

module ModelsGenerator =
    let private writeFile path content = 
        // printfn "%s" path
        let file = FileInfo(path)
        if not file.Directory.Exists then file.Directory.Create();
        if File.Exists path && File.ReadAllText path = content then ()
        else File.WriteAllText(path, content)
    
    let rec private generateUsedTypeFiles (opts:ModelGenerateOptions) (ts:Type list) = 
        let files =
            ts 
            |> List.filter (fun t -> not (Type.generatedTypes.Contains t))
            |> List.map (fun t -> generateFile opts t)
        
        files
        |> List.collect (fun t -> generateUsedTypeFiles opts t.ImportedTypes)        
        |> List.append files

    [<CompiledName("Generate")>]
    let generate (assemblies : Assembly seq, 
                  destinationPath: string, 
                  optionsAction: Action<ModelGenerateOptions>) =
        let opts = ModelGenerateOptions destinationPath
        optionsAction.Invoke opts

        let sw = System.Diagnostics.Stopwatch()
        sw.Start()

        let loadedTypes =
            assemblies 
            |> Seq.collect (fun a -> a.ExportedTypes)

        let matcheds =
            loadedTypes
            |> Seq.filter (fun t -> not t.IsNested)
            |> Seq.filter (if isNull opts.TypeFilter then fun _ -> true else FuncConvert.FromFunc opts.TypeFilter)    
            |> Seq.map (generateFile opts)

        let misseds =
            matcheds
            |> Seq.collect (fun x -> generateUsedTypeFiles opts x.ImportedTypes)

        matcheds 
            |> Seq.append misseds
            |> Seq.filter (fun f -> String.IsNullOrWhiteSpace f.Content |> not)
            |> Seq.distinctBy (fun t->t.FullPath)
            |> Seq.iter (fun f -> (writeFile f.FullPath f.Content))
        
        sw.Stop()
        printf "生成文件耗时 %.2f 毫秒"  sw.Elapsed.TotalMilliseconds