namespace TransactionMonad

type DbConn = { ConnectionString: string }

type DbTransaction = { TransactionId: int }

type TransactionResult<'a> = TransactionResult of 'a

module TransactionResult =
    let map f (TransactionResult x) = TransactionResult(f x)


type Transaction<'a> = DbConn -> DbTransaction -> TransactionResult<'a>

module Transaction =

    let returnM (x: 'a) : Transaction<'a> = fun _ _ -> TransactionResult x

    let bind (f: 'a -> Transaction<'b>) (m: Transaction<'a>) : Transaction<'b> =
        fun conn tx ->
            let (TransactionResult a) = m conn tx
            let mb = f a
            mb conn tx

    let (>>=) = bind

    let compose
        (first: 'a -> Transaction<'b>)
        (second: 'b -> Transaction<'c>)
        : 'a -> Transaction<'c> =
        fun input (conn: DbConn) -> bind second (first input) conn


    let (>=>) = compose

    let run (tx: Transaction<'a>) (conn: DbConn) =
        printfn "Create transaction scope"
        let pelle = tx conn
        printfn "Result: %A" pelle
        printfn "Commiting transaction"
        pelle
