namespace TransactionMonad

open TransactionMonad.Transaction
open TransactionMonad.TransactionBuilder
open TransactionResult

module Program =

    // Basic composition
    let selectAndUpdate input =
        DbStuff.loadOrder input
        |> bind (fun order -> DbStuff.updateOrderStatus "new-status" order.Id)

    // "Kleisli" composition
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

        // Composition with computation expression builder
        let updateOrder orderId =
            transaction {
                let! order = DbStuff.loadOrder orderId
                do! DbStuff.updateOrderStatus "new-status" order.Id
                let! updatedOrder = DbStuff.loadOrder orderId
                return updatedOrder
            }

        task {
            let! result1 = run (updateOrder 1) getConn ()
            printfn "Transaction result 1: %A" result1

            let! result2 = run (updateOrder 2) getConn ()
            printfn "Transaction result 2: %A" result2
        }
        |> ignore


        let updateManyOrders status orderIds =
            transaction {
                for id in orderIds do
                    do! DbStuff.updateOrderStatus status id

                return ()
            }

        task {
            // Will succeed to update both orders
            let! result1 =
                run
                    (updateManyOrders
                        "status-1"
                        [
                            1
                            2
                        ])
                    getConn
                    ()

            printfn "Transaction with for..do: %A" result1

            // Will rollback update on order 1 when 3 is not found
            let! result2 =
                run
                    (updateManyOrders
                        "status-2"
                        [
                            1
                            3
                        ])
                    getConn
                    ()

            printfn "Transaction with for..do: %A" result2

            // Succeeds but is a NoOp since list of order ids is empty
            let! result3 = run (updateManyOrders "status-3" []) getConn ()

            printfn "Transaction with for..do: %A" result3


        }
        |> ignore

        0
