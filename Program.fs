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

        let compositionCE id =
            transaction {
                let! order = DbStuff.loadOrder id
                do! DbStuff.updateOrderStatus "new-status" order.Id
                let! updatedOrder = DbStuff.loadOrder id
                return updatedOrder
            }

        task {
            let! result = run (compositionCE 1) getConn ()
            printfn "Transaction result: %A" result
        }
        |> ignore

        0
