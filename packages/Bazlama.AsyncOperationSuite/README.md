# Bazlama.AsyncOperationSuite

A robust and scalable .NET library for managing and monitoring asynchronous operations with real-time progress tracking, result storage, and comprehensive operation management capabilities.

## Overview

Bazlama.AsyncOperationSuite provides a complete solution for handling long-running asynchronous operations in .NET applications. It offers a structured approach to manage background tasks with features like progress tracking, result storage, operation queuing, and real-time monitoring.

## Key Features

- **Asynchronous Operation Management**: Execute and monitor long-running background operations
- **Real-time Progress Tracking**: Track operation progress with detailed status updates
- **Multiple Storage Providers**: Support for Memory and SQL Server storage backends
- **Configurable Workers**: Adjustable worker threads and queue management
- **Payload Constraints**: Control concurrent operations per payload type
- **Operation Results**: Store and retrieve operation results with detailed metadata
- **Comprehensive Logging**: Built-in logging and monitoring capabilities

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Bazlama.AsyncOperationSuite
```

Or via Package Manager Console:

```powershell
Install-Package Bazlama.AsyncOperationSuite
```

## Quick Start

### 1. Configure Services

Add AsyncOperationSuite services to your application:

```csharp
using Bazlama.AsyncOperationSuite.Extensions;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage;

var builder = WebApplication.CreateBuilder(args);

// Add AsyncOperationSuite services with Memory Storage
builder.Services.AddAsyncOperationSuiteMemoryStorage();
builder.Services.AddAsyncOperationSuiteService(builder.Configuration);

var app = builder.Build();
app.Run();
```

### 2. Define Operation Payload

Create a payload class that inherits from `AsyncOperationPayloadBase`:

```csharp
public class DelayOperationPayload : AsyncOperationPayloadBase
{
    public int DelaySeconds { get; set; } = 1;
    public int StepCount { get; set; } = 15;
}
```

### 3. Implement Operation Processor

Create an operation processor that inherits from `AsyncOperationProcess<TPayload>`:

```csharp
public class DelayOperationProcessor : AsyncOperationProcess<DelayOperationPayload>
{
    public DelayOperationProcessor(
        DelayOperationPayload payload,
        AsyncOperation asyncOperation,
        AsyncOperationService asyncOperationService)
        : base(payload, asyncOperation, asyncOperationService)
    {
    }

    protected override async Task OnExecuteAsync(
        IServiceProvider serviceProvider,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        for (var i = 0; i < Payload.StepCount; i++)
        {
            var progress = (i + 1) * 100 / Payload.StepCount;
            await PublishProgress($"Step {i + 1} of {Payload.StepCount}", progress, cancellationToken);
            await Task.Delay(Payload.DelaySeconds * 1000, cancellationToken);
        }

        SetResult("Completed", $"Operation '{Payload.Name}' completed successfully.");
    }
}
```

### 4. Execute Operation

```csharp
var payload = new DelayOperationPayload
{
    Name = "My First Operation",
    Description = "Testing the async operation suite",
    DelaySeconds = 2,
    StepCount = 10
};

// The operation will be automatically queued and processed
```

## Configuration

### Basic Configuration

Configure AsyncOperationSuite in your `appsettings.json`:

```json
{
  "AsyncOperationSuiteConfiguration": {
    "WorkerCount": 5,
    "QueueSize": 1000,
    "PayloadConcurrentConstraints": {
      "DelayOperationPayload": 3,
      "ReportOperationPayload": 1
    }
  }
}
```

**Configuration Options:**
- `WorkerCount`: Number of concurrent worker threads (default: 5)
- `QueueSize`: Maximum size of the operation queue (default: 1000)
- `PayloadConcurrentConstraints`: Dictionary of payload type names and their maximum concurrent execution limits

### Storage Configuration

#### Memory Storage

Memory storage is ideal for development, testing, or applications that don't require persistence.

```csharp
// Register Memory Storage
builder.Services.AddAsyncOperationSuiteMemoryStorage(builder.Configuration);
```

Memory storage configuration in `appsettings.json`:

```json
{
  "AsyncOperationSuiteConfiguration": {
    "MemoryStorage": {
      "MaxOperations": 1000,
      "MaxPayloads": 1000,
      "MaxProgress": 5000,
      "MaxResults": 1000,
      "CleanupStrategy": "RemoveCompletedFirst",
      "CleanupBatchSize": 100,
      "EnableAutoCleanup": true,
      "CleanupThreshold": 0.9
    }
  }
}
```

**Memory Storage Options:**
- `MaxOperations`: Maximum operations to keep in memory (default: 100)
- `MaxPayloads`: Maximum payloads to keep in memory (default: 100)
- `MaxProgress`: Maximum progress records to keep in memory (default: 1000)
- `MaxResults`: Maximum results to keep in memory (default: 100)
- `CleanupStrategy`: Strategy when limit is reached
  - `RemoveOldest`: Remove oldest items first
  - `RemoveCompletedFirst`: Remove completed operations first
  - `RemoveFailedFirst`: Remove failed operations first
  - `ThrowException`: Throw exception when limit reached
- `CleanupBatchSize`: Number of items to remove during cleanup (0 = auto 10%)
- `EnableAutoCleanup`: Enable automatic cleanup (default: true)
- `CleanupThreshold`: Cleanup trigger percentage (default: 0.9 = 90%)

#### SQL Server Storage

SQL Server storage provides persistence and is suitable for production environments.

```csharp
using Bazlama.AsyncOperationSuite.Storage.MSSQLStorage;

// Register SQL Server Storage
builder.Services.AddAsyncOperationSuiteMSSQLStorage(builder.Configuration);
```

SQL Server storage configuration in `appsettings.json`:

```json
{
  "AsyncOperationSuiteConfiguration": {
    "MSSQLStorage": {
      "ConnectionString": "Data Source=localhost;Initial Catalog=AsyncOperationSuite;Integrated Security=true;TrustServerCertificate=True;",
      "CommandTimeout": 30,
      "EnableDetailedLogging": false,
      "MaxPoolSize": 100,
      "MinPoolSize": 5
    }
  }
}
```

**SQL Server Storage Options:**
- `ConnectionString`: SQL Server connection string (required)
- `CommandTimeout`: Command timeout in seconds (default: 30)
- `EnableDetailedLogging`: Enable detailed SQL logging (default: false)
- `MaxPoolSize`: Maximum connection pool size (default: 100)
- `MinPoolSize`: Minimum connection pool size (default: 5)

### Production Configuration Example

```json
{
  "AsyncOperationSuiteConfiguration": {
    "WorkerCount": 10,
    "QueueSize": 5000,
    "PayloadConcurrentConstraints": {
      "EmailOperationPayload": 5,
      "ReportGenerationPayload": 2,
      "DataImportPayload": 1,
      "BackupOperationPayload": 1
    },
    "MSSQLStorage": {
      "ConnectionString": "Data Source=prod-sql-server;Initial Catalog=AsyncOperationSuite;User ID=async_user;Password=your_secure_password;TrustServerCertificate=True;Connection Timeout=30;",
      "CommandTimeout": 60,
      "EnableDetailedLogging": false,
      "MaxPoolSize": 200,
      "MinPoolSize": 10
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Bazlama.AsyncOperationSuite": "Information"
    }
  }
}
```

## Database Schema (SQL Server)

When using SQL Server storage, the following tables will be created automatically:

- `AsyncOperations`: Stores operation metadata and status
- `AsyncOperationPayloads`: Stores operation payload data
- `AsyncOperationProgress`: Stores progress updates
- `AsyncOperationResults`: Stores operation results

## Storage Providers

### Memory Storage
- **Pros**: Ultra-fast operations, no network latency, ideal for development
- **Cons**: No persistence, limited by available RAM, data loss on restart
- **Use Cases**: Development, testing, temporary operations, caching scenarios

### SQL Server Storage
- **Pros**: Full persistence, ACID compliance, scalable, production-ready
- **Cons**: Network latency, requires database infrastructure
- **Use Cases**: Production environments, audit requirements, long-term storage

## Scaling Guidelines

### Worker Configuration

Adjust worker count based on CPU cores and workload:

```json
{
  "AsyncOperationSuiteConfiguration": {
    "WorkerCount": 10,
    "QueueSize": 5000
  }
}
```

### Payload Constraints

Control concurrent operations per type to prevent resource exhaustion:

```json
{
  "AsyncOperationSuiteConfiguration": {
    "PayloadConcurrentConstraints": {
      "CPUIntensiveOperation": 2,
      "IOIntensiveOperation": 10,
      "DatabaseOperation": 5,
      "EmailOperation": 20
    }
  }
}
```

## Best Practices

### Operation Design
- Keep operations idempotent when possible
- Implement proper cancellation token handling
- Use progress reporting for long-running operations
- Set meaningful operation names and descriptions

### Performance Optimization
- Configure worker count based on your CPU cores
- Set appropriate payload constraints
- Use SQL Server storage for production environments
- Monitor queue size and adjust accordingly

### Error Handling

```csharp
protected override async Task OnExecuteAsync(
    IServiceProvider serviceProvider,
    ILogger logger,
    CancellationToken cancellationToken)
{
    try
    {
        // Your operation logic
        await PublishProgress("Processing...", 50, cancellationToken);
        
        SetResult("Success", "Operation completed successfully");
    }
    catch (OperationCanceledException)
    {
        logger.LogWarning("Operation was cancelled");
        throw;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Operation failed");
        throw;
    }
}
```

## Troubleshooting

### Common Issues

**"Unable to resolve service for type 'AsyncOperationService'"**

Make sure you've registered the service:
```csharp
builder.Services.AddAsyncOperationSuiteService(builder.Configuration);
```

**Memory Storage Cleanup Issues**

Adjust cleanup configuration for your workload:
```json
{
  "AsyncOperationSuiteConfiguration": {
    "MemoryStorage": {
      "MaxOperations": 5000,
      "CleanupThreshold": 0.8,
      "CleanupStrategy": "RemoveCompletedFirst"
    }
  }
}
```

**SQL Server Connection Issues**

Verify your connection string and database permissions:
```json
{
  "AsyncOperationSuiteConfiguration": {
    "MSSQLStorage": {
      "ConnectionString": "...",
      "CommandTimeout": 60,
      "EnableDetailedLogging": true
    }
  }
}
```

## Web API Integration

The **Bazlama.AsyncOperationSuite.Mvc** extension package provides ready-to-use REST API endpoints and controllers, making it incredibly easy to integrate operation management into your web applications. With just a few lines of code, you get a complete API layer with built-in Swagger documentation.

### Quick Setup

Install the MVC extension package:

```bash
dotnet add package Bazlama.AsyncOperationSuite.Mvc
```

Add the controllers to your application:

```csharp
using Bazlama.AsyncOperationSuite.Mvc.Extensions;

builder.Services.AddControllers();
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: false);
```

### What You Get

The MVC extension provides comprehensive REST API endpoints out of the box:

- **Operation Publishing**: `POST /api/operation/publish` - Submit new operations
- **Operation Query**: `GET /api/operation/query` - Query operations with filtering
- **Active Operations**: `GET /api/operation/active` - Monitor running operations
- **Payload Types**: `GET /api/operation/payload` - Get registered operation types
- **Engine Info**: `GET /api/operation/engine-info` - System health and statistics
- **Swagger Documentation**: Interactive API documentation at `/swagger`

### Usage Example

```bash
# Publish a new operation
POST /api/operation/publish
Content-Type: application/json

{
  "payloadType": "DelayOperationPayload",
  "payload": {
    "Name": "My Operation",
    "Description": "Processing data",
    "DelaySeconds": 5,
    "StepCount": 10
  }
}

# Query operations
GET /api/operation/query?status=Running&pageSize=10

# Get active operations
GET /api/operation/active
```

For detailed information about the MVC extension, API endpoints, and frontend dashboard integration, see the [Bazlama.AsyncOperationSuite.Mvc](https://www.nuget.org/packages/Bazlama.AsyncOperationSuite.Mvc) package documentation.

## Requirements

- .NET 8.0 or later
- SQL Server (for SQL storage provider)

## Links

- [GitHub Repository](https://github.com/MuratBudun/Bazlama.AsyncOperationSuite)
- [MVC Extension Package](https://www.nuget.org/packages/Bazlama.AsyncOperationSuite.Mvc)
- [Sample Application](https://github.com/MuratBudun/Bazlama.AsyncOperationSuite/tree/main/sample)

## License

This project is licensed under the MIT License.

## Author

Murat Budun - [GitHub](https://github.com/MuratBudun)