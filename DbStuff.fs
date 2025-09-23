namespace TransactionMonad

module DbStuff =

    let select1 value =
        fun (conn: DbConn) (tx: DbTransaction) ->
            printfn "Querying for %i" value
            TransactionResult "PELLE WAS HERE"

    let update1 value =
        fun (conn: DbConn) (tx: DbTransaction) ->
            printfn "Updating %s" value
            TransactionResult value
