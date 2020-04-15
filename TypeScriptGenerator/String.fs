[<AutoOpen>]
module internal String

open System
open System.Text

type CaseState =
| Start = 0
| Lower = 1
| Upper = 2
| NewWord = 3

let inline private append (sb:StringBuilder) (value:'a) =
    ignore <| sb.Append value

let toKebabCase (s:string) =
    let sep = '-'
    let sb = StringBuilder();
    let mutable state = CaseState.Start;
    for i in 0..s.Length-1 do
        if s.[i] = ' ' then
            if not (state = CaseState.Start) then            
                state <- CaseState.NewWord
        else if Char.IsUpper s.[i] then
            match state with
            | CaseState.Upper ->
                let hasNext = i + 1 < s.Length
                if i > 0 && hasNext then                
                    let nextChar = s.[i + 1]
                    if not (Char.IsUpper(nextChar)) && not (nextChar = sep) then                    
                        append sb sep
            | CaseState.Lower
            | CaseState.NewWord ->
                append sb sep
            | _ -> ()
            
            let c = Char.ToLowerInvariant(s.[i])
            append sb c
            state <- CaseState.Upper
        else if s.[i] = sep then  
            append sb sep
            state <- CaseState.Start
        else
            if state = CaseState.NewWord then            
                append sb sep 
            append sb s.[i]
            state <- CaseState.Lower
    sb.ToString()

let toCamelCase (s:string) =
    if String.IsNullOrEmpty(s) || not <| Char.IsUpper(s.[0]) then
        s
    else
        let chars = s.ToCharArray()
        let breaks = 
            chars        
            |> Array.mapi (fun i c ->
                if i = 1 && not <| Char.IsUpper s.[i] then 
                    true, c
                else
                    let hasNext = (i + 1 < s.Length)
                    if i > 0 && hasNext && not <| Char.IsUpper s.[i + 1] then   
                        true,
                            if Char.IsSeparator(s.[i + 1]) then 
                                Char.ToLowerInvariant c
                            else 
                                c
                    
                    else
                        false, Char.ToLowerInvariant c
            )
        let index = Array.tryFindIndex (fun (b,c) -> b) breaks
        match index with
        | Some i ->
            Array.append 
                (Array.take i breaks |> Array.map (fun (_,c)->c))
                (Array.skip i chars)
        | None -> Array.map (fun (_,c)->c) breaks
        |> String

let format fm (args:obj seq) =
    String.Format(fm, args |> Seq.toArray);