module SalesTaxKataInFSharp.DomainTypes

type BaseTaxStatus =
    | Exempt
    | FullyTaxed

type ImportStatus =
    | Imported
    | Local

type TaxStatus = BaseTaxStatus * ImportStatus

module TaxStatus =
    let GetPercentage status =
        match status with
        | Exempt, Local -> 0
        | Exempt, Imported -> 5
        | FullyTaxed, Local -> 10
        | FullyTaxed, Imported -> 15

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

type TaxPrice = TaxPrice of decimal

module TaxPrice =
    let CreateFromTotalPrice percentageTaxed (TotalPrice totalPrice) =
        totalPrice
        |> fun totalPrice -> (totalPrice * decimal percentageTaxed) / 100M
        |> fun preRoundPrice -> ceil (preRoundPrice * 20M) / 20M
        |> TaxPrice

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

type Order = { Rows: CompleteOrderRow list }
