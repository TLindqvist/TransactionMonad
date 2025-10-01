# TransactionMonad

Exploring CE builders by creating a "transaction context", i.e. a transaction monad. Usable for composing different parts of a db transaction.

## What's nice about monads

I like monads as a design pattern since:

1. It allows us to encapsualte and manage side-effects
2. It allows of to compose those side-effects

Examples of this are `Promise`, `Task` and `Future`,  the name varies depending on programming language. Using `Task` in for example C# removes the need think about threads and thread pools, that side-effect is managed for us. Also, we can compose tasks (2) in a variety of ways. The most common way of composing them is to sequence them with `ContinueWith`. In JavaScript it would look similair but use `then` instead of `ContinueWith`.

```cs
using var cts = new CancellationTokenSource();
var scheduler = TaskScheduler.Default;

// Run task synchronously
Task.Run(
        () =>
        {
            Console.WriteLine("Starting...");
            return Task.CompletedTask;
        },
        cts.Token)
    .ContinueWith(
        prevTask =>
        {
            Console.WriteLine("Wait for 3 seconds");
            Task.Delay(3000).Wait();
        },
        scheduler)
    .ContinueWith(
        prevTask =>
        {
            Console.WriteLine("Finished...");
        },
        TaskScheduler.Default)
    .Wait();
```

In fact it turns out we are looking at the programming version of a monad. If we have a function returning a data structure that is a version of `SomeType<T>` **and** it can be sequenced it is likely that we are using the monad design pattern. When sequencing we get a new "flattened" `SomeType<T>`. By sequencing I mean "do this, then that". In my mind I visualize it as below. No matter hpw many `SomeType<T>` we sequence it will result in one "new" `SomeType<T>`.

![Sometype](/assets/sometype.png)

Other examples are:

- `List<T>` can be sequence with `SelectMany`
- `Nullable<T>` can be sequenced but C# lets you do this imperatively by yourself

A transaction monad or transaction context is simply a `Transaction<T>` structure that can be sequenced.

## Computation expressions

The code above is often referred to as "callback hell". It is often harder to reason about if we got deep nesting of callbacks and especiallly when async operations are involved. To avoid this many languages support `async`/`await` for async tasks. The above code using async await would look like:

```cs
using var cts = new CancellationTokenSource();

Console.WriteLine("Starting...");

Console.WriteLine("Wait for 3 seconds");
await Task.Delay(3000, cts.Token);

Console.WriteLine("Finished...");
```

Much shorter and easier to reason about. This is, unfortunately, not supported for other monads than `Task<T>`, at least not in C#. In F# however, we are free to do this for any monad we like as long as we create a computation expression builder (CE builder). In F# some commonly used CE builder is already predefined, `task` is one of them and the above code would look like:

```fs
task {
  printfn "Starting..."
  printfn "Wait for 3 seconds"
  do! Task.Delay 3000
  printfn "Finished..."
}
```

Instead of await we use `do!` to await for something that does not return any data. If it would return data we would use `let!`. CE builders are more flexible and supports, custom implementation for 8 different "expression forms" plus loops, exception handling and custom operations.

In this repository the computation expression also has implementation for:

- `for .. do`, allows to loop through commands that return `()`

Besides `task` other examples of computation expressions implementing sequencing with `let!` and `do!` are:

- `transaction` in this repository
- `async` an alternative async model in (F# core library)[https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/async-expressions]
- `result` in [FsToolkit.ErrorHandling](https://demystifyfp.gitbook.io/fstoolkit-errorhandling/fstoolkit.errorhandling/result/ce)

For examples of computation expressions used in other ways the following are interesting examples:

- Multitude of them in [Farmer](https://compositionalit.github.io/farmer/)
- `application` in [Saturn Framework](https://saturnframework.org/)
- `http` in [FsHttp](https://github.com/fsprojects/FsHttp)

## Rant about ORM's

- Using an ORM, for example EF Core can give you similair benefits.

- However, I have seen too many examples of ORM's being misunderstood or a hinderance:

    - Not understanding transactions and calling `SaveChanges` multiple times and thus committing inconsistent data
    - Attaching already existing data to the `DbContext` resulting in EF Core trying to INSERT it even if it already exists
    - Mixing tracked and untracked data in incorrect ways
    - Misusing the freedom that EF Core and LINQ gives to load data in inconsitent ways. The effect being API calls fails since some nested data is sometimes loaded and sometimes not.
    - Misuing the freedom that EF Core and LINQ gives to creating very expensive queries.
    - Configured query filters causes bugs since it is easy to miss that a query will be run with more clauses than you specified
    - Magic behavior from custom interceptors performing stuff behind the scenes
    - You or your colleagues creating overly complex solutions since EF Core cant do something that the programming language and database is capable of doing
    - Inheritance becomes more prevalent since composition is not supported
    - I think the main reason for it beig hard to use for many programmers is that requires the programmer to understand a larger porttion of the stack:
        - You still need to be able to think in SQL and DB
        - You need to be able to configure and understand the implications of the entity configurations
        - Since it is heavily connected to dependency injection container, the programmer needs to understand hw that affects transactions and changer tracking
        - Whether data is tracked or not will affect what mutation of entities does

- Dapper.Fsharp wraps Dapper, a micro-ORM, and provides som basic computation expression builders for making basic queries.
- One downside compared to EF Core is that you have to update the DbModels, perform mapping to your domain models
- A second downside compared to EF core is that you are required to do extra work when using some sort of joined queries
- Dapper.Fsharp does not support such complex queries as LINQ and EF Core does. This means that you might have to resort to raw SQL. Personally, I am fine with that since the database layer is testable,e specially with test containters.
- The biggest upside is that the persistance layer will be a separate module will work the same way all of the time.  
  A layer that can be tested in isolation. A layer that can be used by less experienced programmers.

## FsToolkit.ErrorHandling

One of the most reusable packages in all of the F# ecosystem is FsToolkit.ErrorHandling. It uses a lot of the design patterns that is common in functional programming. I have chosen to add it as one of tha last commits in this experiment so that the difference it makes can be viewed. Although minor the difference in `DbStuff` is a nice one.