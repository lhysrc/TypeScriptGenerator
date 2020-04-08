namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection

open FileGenerator

module ModelsGenerator =
    let private writeFile path content = 
        let file = FileInfo(path)
        if not file.Directory.Exists then file.Directory.Create();
        File.WriteAllText(path, content)
    

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
        Type.loadedTypes.UnionWith loadedTypes

        let matcheds =
            loadedTypes
            |> Seq.filter (fun t -> not t.IsNested)
            |> Seq.filter (if isNull opts.TypeMatcher then fun _ -> true else FuncConvert.FromFunc opts.TypeMatcher)    
            |> Seq.map (generateFile destinationPath loadedTypes)

        let misseds =
            matcheds
            |> Seq.collect (fun m->m.UsedTypes)
            |> Seq.map (generateFile destinationPath loadedTypes)

        matcheds 
            |> Seq.append misseds
            |> Seq.filter (fun f -> String.IsNullOrWhiteSpace f.Content |> not)
            |> Seq.iter (fun f -> (writeFile f.FullPath f.Content))
        
        sw.Stop()
        printf "生成文件耗时%f毫秒"  sw.Elapsed.TotalMilliseconds