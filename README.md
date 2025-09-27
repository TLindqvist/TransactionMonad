# TransactionMonad

Exploring CE builders by creating a "transaction context", i.e. a transaction monad. Usable for composing different parts of a db transaction.

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
