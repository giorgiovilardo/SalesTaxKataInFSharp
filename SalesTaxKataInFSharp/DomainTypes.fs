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

type Item =
    { Name: string
      Price: Price
      TaxStatus: TaxStatus }
