[<AutoOpen>]
module String

open System
open System.Text

type CaseState =
| Start
| Lower
| Upper
| NewWord

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
    s