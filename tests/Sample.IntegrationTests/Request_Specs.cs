using System.Text;
using MassTransit;
using MassTransit.Testing;
using Sample.Api.StateMachines;
using Sample.Contracts;
using Xunit.Abstractions;

namespace Sample.IntegrationTests;

public sealed class Request_Specs : IClassFixture<SampleWebApplicationFactory>
{
    private readonly SampleWebApplicationFactory _factory;
    private readonly ITestOutputHelper _outputHelper;

    public Request_Specs(SampleWebApplicationFactory factory, ITestOutputHelper outputHelper)
    {
        _factory = factory;
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task Should_use_correlation_id_for_request_id()
    {
        // Arrange
        ITestHarness harness = _factory.Services.GetTestHarness();
        await harness.Start();

        IRequestClient<SubmitOrder>? client = harness.GetRequestClient<SubmitOrder>();
        ISagaStateMachineTestHarness<OrderStateMachine, OrderState>? sagaHarness = harness.GetSagaStateMachineHarness<OrderStateMachine, OrderState>();
        
        // Act
        await client.GetResponse<OrderSubmissionAccepted>(new SubmitOrder(Guid.NewGuid()));

        await harness.OutputTimeline(new TextWriterHelper(_outputHelper));
        await harness.InactivityTask;

        await Task.Delay(TimeSpan.FromSeconds(5));
        
        // Assert
        Assert.True(await harness.Sent.Any<SubmitOrder>(), "Submit Order should have been sent");
        Assert.True(await sagaHarness.Consumed.Any<OrderValidated>(), "Order Validated should have passed");
        Assert.True(await harness.Published.Any<OrderAccepted>(), "OrderAccepted should have passed");

        await harness.Stop();
    }
}

public class TextWriterHelper : TextWriter
{
    private readonly ITestOutputHelper _helper;
    public override Encoding Encoding { get; }

    public TextWriterHelper(ITestOutputHelper helper)
    {
        _helper = helper;
    }

    public override void WriteLine(string? value)
    {
        _helper.WriteLine(value);
    }
}