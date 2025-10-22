using Bazlama.AsyncOperationSuite;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Services;

namespace Example.Web.Api.AsyncOperations;

public class DelayOperationPayload: AsyncOperationPayloadBase
{
    public int DelaySeconds { get; set; } = 1;
    public int StepCount { get; set; } = 15;
}

public class DelayOperationProcessor : AsyncOperationProcess<DelayOperationPayload>
{
    public DelayOperationProcessor(
        DelayOperationPayload payload,
        AsyncOperation asyncOperation,
        AsyncOperationService asyncOperationManager)
        : base(payload, asyncOperation, asyncOperationManager)
    {
    }

    protected override async Task OnExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Starting the process, {Payload.Name} - {Payload.DelaySeconds} second(s) | {Payload.StepCount} step(s)");

        int totalSeconds = 0;
		for (var i = 0; i < Payload.StepCount; i++)
        {
            var progress = (i + 1) * 100 / Payload.StepCount;
            await PublishProgress($"Step {i + 1} of {Payload.StepCount}", progress, cancellationToken);
            await Task.Delay(Payload.DelaySeconds * 1000, cancellationToken);
            Console.WriteLine($"Step {i + 1} of {Payload.StepCount} completed");
			totalSeconds += Payload.DelaySeconds;
		}

		SetResult($"{totalSeconds}", $"Delay operation '{Payload.Name}' completed successfully.");
		Console.WriteLine($"Delay operation '{Payload.Name}' completed successfully.");
	}
}
