open System
open FSharp.Data
open System.Text
open System.IO
open System.Text.Json
open System.Collections.Generic

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)
let enc = Encoding.GetEncoding("windows-1251")
let filePath = @"D:\Downloads\receipt.csv";
let cardAcct = "Liabilities:Open:OpenCard"
let mappingDict = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(@"..\..\..\accountMap.json"));

let processedCsv = 
    let expectedDelims = 10
    let delim = ';'
    File.ReadAllLines(filePath, enc)
    |> Array.map (fun l -> 
        let lineDelimCount = l |> Seq.filter ((=) delim) |> Seq.length
        if lineDelimCount >= expectedDelims then l
        else sprintf "%s%s" l (Seq.init (expectedDelims - lineDelimCount) (fun _ -> delim) |> Seq.toArray |> String))
    |> String.concat Environment.NewLine

let mappingOrCategory cat = 
    match mappingDict.TryGetValue cat with
    | (true, v) -> v
    | (false, _) -> cat

let cf = CsvFile.Parse(processedCsv, separators = ";").Cache()
for row in cf.Rows |> Seq.filter (fun x -> x.["Статус"] = "Проведена") |> Seq.rev do
    let dateStr = DateTime.Parse(row.["Дата совершения операции"]).Date.ToString("yyyy-MM-dd")
    let credit = row.["Сумма расхода"]
    let amountWithCurrency = if (String.IsNullOrWhiteSpace(credit)) then sprintf "%s %s" row.["Сумма пополнения"] row.["Валюта пополнения"] else sprintf "%s %s" credit row.["Валюта расхода"]
    printfn "%s * \"%s\"" dateStr (row.["Описание операции"].Replace('\\', '/'))
    printfn "    %s\t%s" cardAcct amountWithCurrency
    printfn "    %s" (mappingOrCategory row.["Категория операции"])
    printfn ""

0 |> ignore