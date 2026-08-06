var builder = DistributedApplication.CreateBuilder(args);

var postgresDb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .AddDatabase("orders-postgres", "ordersdb");

var sqlDb = builder.AddSqlServer("sql")
    .WithDataVolume()
    .AddDatabase("orders-sqlserver", "OrdersDb");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithDataVolume()
    .WithManagementPlugin();

builder.AddProject<Projects.DataAccess_Api>("api")
    .WithReference(postgresDb)
    .WithReference(sqlDb)
    .WithReference(rabbitmq)
    .WaitFor(postgresDb)
    .WaitFor(sqlDb)
    .WaitFor(rabbitmq);

await builder.Build().RunAsync();
