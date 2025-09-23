namespace TransactionMonad

module DbStuff =

    let select1 value =
        fun (conn: DbConn) (tx: DbTransaction) ->
            task {
                printfn "Querying for %i" value
                return TransactionResult "PELLE WAS HERE"
            }

    let update1 value =
        fun (conn: DbConn) (tx: DbTransaction) ->
            task {
                printfn "Updating %s" value
                return TransactionResult value
            }
