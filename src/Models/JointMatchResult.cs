namespace JointLengthSequencing.Models;

/// <summary>
/// Represents a single match result between a joint in the base dataset
/// and a joint in the target dataset.
/// </summary>
public class JointMatchResult
{
    /// <summary>
    /// The index of the joint in the base dataset.
    /// </summary>
    public int BaseIndex { get; set; }

    /// <summary>
    /// The index of the joint in the target dataset.
    /// </summary>
    public int TargetIndex { get; set; }
}
