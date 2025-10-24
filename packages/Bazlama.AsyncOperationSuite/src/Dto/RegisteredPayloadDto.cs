namespace Bazlama.AsyncOperationSuite.Dto;

public record RegisteredPayloadDto
{
    public required string PayloadTypeName { get; init; }
    public required string ProcessTypeName { get; init; }
}
