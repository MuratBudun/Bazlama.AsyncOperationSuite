namespace Bazlama.AsyncOperationSuite.Dto;

public record OperationListDto
{
	public required int TotalCount { get; set; }
	public required int PageNumber { get; set; }
	public required int PageSize { get; set; }
//	public required List<OperationDto> Items { get; set; }
}

