[<AutoOpen>]
module Type

open System

let getName (t:Type)=
    let name = t.Name
    let num = name.IndexOf('`');
    if num > -1 then name.Substring(0, num) else name


