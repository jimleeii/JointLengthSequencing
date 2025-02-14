using System.Text.Json.Serialization;

namespace JointLengthSequencing.Models;

/// <summary>
/// Represents a joint in a skeleton, including its original index in the dataset and its length.
/// </summary>
public record Joint
{
	/// <summary>
	/// Gets or sets the original index of the joint in the dataset.
	/// </summary>
	/// <value>The original index.</value>
	public int OriginalIndex { get; set; }

	/// <summary>
	/// Gets or sets the length of the joint.
	/// </summary>
	/// <value>The length.</value>
	[JsonPropertyName("length")]
	public double Length { get; set; }
}