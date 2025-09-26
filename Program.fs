namespace TransactionMonad

open TransactionMonad.Transaction
open TransactionMonad.TransactionBuilder
open TransactionResult

module Program =
    let selectAndUpdate input =
        DbStuff.loadOrder input
        |> bind (fun order -> DbStuff.updateOrderStatus "new-status" order.Id)

    let selectAndUpdate': int -> Transaction<Unit> =
        DbStuff.loadOrder
        >=> fun order -> DbStuff.updateOrderStatus "new-status" order.Id

    [<EntryPoint>]
    let main args =
        DbStuff.configure ()

        let connStr =
            args
            |> Array.tryHead
            |> Option.defaultValue "Data Source=ExistingDb.db"

        DbMigrations.migrate connStr |> ignore

        let getConn =
            DbStuff.createConnection connStr

        let updateOrder orderId =
            transaction {
                let! order = DbStuff.loadOrder orderId
                do! DbStuff.updateOrderStatus "new-status" order.Id
                let! updatedOrder = DbStuff.loadOrder orderId
                return updatedOrder
            }

        task {
            let! result = run (updateOrder 1) getConn ()
            printfn "Transaction result: %A" result
        }
        |> ignore

        0
