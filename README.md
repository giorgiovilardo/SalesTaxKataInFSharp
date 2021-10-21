# Sales Tax Kata in F#

[Kata Text](KATA.md)

This is more of a journal rather than a readme, as it's a sequential dump of how I approach the problem posed by the kata. It's a living document given how little time I have, so I hope to come back to it.
It's divided in sessions, corresponding to the time I can actually sit and write 😂

## First session

## Domain modeling

Let's start from the basics: the ubiquitous language. By reading the kata text, we start to build over some important terms for our analysis. *Article, exempt, import, price, tax rate, quantity...* We can build up on those.

### The item

Obviously the first idea is to model the item. Pretty clearly a record type, but we cannot model it right now, but why?

### The status

Immediate insight: an Article/Item (we still haven't decided how to call it!) has two "status" fields: if it's exempt from the base tax, and if it's imported. We need to model these first. They are represented by discriminated union types in F#:

```f#
type BaseTaxStatus =
    | Exempt
    | FullyTaxed

type ImportStatus =
    | Imported
    | Local
```

I've been careful to not mention a `Is` verb on the type, or it could have been easily seen as a bool. Now we can have a `BaseTaxStatus` of `PartiallyTaxed` and just add it to the union type. Same idea for the `ImportStatus`. They are just `empty cases` (no `of type`) as they don't carry particular information other than being some sort of flags.

Another insight: the two statuses go together, as they both concur at creating the combination that will be used to determine the tax calculation. It could be an idea to declare a tuple type. It's hard to name tho. `Taxableness`? `TaxTupleForFinalCalculation`? I'll just name it `TaxStatus` but I'm not satisfied.

```f#
type TaxStatus = BaseTaxStatus * ImportStatus
```

Let's put those in our domain types file.

### The item, part 2

Can we go back to the item now? Meh, no.

### The price

We need to better model the price. Can the price be negative? I don't think shops give free money 👻 It can be zero? Yeah, probably it can. It should be very precise, so a decimal. But domain modeling techniques tell us that we can't pass ANY decimal. It has to be a specific `Price` decimal, with our guarantees in place. I won't be taking in consideration that we can just construct a bad value with, i.e., `Price -200.00M`. In a real application we would use private constructors, signature files, modules, tons of things (maybe too much actually, there should be a standard way to create constrained types). 

```f#
type Price =
    | Price of decimal
    
module Price =
    let Create amount =
        match amount with
        | amount when amount >= 0M -> Price amount
        | _ -> failwith "Price can't be negative!"
    let (|Price|) amount = amount
```

I don't like the `failwith` on the factory function (signature should be `decimal -> Price option`) but we can go back and fix it later. The active pattern (the thingie into the `(| |)` block) is needed to have a fast way to unpack in functions, e.g.

```f#
let printPrice (Price amount) = printfn "%M" amount
```

The `(Price amount)` part is a pattern match matching on the custom `Price` pattern that unpacks the underlying decimal.

### The item, part 3

We now have two proper parts of our item, correctly modeled; a `TaxStatus` and a `Price`. Let's start to check how a record
for the item type might look.

```f#
type Item =
    { Name: string
      Price: Price
      TaxStatus: TaxStatus }
```

I'm leaving out quantity as it's not intrinsic to the item, it's more part of the receipt (tuple of item and quantity?).
I'm doubtful that `TaxStatus` should stay on the item now; something `Local` here might be `Imported` in another nation.
For now, it's fine like this. Can we do something for the `Name`? Enforcing some rules, some pre-validation? Other than
not being empty, there's not much we can do.

## Second session

I had the doubt that `Item` could be an union type of different kinds of items, like:

```f#
type Item =
    | ExemptAndImportedItem
    | ExemptAndLocalItem
    | BaseTaxableAndLocalItem
    // and so on...
```

and it's absolutely apparent that this leads to combinatorial explosions. Having a single `TaxStatus` is better, it just does not belong to the `Item` record probably, so let's extract it from the `Item` record. We still didn't write a line of code other than the helper methods for `Price`. Let's also rename the item field to `UnitPrice`, to be more clear.

```f#
type Item = { Name: string; UnitPrice: Price }
```

### The price, part 2

Earlier I said that price can't be negative. Sure, it does not look like the price can be negative. But by talking with domain experts, we understandd that if the customer doesn't want an item **when it's already in the receipt**, the cashier can put the same item and the same quantity, just negative, to cancel out the price from the receipt/basket. (This is not a kata rule, just something I made up on the spot)

With this insight, I still prefer to leave `Price` constrained. We can relax the constrains later, when modeling the receipt, with a more open `TotalPrice` that is `Price * Quantity` and is allowed to go negative. Who cares of having 2-3 different types for prices and having to have one set of constraints for everyone?

### The receipt

The domain looks a bit foggy in the receipt part. We receive some string data from somewhere, in batch; the batch is composed by one or more lines of a structure representing the quantity, the name (with extractable tax data) and the unit price. I think a cool name could be `OrderRow`, but I'm not 100% satisfied.

The basic `OrderRow` looks like this in code:

```f#
type OrderRow =
    { Quantity: int
      Item: Item
      TaxStatus: TaxStatus }
```

This seems insufficient tho. Where's the `TotalPrice`? Where's the sales tax amount?
We could insert them in the record now and calculate them on parse, but I want to try with many many types today, so let's rename this to `ParsedOrderRow` and let's make another type called `CompleteOrderRow`. The names are sucky, I know this.

```f#
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
```

We miss the types for `TotalPrice` and `TaxPrice`, so let's write them before the rows. `TotalPrice` is pretty unconstrained, so it's just a single case union deriving from `decimal`. `TaxPrice`, thinking about it has some constraints in that:

* can exist only in presence of a sale, so a `TotalPrice` or a `Price`;
* can be calculated only if, with the `Price`, there is the taxed percentage;
* has to be rounded up to the nearest `0.05`.

It's probably fine using a factory function that derives it from a `TotalPrice` and a percentage. All the thingies on private constructor and loads of tests (with FsCheck preferably) stays valid 🥸 I'll probably write some tests later, yes yes I will 🥸

```f#
type TotalPrice = TotalPrice of decimal

type TaxPrice = TaxPrice of decimal

module TaxPrice =
    let CreateFromTotalPrice percentageTaxed (TotalPrice totalPrice) =
        totalPrice
        |> fun totalPrice -> (totalPrice * decimal percentageTaxed) / 100M
        |> fun preRoundPrice -> ceil (preRoundPrice * 20M) / 20M
        |> TaxPrice

    let (|TaxPrice|) amount = amount
```

Obviously the rounding function is hardcoded, but can be easily extracted. When, and if, the time is right. Can easily go into the `TaxPrice` module, or a domain services module. We could skip the calculations by pattern matching for `0`, but it's honestly overkill. Another thing we could do is declare a type for the percentage of taxation, forcing it to be non-negative. Won't do it right now.

Huh, we were talking about the receipt? We wrote a lot of types and little "real" code. I hope we won't be bitten in the popo later.

### Our first "complex" function

The factory from `ParsedOrderRow` to `CompleteOrderRow` is not something to scoff at; needs the argument annotated as inference cannot work out that we're enriching a "lesser" type. A huge red flag emerges that we probably need to move some functions used here in the types, like the match to get the int percentage. And we probably need a `Value` function for the Price as the active pattern is not sufficient (or my knowledge of the syntax is too low right now). I think active patterns can solve the TaxStatus combinatorial explosion in the match too, as I've seen something similar for FizzBuzz in F#.

```f#
module CompleteOrderRow =
    let CreateFromParsedOrderRow (parsedOrderRow: ParsedOrderRow) =
        let taxPercentage =
            match parsedOrderRow.TaxStatus with
            | Exempt, Local -> 0
            | Exempt, Imported -> 5
            | FullyTaxed, Local -> 10
            | FullyTaxed, Imported -> 15

        let totalPrice =
            let unitPrice =
                match parsedOrderRow.Item.UnitPrice with
                | Price amount -> amount

            TotalPrice(unitPrice * (decimal parsedOrderRow.Quantity))

        { Quantity = parsedOrderRow.Quantity
          Item = parsedOrderRow.Item
          TaxStatus = parsedOrderRow.TaxStatus
          TotalPrice = totalPrice
          SalesTax = TaxPrice.CreateFromTotalPrice taxPercentage totalPrice }
```

Let's refactor a bit.

### Random refactor for conciseness

The new `TaxStatus` type:

```f#
type TaxStatus = BaseTaxStatus * ImportStatus

module TaxStatus =
    let GetPercentage status =
        match status with
        | Exempt, Local -> 0
        | Exempt, Imported -> 5
        | FullyTaxed, Local -> 10
        | FullyTaxed, Imported -> 15
```

This goes under the `Price` module:

```f#
let Value (Price price) = price
```

And this is the new `CompleteOrderRow` module:

```f#
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
```

I like to keep variables not inlined so the record constructor looks neater, but it's a personal choice. That total price logic is still a longish chain of semi-anonymous things so maybe it's not easily followed, but I'm fine with having extracted the tax percentage logic into the type. Will think about 

### Recap

We now have a buttload of types, but some kind of form is emerging. An `Order`, or a Receipt, is basically a collection of `CompleteOrderRow`. We know that an order with 0 rows makes absolutely no sense, so, instead of having `type Order = { Rows: CompleteOrderRow list }` in the real world we would probably use [`NonEmptyList` from FSharpPlus.Data](https://fsprojects.github.io/FSharpPlus/reference/fsharpplus-data-nonemptylist.html), but for now a simple `list` is ok.

```f#
type Order = { Rows: CompleteOrderRow list }
```

The chain our kata will follow is now sort of clear: a string enters the system, it's transformed into some `ParsedOrderRow`s that become a list of `CompleteOrderRow` that becomes an `Order`. From there we can output the receipt string.

## Third session

It's harder to keep track of my flow of thought since I'm tired :)

I wrote some more code, it's starting to look like a mess but the core domain logic is there, on the place where I think it makes sense. I'm missing the parser in the domain services and I'll try to refactor. I didn't use TDD techniques as they impose me YALOTTKTO *(yet another layer of things to keep tabs on)* 😁 hope my colleagues don't see this 😆 jk, this is more an exercise to see where the type system brings me rather than a "let's write it correctly".

I had to add some `Value` functions to unpack from the custom type. I'm probably missing some FP concept here, because a custom type looks a lot like a `Some` so there should be some thingy I can do to work better with those "constrained" types around the app. (Remember to check out lenses). Sadly I still could not finish SW book, so maybe I'm anticipating things.

I wrote `Order` this time as a type with members rather than a type + a module of functions, just to see how it works in `fsi`, and wrote a couple of helper methods to avoid having a huge `ToReceipt` func. Not having to constantly pass args is cool, I suppose you have to use currying more in real apps. I didn't like `List.append` in the pipeline but I refactored it out before committing so you won't see it. I think appending to the end of a list is so hard that reducing to string and concatenating is the better approach.

Refactor opportunity: I don't like `Order` as a record. No value gained by it, would have been easier as a simple type alias.

## Fourth session

Early morning session before work FTW!

```f#
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
```

This is the "refactored" TaxStatus module, as I really disliked that matrix of patterns on tax-statuses.

I decided against writing the parser for now, it's not really important for now as it's just an exercise in regex matching or FParsec usage.

A couple late afternoons later, it's time to refactor `Price` to be an option; domain records will stay the same as I want the domain to be the most error/option free as possible; all bad stuff has to stay at the edges. The `Create` function for prices is a good candidate to return option tho.  
