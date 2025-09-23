namespace TransactionMonad

open TransactionMonad.Transaction
open TransactionMonad.TransactionBuilder

module Program =
    let conn =
        {
            ConnectionString = "a-connection-string"
        }

    let tx = { TransactionId = 1 }

    let selectAndUpdate input =
        DbStuff.select1 input |> bind DbStuff.update1

    let selectAndUpdate' =
        DbStuff.select1 >=> DbStuff.update1

    [<EntryPoint>]
    run (selectAndUpdate 1) conn |> ignore

    let getId = transaction { return 42 }

    let pelleComposition =
        transaction {
            let! a = getId
            let! b = selectAndUpdate a

            printfn "First part of CE: %i %s" a b

            let! c = selectAndUpdate' a
            return c
        }

    printfn "Starting example 2"

    run pelleComposition conn |> ignore
