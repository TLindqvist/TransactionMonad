namespace TransactionMonad

module TransactionBuilder =
    type TransactionBuilder() =
        member _.Return(x: 'a) : Transaction<'a> =
            fun _ _ -> TransactionResult x

        member _.Bind
            (m: Transaction<'a>, f: 'a -> Transaction<'b>)
            : Transaction<'b> =
            Transaction.bind f m

        member _.Zero() : Transaction<unit> = fun _ _ -> TransactionResult()


    let transaction = TransactionBuilder()
