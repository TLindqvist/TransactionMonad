namespace TransactionMonad

module TransactionBuilder =
    type TransactionBuilder() =
        member _.Return(x: 'a) : Transaction<'a> =
            fun _ _ -> task { return Success x }

        member _.ReturnFrom(m: Transaction<'a>) : Transaction<'a> = m

        member _.Bind
            (m: Transaction<'a>, f: 'a -> Transaction<'b>)
            : Transaction<'b> =
            Transaction.bind f m

        member _.Zero() : Transaction<unit> =
            fun _ _ -> task { return Success() }


        member _.For
            (seq: seq<'a>, f: 'a -> Transaction<unit>)
            : Transaction<unit> =
            fun conn tx ->
                task {
                    let mutable result = Success()

                    for item in seq do
                        match result with
                        | Success _ ->
                            let! itemResult = f item conn tx
                            result <- itemResult
                        | Failure err -> ()

                    return result
                }

        member _.Combine
            (first: Transaction<unit>, second: Transaction<'a>)
            : Transaction<'a> =
            Transaction.bind (fun _ -> second) first

        member _.Delay(f: unit -> Transaction<'a>) : Transaction<'a> = f ()

    let transaction = TransactionBuilder()
