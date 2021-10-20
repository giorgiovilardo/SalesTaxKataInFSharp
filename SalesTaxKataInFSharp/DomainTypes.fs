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
    let Create amount =
        match amount with
        | amount when amount >= 0M -> Price amount
        | _ -> failwith "Price can't be negative!"

    let Value (Price price) = price

    let (|Price|) (amount: decimal) = amount

type Item = { Name: string; UnitPrice: Price }

type TotalPrice = TotalPrice of decimal

module TotalPrice =
    let Value (TotalPrice totalPrice) = totalPrice

type TaxPrice = TaxPrice of decimal

module TaxPrice =
    let CreateFromTotalPrice percentageTaxed (TotalPrice totalPrice) =
        totalPrice
        |> fun totalPrice -> (totalPrice * decimal percentageTaxed) / 100M
        |> fun preRoundPrice -> ceil (preRoundPrice * 20M) / 20M
        |> TaxPrice

    let Value (TaxPrice taxPrice) = taxPrice

    let (|TaxPrice|) amount = amount

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
        $"{order.Quantity} {order.Item.Name}: %.2f{TaxPrice.Value order.SalesTax
                                                   + TotalPrice.Value order.TotalPrice}"

    let CreateFromParsedOrderRow (parsedOrderRow: ParsedOrderRow) =
        let taxPercentage =
            TaxStatus.GetPercentage parsedOrderRow.TaxStatus

        let totalPrice =
            parsedOrderRow.Item.UnitPrice
            |> Price.Value
            |> (*) (decimal parsedOrderRow.Quantity)
            |> TotalPrice

        let salesTax =
            TaxPrice.CreateFromTotalPrice taxPercentage totalPrice

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
    member this.SumSales =
        this.Rows
        |> List.map (fun orderRow -> TaxPrice.Value orderRow.SalesTax)
        |> List.sum

    member this.SumPrices =
        this.Rows
        |> List.map (fun orderRow -> TotalPrice.Value orderRow.TotalPrice)
        |> List.sum

    member this.ToReceipt =
        this.Rows
        |> List.map CompleteOrderRow.ToString
        |> List.map (fun x -> x + "\n")
        |> List.reduce (+)
        |> (+) $"Sales Taxes: %.2f{this.SumSales}\n"
        |> (+) $"Total: %.2f{this.SumPrices}\n"

// the "Test suite"
//let item1 = { Name="TestItem 1"; UnitPrice = Price 19.99M}
//let item2 = { Name="TestItem 2"; UnitPrice = Price 29.99M}
//let por1 = { Quantity = 1; Item = item; TaxStatus = FullyTaxed, Local}
//let por2 = { Quantity = 3; Item = item2; TaxStatus = FullyTaxed, Local }
//let com1 = CompleteOrderRow.CreateFromParsedOrderRow por1
//let com2 = CompleteOrderRow.CreateFromParsedOrderRow por2
//let k = { Rows = [ com1; com2 ]};;
