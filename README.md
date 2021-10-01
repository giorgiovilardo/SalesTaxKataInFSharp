# Sales Tax Kata in F#

[Kata Text](KATA.md)

This is more of a journal rather than a readme, as it's a sequential dump of how I approach the problem posed by the kata. It's a living document given how little time I have, so I hope to come back to it.

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
For now, it's fine like this.
