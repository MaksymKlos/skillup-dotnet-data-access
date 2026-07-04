using DataAccess.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGet("/", () => "dotnet-data-access-playground: EF Core (write) + Dapper (read)");

await app.RunAsync();
