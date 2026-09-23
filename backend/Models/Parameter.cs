using System.ComponentModel.DataAnnotations;

namespace GameAnalytics.Models;

public class Parameter
{
	public Guid Id { get; set; } = Guid.NewGuid();
	[Required, MaxLength(100)]
	public string Name { get; set; } = "";
	public ParameterType ParameterType { get; set; }
	public DataType DataType { get; set; }

	public Guid ProjectId { get; set; }
	public Project Project { get; set; } = null!;

	public Guid? EventModelId { get; set; }
	public EventModel? EventModel { get; set; }
}
