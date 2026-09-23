using GameAnalytics.Models;

namespace GameAnalytics.DTOs;

public record CreateParameterRequest(
	string Name,
	ParameterType ParameterType,
	DataType DataType,
	Guid? EventModelId
);

public record ParameterResponse(
	Guid Id,
	string Name,
	ParameterType ParameterType,
	DataType DataType,
	Guid ProjectId,
	Guid? EventModelId
);
