namespace TransactionMonad

open System.Threading.Tasks
open System.Data

type TransactionResult<'a> =
    | Success of 'a
    | Failure of string

type Transaction<'a> =
    IDbConnection -> IDbTransaction -> Task<TransactionResult<'a>>

module TransactionResult =
    let map f x =
        task {
            match! x with
            | Failure err -> return Failure err
            | Success x' -> return Success <| f x'
        }

module Transaction =

    let returnM (x: 'a) : Transaction<'a> = fun _ _ -> task { return Success x }

    let bind (f: 'a -> Transaction<'b>) (m: Transaction<'a>) : Transaction<'b> =
        fun conn tx ->
            task {
                let! x = m conn tx

                match x with
                | Success x' ->
                    let mb = f x'
                    return! mb conn tx
                | Failure err -> return Failure err
            }

    let (>>=) = bind

    let compose
        (first: 'a -> Transaction<'b>)
        (second: 'b -> Transaction<'c>)
        : 'a -> Transaction<'c> =
        fun input (conn: IDbConnection) -> bind second (first input) conn


    let (>=>) = compose

    let run (tx: Transaction<'a>) (connFactory: unit -> IDbConnection) () =
        task {
            try
                use conn = connFactory ()
                conn.Open()

                use ts =
                    conn.BeginTransaction IsolationLevel.ReadCommitted

                match! tx conn ts with
                | Success result ->
                    ts.Commit()
                    conn.Close()
                    return Ok result
                | Failure err ->
                    ts.Rollback()
                    conn.Close()
                    return Error err
            with ex ->
                return Error <| sprintf "Error: %A" ex
        }
