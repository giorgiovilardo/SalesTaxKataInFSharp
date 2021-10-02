module SalesTaxKataInFSharp.DomainTypes

type BaseTaxStatus =
    | Exempt
    | FullyTaxed

type ImportStatus =
    | Imported
    | Local

type TaxStatus = BaseTaxStatus * ImportStatus

type Price = Price of decimal

module Price =
    let Create amount =
        match amount with
        | amount when amount >= 0M -> Price amount
        | _ -> failwith "Price can't be negative!"

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
