using DataAccess.Api.Endpoints;
using DataAccess.Api.Messaging;
using DataAccess.Application.Abstractions;
using DataAccess.Application.Common;
using DataAccess.Application.Orders.Commands;
using DataAccess.Application.Products.Commands;
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

builder.Services.AddScoped<ICommandHandler<CreateProductCommand, Result<Guid>>, CreateProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<RestockProductCommand, Result>, RestockProductCommandHandler>();
builder.Services.AddScoped<ICommandHandler<PlaceOrderCommand, Result<Guid>>, PlaceOrderCommandHandler>();

builder.Services.AddOpenApi();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapDefaultEndpoints();
app.MapOpenApi();

app.MapGet("/", () => "dotnet-data-access-playground: EF Core (write) + Dapper (read)");

app.MapProductEndpoints();
app.MapOrderEndpoints();
app.MapOutboxEndpoints();
app.MapDemoEndpoints();

await app.RunAsync();
