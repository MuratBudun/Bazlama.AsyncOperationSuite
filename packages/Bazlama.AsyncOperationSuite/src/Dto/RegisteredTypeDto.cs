namespace Bazlama.AsyncOperationSuite.Dto;

public record RegisteredTypeDto
{
    public required string PayloadTypeName { get; init; }
    public required string ProcessTypeName { get; init; }
}
