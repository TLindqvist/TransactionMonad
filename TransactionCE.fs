namespace TransactionMonad

module TransactionBuilder =
    type TransactionBuilder() =
        member _.Return(x: 'a) : Transaction<'a> =
            fun _ _ -> task { return Success x }

        member _.Bind
            (m: Transaction<'a>, f: 'a -> Transaction<'b>)
            : Transaction<'b> =
            Transaction.bind f m

        member _.Zero() : Transaction<unit> =
            fun _ _ -> task { return Success() }


    let transaction = TransactionBuilder()
