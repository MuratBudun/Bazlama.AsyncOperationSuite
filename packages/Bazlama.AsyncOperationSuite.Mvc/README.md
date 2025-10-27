# Bazlama.AsyncOperationSuite.Mvc

ASP.NET Core MVC extension for Bazlama.AsyncOperationSuite that provides ready-to-use REST API controllers and endpoints for managing asynchronous operations. This package makes it incredibly easy to add a complete operation management API to your web application with just a few lines of code.

## Overview

The MVC extension package provides comprehensive REST API endpoints with built-in Swagger documentation, making it simple to integrate async operation management into your ASP.NET Core applications. No need to write controllers or API endpoints manually - everything is ready to use out of the box.

## Key Features

- **Ready-to-Use API Controllers**: Pre-built controllers for all operation management needs
- **Flexible Registration**: Add all controllers at once or register them selectively
- **Automatic Swagger Integration**: Built-in OpenAPI documentation
- **Authorization Support**: Optional JWT/policy-based authorization
- **Custom Route Prefixes**: Configurable API endpoint paths
- **JSON Schema Support**: Automatic payload type schema generation
- **Real-time Operation Monitoring**: Query active operations and progress
- **Operation Cancellation**: Cancel running operations via API

## Installation

Install via NuGet Package Manager:

```bash
dotnet add package Bazlama.AsyncOperationSuite.Mvc
```

Or via Package Manager Console:

```powershell
Install-Package Bazlama.AsyncOperationSuite.Mvc
```

## Quick Start

### Basic Setup

Add all controllers to your application with a single method call:

```csharp
using Bazlama.AsyncOperationSuite.Extensions;
using Bazlama.AsyncOperationSuite.Storage.MemoryStorage;
using Bazlama.AsyncOperationSuite.Mvc.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add AsyncOperationSuite services
builder.Services.AddAsyncOperationSuiteMemoryStorage();
builder.Services.AddAsyncOperationSuiteService(builder.Configuration);

// Add all MVC controllers
builder.Services.AddControllers();
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: false);

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

That's it! Your API is ready with all operation management endpoints.

### Selective Controller Registration

Register only the controllers you need:

```csharp
// Only operation publishing endpoints
builder.Services.AddAsyncOperationSuiteMvcOperationPublish(requireAuthorization: false);

// Only operation query endpoints
builder.Services.AddAsyncOperationSuiteMvcOperationQuery(requireAuthorization: false);

// Only payload type endpoints
builder.Services.AddAsyncOperationSuiteMvcOperationPayload(requireAuthorization: false);
```

### Custom Route Prefix

Customize the API route prefix:

```csharp
builder.Services.AddAsyncOperationSuiteMvcAllControllers(
    requireAuthorization: false,
    prefix: "api/operations"
);

// Endpoints will be available at: /api/operations/publish, /api/operations/query, etc.
```

## API Endpoints

The MVC extension provides three main controllers with comprehensive endpoints:

### Operation Publishing Controller

**Base Route**: `/api/aos/publish`

- `POST /api/aos/publish` - Publish a new operation
  - Query parameters: `payloadType`, `operationName`, `operationDescription`, `waitForQueueSpace`, `waitForPayloadSlotSpace`
  - Body: JSON payload data
  - Returns: Published operation payload with operation ID

- `POST /api/aos/publish/cancel/{operationId}` - Cancel a running operation
  - Query parameters: `useThrowIfCancellationRequested`, `waitForCompletion`, `timeoutMs`
  - Returns: 200 OK on success, 404 if not found

### Operation Query Controller

**Base Route**: `/api/aos/query`

- `GET /api/aos/query/engine` - Get engine information and statistics
  - Returns: Engine info (worker count, queue size, active operations)

- `GET /api/aos/query/active` - Get all active operations
  - Returns: List of currently running operations

- `GET /api/aos/query/operations` - Query operations with filters
  - Query parameters: `startDate`, `endDate`, `status`, `ownerId`, `search`, `isDesc`, `pageNumber`, `pageSize`
  - Returns: Paginated list of operations

- `GET /api/aos/query/operation/{operationId}` - Get operation by ID
  - Returns: Operation details

- `GET /api/aos/query/operation/{operationId}/payload` - Get operation payload
  - Returns: Operation payload data

- `GET /api/aos/query/operation/{operationId}/result` - Get operation result
  - Returns: Operation result

- `GET /api/aos/query/operation/{operationId}/progress` - Get latest operation progress
  - Returns: Latest progress update

- `GET /api/aos/query/operation/{operationId}/progress/all` - Get all operation progress updates
  - Returns: Complete progress history

- `GET /api/aos/query/payload/{payloadId}` - Get payload by ID
  - Returns: Payload data

- `GET /api/aos/query/payload/{payloadId}/progress` - Get payload progress
  - Returns: Latest progress for payload

- `GET /api/aos/query/payload/{payloadId}/progress/all` - Get all payload progress
  - Returns: Complete progress history for payload

- `GET /api/aos/query/result/{resultId}` - Get result by ID
  - Returns: Result data

### Payload Controller

**Base Route**: `/api/aos/payload`

- `GET /api/aos/payload` - Get all registered payload types
  - Returns: Dictionary of payload and operation type mappings

- `GET /api/aos/payload/type` - Get all payload types with JSON schemas
  - Returns: Dictionary of payload types with their JSON schemas

- `GET /api/aos/payload/type/{name}` - Get specific payload type schema
  - Returns: JSON schema for the specified payload type

## Usage Examples

### Publishing an Operation

```bash
POST /api/aos/publish?payloadType=DelayOperationPayload
Content-Type: application/json

{
  "Name": "My Test Operation",
  "Description": "Testing the async operation",
  "DelaySeconds": 5,
  "StepCount": 10
}
```

Response:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "operationId": "op-123456",
  "name": "My Test Operation",
  "description": "Testing the async operation",
  "delaySeconds": 5,
  "stepCount": 10,
  "createdAt": "2025-10-27T10:30:00Z"
}
```

### Querying Operations

```bash
GET /api/aos/query/operations?status=Running&status=Completed&pageSize=20&pageNumber=1
```

Response:
```json
[
  {
    "id": "op-123456",
    "status": "Running",
    "payloadId": "550e8400-e29b-41d4-a716-446655440000",
    "startedAt": "2025-10-27T10:30:00Z",
    "progress": 45
  }
]
```

### Getting Active Operations

```bash
GET /api/aos/query/active
```

Response:
```json
{
  "activeOperations": 3,
  "operations": [
    {
      "operationId": "op-123456",
      "payloadType": "DelayOperationPayload",
      "status": "Running",
      "progress": 45
    }
  ]
}
```

### Getting Payload Types

```bash
GET /api/aos/payload/type
```

Response:
```json
{
  "DelayOperationPayload": {
    "type": "object",
    "properties": {
      "delaySeconds": { "type": "integer" },
      "stepCount": { "type": "integer" },
      "name": { "type": "string" },
      "description": { "type": "string" }
    }
  }
}
```

### Canceling an Operation

```bash
POST /api/aos/publish/cancel/op-123456?waitForCompletion=true&timeoutMs=5000
```

## Authorization

The MVC extension supports flexible authorization configurations:

### No Authorization (Development)

```csharp
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: false);
```

### JWT Authentication (Production)

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Configure JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

// Require authorization for all controllers
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: true);

app.UseAuthentication();
app.UseAuthorization();
```

### Policy-Based Authorization

```csharp
// Define policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("OperationAdmin", policy =>
        policy.RequireRole("Admin", "OperationManager"));
});

// Apply policy to controllers
builder.Services.AddAsyncOperationSuiteMvcOperationPublish(
    requireAuthorization: true,
    policyName: "OperationAdmin"
);

builder.Services.AddAsyncOperationSuiteMvcOperationQuery(
    requireAuthorization: false
);
```

### Selective Authorization

```csharp
// Public read access, protected write access
builder.Services.AddAsyncOperationSuiteMvcOperationQuery(requireAuthorization: false);
builder.Services.AddAsyncOperationSuiteMvcOperationPayload(requireAuthorization: false);
builder.Services.AddAsyncOperationSuiteMvcOperationPublish(requireAuthorization: true);
```

## Swagger Integration

The MVC extension works seamlessly with Swagger/OpenAPI:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Async Operation Suite API",
        Version = "v1",
        Description = "API for managing asynchronous operations"
    });

    // Add JWT authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Async Operation Suite API v1");
    options.RoutePrefix = "swagger";
});
```

Navigate to `/swagger` to access the interactive API documentation.

## Configuration

### Extension Method Options

All registration methods support the following parameters:

- `requireAuthorization` (bool): Enable authorization for the controllers (default: false)
- `policyName` (string?): Optional authorization policy name
- `prefix` (string): API route prefix (default: "api/aos")

### Available Registration Methods

```csharp
// Register all controllers
AddAsyncOperationSuiteMvcAllControllers(requireAuthorization, policyName, prefix)

// Register publish controller only
AddAsyncOperationSuiteMvcOperationPublish(requireAuthorization, policyName, prefix)

// Register query controller only
AddAsyncOperationSuiteMvcOperationQuery(requireAuthorization, policyName, prefix)

// Register payload controller only
AddAsyncOperationSuiteMvcOperationPayload(requireAuthorization, policyName, prefix)
```

## CORS Configuration

For frontend integration, configure CORS:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowFrontend");
```

## Frontend Integration

The API works seamlessly with any frontend framework. Example using JavaScript/TypeScript:

```typescript
// Publish an operation
const response = await fetch('/api/aos/publish?payloadType=DelayOperationPayload', {
    method: 'POST',
    headers: {
        'Content-Type': 'application/json',
        'Authorization': 'Bearer YOUR_JWT_TOKEN'
    },
    body: JSON.stringify({
        Name: 'My Operation',
        DelaySeconds: 5,
        StepCount: 10
    })
});

const result = await response.json();
console.log('Operation ID:', result.operationId);

// Poll for progress
const progressResponse = await fetch(`/api/aos/query/operation/${result.operationId}/progress`);
const progress = await progressResponse.json();
console.log('Progress:', progress.percentage);
```

## Error Handling

The API returns standard HTTP status codes:

- `200 OK`: Successful operation
- `400 Bad Request`: Invalid payload or parameters
- `404 Not Found`: Operation/payload/result not found
- `408 Request Timeout`: Operation cancellation timeout
- `429 Too Many Requests`: Queue full or payload limit exceeded
- `500 Internal Server Error`: Server error

Example error response:
```json
{
    "message": "Queue is full. Please try again later."
}
```

## Best Practices

### Production Deployment

1. **Enable Authorization**: Always require authorization in production
```csharp
builder.Services.AddAsyncOperationSuiteMvcAllControllers(requireAuthorization: true);
```

2. **Use HTTPS**: Ensure SSL/TLS is configured
```csharp
app.UseHttpsRedirection();
```

3. **Configure Rate Limiting**: Protect against abuse
```csharp
builder.Services.AddRateLimiter(options => { /* configuration */ });
```

4. **Enable Logging**: Monitor API usage
```json
{
  "Logging": {
    "LogLevel": {
      "Bazlama.AsyncOperationSuite.Mvc": "Information"
    }
  }
}
```

### Performance Optimization

1. **Use Pagination**: Always use pagination for operation queries
2. **Filter Results**: Use date ranges and status filters
3. **Cache Payload Types**: Cache the payload type schemas on the frontend
4. **Use SQL Storage**: For production, use SQL Server storage instead of memory

## Sample Application

The repository includes a complete sample application demonstrating all features:

```bash
cd sample/api
dotnet run
```

Visit `https://localhost:5292/swagger` to explore the API.

## Troubleshooting

### Controllers Not Registered

Make sure you've called `AddControllers()` before adding AsyncOperationSuite controllers:
```csharp
builder.Services.AddControllers();
builder.Services.AddAsyncOperationSuiteMvcAllControllers();
```

### Routes Not Working

Ensure `MapControllers()` is called:
```csharp
app.MapControllers();
```

### Authorization Issues

Check that authentication middleware is added before authorization:
```csharp
app.UseAuthentication();
app.UseAuthorization();
```

### Swagger Not Showing Endpoints

Make sure controllers are registered before Swagger:
```csharp
builder.Services.AddControllers();
builder.Services.AddAsyncOperationSuiteMvcAllControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
```

## Requirements

- .NET 8.0 or later
- ASP.NET Core 8.0 or later
- Bazlama.AsyncOperationSuite (core package)

## Dependencies

- Microsoft.AspNetCore.App framework
- NJsonSchema (for JSON schema generation)
- Bazlama.AsyncOperationSuite (core package)

## Links

- [GitHub Repository](https://github.com/MuratBudun/Bazlama.AsyncOperationSuite)
- [Core Package](https://www.nuget.org/packages/Bazlama.AsyncOperationSuite)
- [Sample Application](https://github.com/MuratBudun/Bazlama.AsyncOperationSuite/tree/main/sample)

## License

This project is licensed under the MIT License.

## Author

Murat Budun - [GitHub](https://github.com/MuratBudun)