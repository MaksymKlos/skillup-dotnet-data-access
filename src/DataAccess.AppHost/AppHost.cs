var builder = DistributedApplication.CreateBuilder(args);

var postgresDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("ordersdb");

var sqlDb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("OrdersDb");

builder.AddProject<Projects.DataAccess_Api>("api")
    .WithReference(postgresDb)
    .WithReference(sqlDb)
    .WaitFor(postgresDb)
    .WaitFor(sqlDb);

await builder.Build().RunAsync();
