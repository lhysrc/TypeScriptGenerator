namespace TypeScriptGenerator

open System
open System.Text
open System.Xml.Linq
open Configuration // To access Configuration.xmlDocs

module DocGenerator =

    let generateJsDoc (xmlDocKey: string) : string =
        if not Configuration.currentEnableJsDoc then // Check the global flag
            "" // If JSDoc is not enabled, return empty string immediately
        else
            match Configuration.xmlDocs.TryGetValue(xmlDocKey) with
            | true, xmlContent when not (String.IsNullOrWhiteSpace xmlContent) ->
                try
                // Wrap the content to make it a valid XML document for parsing
                // The content from xmlDocs is the inner XML of the <member> tag
                let wrappedXml = sprintf "<docroot>%s</docroot>" xmlContent
                let rootElement = XElement.Parse(wrappedXml)
                
                let sb = StringBuilder()
                sb.AppendLine("/**")

                // Extract summary
                match rootElement.Element("summary") with
                | null -> ()
                | summaryEl -> 
                    let summaryText = summaryEl.Value.Trim()
                    if not (String.IsNullOrWhiteSpace summaryText) then
                        summaryText.Split([|Environment.NewLine|], StringSplitOptions.None)
                        |> Array.iter (fun line -> sb.AppendLine(sprintf " * %s" line.Trim()))

                // Extract typeparam
                rootElement.Elements("typeparam")
                |> Seq.iter (fun typeParamEl ->
                    match typeParamEl.Attribute("name") with
                    | null -> ()
                    | nameAttr ->
                        let desc = typeParamEl.Value.Trim()
                        sb.AppendLine(sprintf " * @template %s %s" nameAttr.Value desc |> _.TrimEnd())
                )

                // Extract params
                rootElement.Elements("param")
                |> Seq.iter (fun paramEl ->
                    match paramEl.Attribute("name") with
                    | null -> ()
                    | nameAttr ->
                        let desc = paramEl.Value.Trim()
                        sb.AppendLine(sprintf " * @param %s %s" nameAttr.Value desc |> _.TrimEnd())
                )
                
                // Extract returns
                match rootElement.Element("returns") with
                | null -> ()
                | returnsEl -> 
                    let returnsText = returnsEl.Value.Trim()
                    if not (String.IsNullOrWhiteSpace returnsText) then
                        sb.AppendLine(sprintf " * @returns %s" returnsText)

                // Extract remarks
                match rootElement.Element("remarks") with
                | null -> ()
                | remarksEl ->
                    let remarksText = remarksEl.Value.Trim()
                    if not (String.IsNullOrWhiteSpace remarksText) then
                        sb.AppendLine(" *") // Add a blank line before remarks if there was a summary
                        remarksText.Split([|Environment.NewLine|], StringSplitOptions.None)
                        |> Array.iter (fun line -> sb.AppendLine(sprintf " * %s" line.Trim()))
                
                // Only return if we actually added something beyond the initial "/**"
                if sb.Length > 4 then // Length of "/**\n"
                    sb.AppendLine(" */")
                    sb.ToString()
                else
                    "" // No meaningful content extracted
            with
            | ex -> 
                printfn "Error parsing XML documentation for key '%s': %s" xmlDocKey ex.Message
                "" // Return empty string on error
        | _ -> "" // No documentation found or content is whitespace
