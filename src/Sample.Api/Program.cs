using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Sample.Api;
using Sample.Api.Consumers;
using Sample.Api.StateMachines;
using Sample.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMassTransit(x =>
{
    x.AddConsumersFromNamespaceContaining<NotifyCustomerOrderSubmittedConsumer>();
    x.AddSagaStateMachine<OrderStateMachine, OrderState, OrderStateDefinition>()
        .RedisRepository(r => r.DatabaseConfiguration(builder.Configuration.GetConnectionString("Redis")));

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration.GetConnectionString("RabbitMq"));

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/Order", async ([FromBody] Order order, IRequestClient<SubmitOrder> client) =>
{
    var response = await client.GetResponse<OrderSubmissionAccepted>(new SubmitOrder(order.OrderId));

    return Results.Ok(new
    {
        response.Message.OrderId
    });
});

app.MapGet("/Order/{id:guid}", async (Guid id, IRequestClient<GetOrderStatus> client) =>
{
    var response = await client.GetResponse<OrderStatus, OrderNotFound>(new GetOrderStatus(id));

    return response.Is(out Response<OrderStatus>? order)
        ? Results.Ok(order!.Message)
        : Results.NotFound();
});

app.Run();

public partial class Program
{
}