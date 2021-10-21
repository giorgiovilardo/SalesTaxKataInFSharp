module SalesTaxKataInFSharp.DomainTypes

type BaseTaxStatus =
    | Exempt
    | FullyTaxed

type ImportStatus =
    | Imported
    | Local

type TaxStatus = BaseTaxStatus * ImportStatus

module TaxStatus =
    let private GetBaseTaxPercentage status =
        match status with
        | Exempt -> 0
        | FullyTaxed -> 10

    let private GetImportTaxPercentage status =
        match status with
        | Local -> 0
        | Imported -> 5

    let GetPercentage ((baseStatus, importStatus): TaxStatus) =
        GetBaseTaxPercentage baseStatus
        + GetImportTaxPercentage importStatus

type Price = Price of decimal

module Price =
    let create amount =
        match amount with
        | amount when amount >= 0M -> Some(Price amount)
        | _ -> None

    let get (Price price) = price

type Item =
    { Name: string
      UnitPrice: Price option }

type TotalPrice = TotalPrice of decimal

module TotalPrice =
    let get (TotalPrice totalPrice) = totalPrice

type TaxPrice = TaxPrice of decimal

module TaxPrice =
    let ofTotalPrice percentageTaxed (TotalPrice totalPrice) =
        totalPrice
        |> fun totalPrice -> (totalPrice * decimal percentageTaxed) / 100M
        |> fun preRoundPrice -> ceil (preRoundPrice * 20M) / 20M
        |> TaxPrice

    let get (TaxPrice taxPrice) = taxPrice

type ParsedOrderRow =
    { Quantity: int
      Item: Item
      TaxStatus: TaxStatus }

type CompleteOrderRow =
    { Quantity: int
      Item: Item
      TaxStatus: TaxStatus
      TotalPrice: TotalPrice
      SalesTax: TaxPrice }

module CompleteOrderRow =
    let ToString order =
        $"{order.Quantity} {order.Item.Name}: %.2f{TaxPrice.get order.SalesTax
                                                   + TotalPrice.get order.TotalPrice}"

    let ofParsedOrderRow (parsedOrderRow: ParsedOrderRow) =
        let taxPercentage =
            TaxStatus.GetPercentage parsedOrderRow.TaxStatus

        let totalPrice =
            parsedOrderRow.Item.UnitPrice
            |> Option.defaultValue (Price 0M)
            |> Price.get
            |> (*) (decimal parsedOrderRow.Quantity)
            |> TotalPrice

        let salesTax =
            TaxPrice.ofTotalPrice taxPercentage totalPrice

        { Quantity = parsedOrderRow.Quantity
          Item = parsedOrderRow.Item
          TaxStatus = parsedOrderRow.TaxStatus
          TotalPrice = totalPrice
          SalesTax = salesTax }

type Order =
    { Rows: CompleteOrderRow list }
    // This behaviour should be extracted with some high order function?
    // I tried and the signature is ('a -> decimal) -> 'a -> decimal
    // Feels like I'm missing something here
    member this.sumSales =
        this.Rows
        |> List.map (fun orderRow -> TaxPrice.get orderRow.SalesTax)
        |> List.reduce (+)

    member this.sumPrices =
        this.Rows
        |> List.map (fun orderRow -> TotalPrice.get orderRow.TotalPrice)
        |> List.sum

    //    member this.sum =
//        let mapAndSum f x = List.map f >> List.sum
//        this.Rows |> mapAndSum f

    member this.ToReceipt =
        let mapReduce =
            List.map CompleteOrderRow.ToString
            >> List.map ((+) "\n")
            >> (List.reduce (+))

        this.Rows
        |> List.map CompleteOrderRow.ToString
        |> List.map ((+) "\n")
        |> List.reduce (+)
        |> (+) $"Sales Taxes: %.2f{this.sumSales}\n"
        |> (+) $"Total: %.2f{this.sumPrices}\n"

// the "Test suite"
//let item1 = { Name="TestItem 1"; UnitPrice = Price 19.99M}
//let item2 = { Name="TestItem 2"; UnitPrice = Price 29.99M}
//let por1 = { Quantity = 1; Item = item1; TaxStatus = FullyTaxed, Local}
//let por2 = { Quantity = 3; Item = item2; TaxStatus = FullyTaxed, Local }
//let com1 = CompleteOrderRow.CreateFromParsedOrderRow por1
//let com2 = CompleteOrderRow.CreateFromParsedOrderRow por2
//let k = { Rows = [ com1; com2 ]};;

// the "Option test suite"
//let item1 =
//    { Name = "TestItem 1"
//      UnitPrice = Price.create 19.99M }
//
//let item2 =
//    { Name = "TestItem 2"
//      UnitPrice = Price.create 29.99M }
//
//let item3 =
//    { Name = "TestItem 3"
//      UnitPrice = Price.create -15M }
//
//let por1 =
//    { Quantity = 1
//      Item = item1
//      TaxStatus = FullyTaxed, Local }
//
//let por2 =
//    { Quantity = 3
//      Item = item2
//      TaxStatus = FullyTaxed, Local }
//
//let por3 =
//    { Quantity = 4
//      Item = item3
//      TaxStatus = FullyTaxed, Local }
//
//let com1 =
//    CompleteOrderRow.ofParsedOrderRow por1
//
//let com2 =
//    CompleteOrderRow.ofParsedOrderRow por2
//
//let com3 =
//    CompleteOrderRow.ofParsedOrderRow por3
//
//let k = { Rows = [ com1; com2 ] }
