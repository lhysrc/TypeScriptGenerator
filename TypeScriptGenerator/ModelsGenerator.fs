namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection

open FileGenerator

module ModelsGenerator =
    let private writeFile path content = 
        // printfn "%s" path
        let file = FileInfo(path)
        if not file.Directory.Exists then file.Directory.Create();
        File.WriteAllText(path, content)
    
    let rec generateUsedTypeFiles (destinationPath:string) (ts:Type list) = 
        let files =
            ts 
            |> List.filter (fun t -> not (Type.generatedTypes.Contains t))
            |> List.map (fun t -> generateFile destinationPath t)
        
        files
        |> List.collect (fun t -> generateUsedTypeFiles destinationPath t.UsedTypes)        
        |> List.append files

    let create (assemblies : Assembly seq, 
                destinationPath:string, 
                optionAction:Action<Options>) =
        let opts = Options()
        optionAction.Invoke opts

        let sw = System.Diagnostics.Stopwatch()
        sw.Start()

        let loadedTypes =
            assemblies 
            |> Seq.collect (fun a -> a.ExportedTypes)

        let matcheds =
            loadedTypes
            |> Seq.filter (fun t -> not t.IsNested)
            |> Seq.filter (if isNull opts.TypeMatcher then fun _ -> true else FuncConvert.FromFunc opts.TypeMatcher)    
            |> Seq.map (generateFile destinationPath)

        let misseds =
            matcheds
            |> Seq.collect (fun x -> generateUsedTypeFiles destinationPath x.UsedTypes)

        matcheds 
            |> Seq.append misseds
            |> Seq.filter (fun f -> String.IsNullOrWhiteSpace f.Content |> not)
            |> Seq.distinctBy (fun t->t.FullPath)
            |> Seq.iter (fun f -> (writeFile f.FullPath f.Content))
        
        sw.Stop()
        printf "生成文件耗时%f毫秒"  sw.Elapsed.TotalMilliseconds