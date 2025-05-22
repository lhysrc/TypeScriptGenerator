namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection
open System.Xml.Linq // Added for XML parsing
open Configuration    // Added for xmlDocs and addXmlDoc

open FileGenerator

module ModelsGenerator =
    let private writeFile path content = 
        // printfn "%s" path
        let file = FileInfo(path)
        if not file.Directory.Exists then file.Directory.Create();
        if File.Exists path && File.ReadAllText path = content then ()
        else File.WriteAllText(path, content)
    
    let rec private generateImportedTypeFiles (opts:ModelGenerateOptions) (ts:Type list) = 
        let files =
            ts 
            |> List.filter (fun t -> not (Cache.generatedTypes.Contains t))
            |> List.map (fun t -> generateFile opts t)
        
        files
        |> List.collect (fun t -> generateImportedTypeFiles opts t.ImportedTypes)        
        |> List.append files

    [<CompiledName("Generate")>]
    let generate (assemblies : Assembly seq, 
                  destinationPath: string, 
                  optionsAction: Action<ModelGenerateOptions>) =
        let opts = ModelGenerateOptions destinationPath
        optionsAction.Invoke opts
        Configuration.setOptions opts
        let sw = System.Diagnostics.Stopwatch()
        sw.Start()

        // Load XML documentation
        for assembly in assemblies do
            try
                if not assembly.IsDynamic then // Dynamic assemblies typically don't have a location or XML docs
                    let assemblyPath = assembly.Location
                    if not (String.IsNullOrWhiteSpace assemblyPath) then
                        let xmlPath = Path.ChangeExtension(assemblyPath, ".xml")
                        if File.Exists xmlPath then
                            let xmlDoc = XDocument.Load(xmlPath)
                            match xmlDoc.Root.Element("members") with
                            | null -> 
                                printfn "Warning: XML documentation file at '%s' does not contain a <members> root element." xmlPath
                            | membersElement ->
                                for memberEl in membersElement.Elements("member") do
                                    match memberEl.Attribute("name") with
                                    | null -> () // Member tag without a name attribute, skip.
                                    | nameAttr ->
                                        let memberContent = memberEl.Value.Trim() // .Value gets inner text of the <member> node
                                        if not (String.IsNullOrWhiteSpace memberContent) then
                                            Configuration.addXmlDoc nameAttr.Value memberContent
                        else
                            // It's common for XML docs to be missing, so this might be too verbose if not a warning.
                            // Depending on requirements, this could be a more silent failure or a configurable warning.
                            printfn "FYI: XML documentation file not found for assembly: '%s' (expected at '%s')" assembly.FullName xmlPath
                    else
                        printfn "Warning: Assembly location is null or empty for '%s'. Skipping XML documentation loading." assembly.FullName
            with
            | ex -> 
                printfn "Error loading XML documentation for assembly '%s': %s" assembly.FullName ex.Message

        let loadedTypes =
            assemblies 
            |> Seq.collect (fun a -> a.ExportedTypes)

        let exports =
            loadedTypes
            |> Seq.filter (fun t -> not t.IsNested)
            |> Seq.filter (if isNull opts.TypeFilter then fun _ -> true else FuncConvert.FromFunc opts.TypeFilter)    
            |> Seq.map (generateFile opts)
            |> Seq.filter (fun f -> not <| String.IsNullOrWhiteSpace f.Content)
            |> Seq.cache

        let imports =
            exports
            |> Seq.collect (fun x -> generateImportedTypeFiles opts x.ImportedTypes)
            |> Seq.filter (fun f -> not <| String.IsNullOrWhiteSpace f.Content)

        exports 
        |> Seq.append imports
        |> Seq.distinctBy (fun t->t.FullPath)
        |> Seq.iter (fun f -> (writeFile f.FullPath f.Content))
        
        exports 
        |> Seq.groupBy (fun m -> Path.GetDirectoryName m.FullPath)
        |> Seq.iter (fun (dir,files) -> 
            files 
            |> Seq.map (fun f -> 
                if Type.isStatic f.Type then 
                    String.format "import * as {0} from \"./{1}\";{2}export {{ {0} }};"
                        [TS.getNameWithoutGeneric(f.Type);(Path.GetFileNameWithoutExtension f.FullPath);Environment.NewLine]
                else 
                    sprintf "export * from \"./%s\";" (Path.GetFileNameWithoutExtension f.FullPath) )
            |> String.concat Environment.NewLine
            |> writeFile (Path.Combine(dir, "index.ts"))                )

        sw.Stop()
        printf "生成文件耗时 %.2f 毫秒"  sw.Elapsed.TotalMilliseconds
