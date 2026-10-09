# Green Age API

.NET 8 ASP.NET Core Web API solution. Run from this folder:

```powershell
dotnet run --project src/GreenAge.Api
```

The API listens at `http://localhost:5080`; Development Swagger is at `/swagger`. By default `Storage:Provider` is `Json`. JSON files are created under `src/GreenAge.Api/data` at runtime and are seeded with the demo categories and accounts.

Set `Storage:Provider` to `Sql` to select EF Core repositories. `Storage:SqlEngine` supports `Sqlite` (default) or `SqlServer`; set `ConnectionStrings:Default` for the database connection. Development startup applies the `InitialCreate` migration and seeds categories if empty. Other environments create the schema if it does not exist.

Run service tests with:

```powershell
dotnet test GreenAge.slnx
```

Demo accounts: `deliver@demo.com` and `collector@demo.com`, both with password `Demo@123`.
