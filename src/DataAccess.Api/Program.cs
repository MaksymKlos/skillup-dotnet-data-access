using DataAccess.Api.Messaging;
using DataAccess.Infrastructure.Dapper;
using DataAccess.Infrastructure.EfCore;
using DataAccess.ServiceDefaults;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddEfCore(builder.Configuration);
builder.Services.AddDapper(builder.Configuration);

builder.AddRabbitMQClient("rabbitmq");
builder.Services.AddHostedService<OutboxProcessor>();
builder.Services.AddHostedService<OrderEventsConsumer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();

app.MapGet("/", () => "dotnet-data-access-playground: EF Core (write) + Dapper (read)");

await app.RunAsync();
