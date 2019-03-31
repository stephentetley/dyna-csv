﻿// Copyright (c) Stephen Tetley 2019
// License: BSD 3 Clause

namespace DynaCsv

[<AutoOpen>]
module DynaCsv = 
    
    open System.IO
    open FSharp.Data

    open DynaCsv.Common
    open System
    open FSharp.Data.Runtime

    // Principles:
    // We want to reuse FSharp.Data for reading and writing Csv.
    // We want more "dynamism" than typed FSharp.Data offers.



    type DynaCsv<'row> = 
        // TODO - too promiscuous, we only need (a,_) for headers and
        // (_,b) for rows.
        | DynaCsv of FSharp.Data.CsvFile * Runtime.CsvFile<CsvRow>
        
        member internal x.Parent
            with get() : FSharp.Data.CsvFile = match x with |DynaCsv(csv,_) -> csv

        member internal x.Csv
            with get() : Runtime.CsvFile<CsvRow> = match x with |DynaCsv(_,csv) -> csv

        member x.SaveToString (?separator:char, ?quote:char) : string = 
            x.Csv.SaveToString(separator = defaultArg separator ',', 
                               quote = defaultArg quote '"')

        member x.Append1 (values:string[]) : DynaCsv<'row> = 
            let row = new CsvRow(parent= x.Parent, columns=values)
            let csv1 : Runtime.CsvFile<CsvRow> = x.Csv.Append [row]
            DynaCsv(x.Parent, csv1)
        
        member x.Append (values:seq<string[]>) : DynaCsv<'row> = 
            Seq.fold (fun (o:DynaCsv<'row>) (row:string[]) -> o.Append1 row)
                x
                values


    let fromHeaders (headers:string[]) = 
        let spec = headers |> Array.map escapeDoubleQuote |> String.concat "," 
        let csv : CsvFile = 
            CsvFile.Parse(text = spec,
                             separators=",", 
                             quote = '"', 
                             hasHeaders=true)
        DynaCsv(csv, csv.Cache())

    [<Struct>]
    type MapFunc<'row> = 
        | MapFunc of (CsvRow -> CsvRow)
        
        member x.Func
            with get() : (CsvRow -> CsvRow) = match x with | MapFunc(fn) -> fn

    let map (mapper:MapFunc<'row>) (dcsv:DynaCsv<'row>) : DynaCsv<'row> = 
        let func = new Func<CsvRow, CsvRow>(mapper.Func)
        let csv1 : CsvFile = dcsv.Csv.Map func :?> CsvFile
        DynaCsv(dcsv.Parent, csv1)


    type DynaRow = string []

    type Dyna2 = 
        val private CsvHeaders : option<string []>
        val private CsvRows : array<string []>

        new (headers:option<string []>, rows: array<string []>) = 
            { CsvHeaders = headers; CsvRows = rows }

        new (headers:string []) = 
            { CsvHeaders = Some headers; CsvRows = Array.empty} 

        new (headers:string list) = 
            { CsvHeaders = Some (List.toArray headers); CsvRows = Array.empty }

        new (rows: array<string []>) = 
            { CsvHeaders = None; CsvRows = rows }
        
        new (headers:string [], rows: array<string []>) = 
            { CsvHeaders = Some headers; CsvRows = rows }
            
        new (headers:string list, rows: array<string []>) = 
            { CsvHeaders = Some (List.toArray headers); CsvRows = rows }

        member x.Headers with get () : option<string[]> = x.CsvHeaders
        member x.Rows with get () : array<string[]> = x.CsvRows

    let xlaterow (row:CsvRow) : DynaRow = row.Columns



    let load (path:string) : Dyna2 = 
        printfn "load 1"
        use csv = CsvFile.Load(uri = path,
                                separators = ",",
                                hasHeaders = true, 
                                quote = '"' )
        let (arr:DynaRow array) = Seq.map (fun (row:CsvRow) -> row.Columns) csv.Rows |> Seq.toArray
        printfn "loaded %i rows" arr.Length
        new Dyna2( headers = csv.Headers, rows = arr )

    let makeHString (columns:string[]) : string = 
        String.concat "," columns

    let makeCsvRow (parent:CsvFile) (row:DynaRow) : CsvRow = 
        new CsvRow(parent=parent, columns = row)
        

    let save (dcsv:Dyna2) (outputFile:string) : unit = 
        let csv:CsvFile = 
            match dcsv.Headers with
            | Some arr -> CsvFile.Parse (text = makeHString arr, hasHeaders = true)
            | None -> CsvFile.Parse(text = "", hasHeaders = false)
        printfn "save 1"
        let rows = dcsv.Rows |> Seq.map (makeCsvRow csv)
        printfn "rows: %O" rows
        let csv1 = dcsv.Rows |> Seq.map (makeCsvRow csv) |> csv.Append
        printfn "%O" csv
        csv1.Save (path=outputFile)