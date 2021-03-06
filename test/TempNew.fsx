﻿// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause


#r "netstandard"
#r "System.Xml.Linq.dll"


open System.IO
// open Microsoft.FSharp.Collections

// Use FSharp.Data for CSV reading
#I @"C:\Users\stephen\.nuget\packages\FSharp.Data\3.3.3\lib\netstandard2.0"
#r @"FSharp.Data.dll"
open FSharp.Data

#load "..\src\DynaCsv\Old\Common.fs"
#load "..\src\DynaCsv\Old\DynaCsv.fs"
open DynaCsv.Old

// New attempt for 2019...

let blankCsv () = 
    use sr = new StringReader("")
    CsvFile.Load(sr)

let blankCsv1 (headers:string) = 
    CsvFile.Parse(text = headers)           // Parse not Load!

let demo01 () = 
    let csv:FSharp.Data.CsvFile = blankCsv1 ("City,Country")
    match csv.Headers with
    | None -> printfn "#N/A"
    | Some arr -> printfn "%O" (arr |> Array.toList)
    printfn "Rows: %i" (csv.Rows |> Seq.length)
    let row1 = new CsvRow(parent= csv, columns=[| "Leeds"; "UK" |])
    // Append is "functional"
    let csv1 : Runtime.CsvFile<CsvRow> = csv.Append <| [row1]

    csv1.SaveToString () |> printfn "%s"
    let row2 = new CsvRow(parent= csv, columns=[| "Leeds" |])
    // Appending a too short row is not an error...
    let csv2 = csv1.Append <| List.toSeq [row2]
    csv2.SaveToString () |> printfn "%s"

// Look at Map API

let map01 () : Map<string,int> = 
    let m1 = new Map<string,int> ([])
    Map.add "first" 1 m1


let map02 () : Map<string,int> = 
    Map.ofList [("first", 1)]

let demo02 () = 
    let csv = DynaCsv.fromHeaders [| "City"; "Country" |]
    let csv1 = let row1 = [| "Leeds"; "UK" |] in csv.Append [row1]
    csv1.SaveToString() |> printfn "%s"

    csv1.Parent.Headers |> Option.defaultWith (fun _ -> Array.empty) |> Array.toList |> printfn "%O"
    csv1.Csv.Rows |> Seq.iter (fun row -> printfn "%O" (row.Columns |> Array.toList))


let demo03 () = 
    let csv = new Dyna2(headers = ["City"; "Country" ])
    csv.ToString() 

let inputPath = Path.Combine( __SOURCE_DIRECTORY__ , "..", @"data\hospitals.csv")
let outputPath = Path.Combine( __SOURCE_DIRECTORY__ , "..", @"data\hospitals.1.csv")

let demo04 () = 
    printfn "Loading..."
    let dcsv = load inputPath
    printfn "Loaded."
    match dcsv.Headers with
    | Some arr -> printfn "Headers: %O" (Array.toList arr)
    | None -> printfn "No headers"
    printfn "Row Count: %i" (dcsv.Rows |> Seq.toArray).Length
    // dcsv.Rows |> Seq.iter (printfn "%O") 
    printfn "Saving..."
    save dcsv outputPath



let demo05 () = 
    let outputPath = Path.Combine( __SOURCE_DIRECTORY__ , "..", @"data\blank.csv")
    let dcsv = new Dyna2 (rows = [||])
    save dcsv outputPath


let demo05a () = 
    let outputPath = Path.Combine( __SOURCE_DIRECTORY__ , "..", @"data\oneline.csv")
    let row1 = [| "one"; "two"; "three" |]
    let dcsv = new Dyna2 (rows = [| row1 |])
    save dcsv outputPath
