namespace TypeScriptGenerator
open System
open System.IO
open System.Reflection
open System.Text

module internal ConstContentGenerator =
    let generateContent (t: Type) =
        t.GetFields(BindingFlags.Public ||| BindingFlags.Static |||
                       BindingFlags.FlattenHierarchy)
        |> Seq.filter (fun fi -> fi.IsLiteral && not fi.IsInitOnly)
        |> Seq.map (fun fi -> "export " + fi.Name + " = " + string (fi.GetRawConstantValue()))
        |> String.concat Environment.NewLine



module internal ModelPropertyGenerator =
    let generatorProp (p:PropertyInfo) =
        ""

module internal ModelContentGenerator =
    let private generateExportType (t: Type) =
        let typeString = 
            match t with
            | t when t.IsInterface -> "interface"
            | t when (t.IsAbstract && not t.IsSealed) -> "abstract class"
            | t when t.IsEnum -> "enum"
            | _ ->  "class"
            
        String.concat " " [
            "export"
            typeString
        ]

    let generateContent (t: Type) =
        generateExportType t
        


module internal FileGenerator =
    let generateFile (root:string) (t:Type) = 
        let gFunc =
            match t with
            | t when (t.IsAbstract && t.IsSealed) -> ConstContentGenerator.generateContent
            | _ ->  ModelContentGenerator.generateContent

        {|
            Name = t.Name
            Content = gFunc t
            Type  = t
            FullPath = Path.Combine(root, t.Name)
        |}