using MassTransit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Testcontainers.RabbitMq;
using Testcontainers.Redis;

namespace Sample.IntegrationTests;

public sealed class SampleWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly RabbitMqContainer _rabbitMqContainer = new RabbitMqBuilder()
        .WithPortBinding(5672, true)
        .WithPortBinding(15672, true)
        .Build();

    private readonly RedisContainer _redisContainer = new RedisBuilder()
        .WithPortBinding(6379, true)
        .Build();
    
    
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            builder.UseSetting("ConnectionStrings:Redis", _redisContainer.GetConnectionString());
            builder.UseSetting("ConnectionStrings:RabbitMq", _rabbitMqContainer.GetConnectionString());
            services.AddMassTransitTestHarness();
        });
    }

    public async Task InitializeAsync()
    {
        await _rabbitMqContainer.StartAsync();
        await _redisContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _rabbitMqContainer.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }
}