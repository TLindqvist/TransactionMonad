namespace TransactionMonad

open DbUp

module DbMigrations =

    let migrate (connectionString: string) =

        let upgrader =
            DeployChanges.To
                .SqliteDatabase(connectionString)
                .WithScriptsFromFileSystem(@".\db\migrations\")
                .WithScriptsFromFileSystem(@".\db\seed\")
                .LogToConsole()
                .Build()

        printfn "Starting migrations..."

        let result = upgrader.PerformUpgrade()

        if result.Successful then
            Ok "Migrations successful"
        else
            Error <| sprintf "Migrations failed: %A" result.Error
