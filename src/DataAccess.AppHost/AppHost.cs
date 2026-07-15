var builder = DistributedApplication.CreateBuilder(args);

var postgresDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("orders-postgres", "ordersdb");

var sqlDb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("orders-sqlserver", "OrdersDb");

builder.AddProject<Projects.DataAccess_Api>("api")
    .WithReference(postgresDb)
    .WithReference(sqlDb)
    .WaitFor(postgresDb)
    .WaitFor(sqlDb);

await builder.Build().RunAsync();
