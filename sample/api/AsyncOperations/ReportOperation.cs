using Bazlama.AsyncOperationSuite;
using Bazlama.AsyncOperationSuite.Models;
using Bazlama.AsyncOperationSuite.Services;

namespace Example.Web.Api.AsyncOperations;

public enum ReportType
{
	Daily,
	Weekly,
	Monthly
}

public class ReportOperationPayload: AsyncOperationPayloadBase
{
    public DateTime ReportStartDate { get; set; }
    public DateTime ReportEndDate { get; set; }
    public ReportType ReportType { get; set; } = ReportType.Daily;
    public String ReportDescription { get; set; } = string.Empty;
}

public class AsyncOperationTestProcessor : AsyncOperationProcess<ReportOperationPayload>
{
    public AsyncOperationTestProcessor(
        ReportOperationPayload payload,
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
        var progress = 0;
        Console.WriteLine($"Starting the process, {Payload.ReportStartDate} - {Payload.ReportEndDate} | {Payload.ReportType}");
        await PublishProgress("Starting the process", progress, cancellationToken);
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine("Processing the data");

        progress = 10;
        await PublishProgress("Processing the data", progress, cancellationToken);
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine($"Processing progress: {progress}%");

        progress = 50;
        await PublishProgress("Processing the data", progress, cancellationToken);
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine($"Processing progress: {progress}%");

        progress = 90;
        await PublishProgress("Processing the data", progress, cancellationToken);
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine($"Processing progress: {progress}%");

        progress = 100;
        await PublishProgress("Process completed", progress, cancellationToken);
        await Task.Delay(5000, cancellationToken);
        Console.WriteLine($"Processing progress: {progress}%");
        Console.WriteLine("Process completed");
    }
}
