namespace TransactionMonad

type Quantity =
    | Pieces of int
    | Cartons of int

type OrderLine =
    {
        Id: int
        Item: string
        Quantity: Quantity
    }

type Order =
    {
        Id: int
        Supplier: string
        OrderLines: OrderLine list
        Status: string
    }


module Quantity =

    let toValues =
        function
        | Pieces qty -> qty, "Pcs"
        | Cartons qty -> qty, "Ctns"


    let fromValues qty =
        function
        | "Pcs" -> Ok <| Pieces qty
        | "Ctns" -> Ok <| Cartons qty
        | uom -> Error <| sprintf "Unknown unit of measure %s" uom
