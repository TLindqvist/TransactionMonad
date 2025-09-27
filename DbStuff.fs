namespace TransactionMonad

module DbTypes =

    [<CLIMutable>]
    type DbOrderHead =
        {
            Id: int
            Supplier: string
            Status: string
        }

    [<CLIMutable>]
    type DbOrderLine =
        {
            Id: int
            OrderId: int
            Item: string
            Quantity_Amount: int
            Quantity_UoM: string
        }


module Helpers =
    module Option =

        let toResult =
            function
            | Some x -> Ok x
            | None -> Error "Expected Some found None"

    module List =
        let traverseResultM
            (f: 'a -> Result<'b, 'c>)
            (values: 'a list)
            : Result<'b list, 'c> =
            let rec innerFn (state: 'b list) values' =
                match values' with
                | [] -> state |> List.rev |> Ok
                | x :: xs ->
                    let b = f x

                    match b with
                    | Ok b' -> innerFn (b' :: state) xs
                    | Error err -> Error err

            innerFn [] values

    let groupLeftJoined (data: ('a * 'b) seq) : ('a * ('b seq)) seq =
        data
        |> Seq.groupBy fst
        |> Seq.map (fun (left, pairs) ->
            let rights =
                pairs |> Seq.choose (fun x -> Option.ofObj (snd x))

            left, rights)


    module Order =
        open DbTypes

        let private toDbOrderHead (order: Order) =
            {
                DbOrderHead.Id = order.Id
                Supplier = order.Supplier
                Status = order.Status
            }

        let private toDbOrderLine (order: Order) (line: OrderLine) =
            let (amount, uom) =
                Quantity.toValues line.Quantity

            {
                DbOrderLine.Id = line.Id
                OrderId = order.Id
                Item = line.Item
                Quantity_Amount = amount
                Quantity_UoM = uom
            }

        let toDb (order: Order) =
            toDbOrderHead order,
            order.OrderLines |> List.map (toDbOrderLine order)

        // I will remove hardcoded qunatity later
        let fromDbOrderLine line : Result<OrderLine, string> =
            Quantity.fromValues line.Quantity_Amount line.Quantity_UoM
            |> Result.map (fun qty ->
                {
                    OrderLine.Id = line.Id
                    Item = line.Item
                    Quantity = qty
                })

        let fromDb (dbHead: DbOrderHead, dbLines: DbOrderLine seq) =
            dbLines
            |> Seq.toList
            |> List.traverseResultM fromDbOrderLine
            |> Result.map (fun orderLines ->
                {
                    Order.Id = dbHead.Id
                    Supplier = dbHead.Supplier
                    OrderLines = orderLines
                    Status = dbHead.Status
                })

    module TransactionResult =

        let inline requireSingleCount
            (x: 'a)
            (count: int)
            : TransactionResult<'a> =
            if count = 1 then
                Success x
            else
                Failure <| sprintf "Expected single but got %i" count

        let inline requireSingle (xs: 'a seq) : TransactionResult<'a> =
            let taken =
                xs |> Seq.truncate 2 |> Seq.toList

            match taken with
            | [ x ] -> Success x
            | [] -> Failure "Expected single but got no items"
            | _ -> Failure "Expected single but found multiple items"


module DbStuff =
    open Dapper.FSharp.SQLite
    open DbTypes
    open Helpers
    open Microsoft.Data.Sqlite
    open System.Data

    let configure () = OptionTypes.register ()

    let createConnection connStr () : IDbConnection =
        new SqliteConnection(connStr)

    let orderHeadTable =
        table'<DbOrderHead> "OrderHead"

    let orderLinesTable =
        table'<DbOrderLine> "OrderLine"

    let loadOrder id : Transaction<Order> =
        fun (conn: IDbConnection) (tx: IDbTransaction) ->
            task {
                let! queryResults =
                    conn.SelectAsync<DbOrderHead, DbOrderLine>(
                        select {
                            for oh in orderHeadTable do
                                leftJoin ol in orderLinesTable
                                                   on
                                                   (oh.Id = ol.OrderId)

                                where (oh.Id = id)
                        },
                        tx
                    )

                let order =
                    groupLeftJoined queryResults
                    |> Seq.tryHead
                    |> Option.toResult
                    |> Result.bind Order.fromDb


                match order with
                | Error err ->
                    return
                        Failure
                        <| sprintf
                            "Could not load order with id %i since %s"
                            id
                            err
                | Ok order' -> return Success order'
            }

    let updateOrderStatus (status: string) (orderId: int) : Transaction<unit> =
        fun (conn: IDbConnection) (tx: IDbTransaction) ->
            task {
                let! count =
                    conn.UpdateAsync(
                        update {
                            for o in orderHeadTable do
                                setColumn o.Status status
                                where (o.Id = orderId)
                        },
                        tx
                    )

                return TransactionResult.requireSingleCount () count
            }
