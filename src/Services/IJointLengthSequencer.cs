namespace JointLengthSequencing.Services;

/// <summary>
/// Defines the contract for a service that sequences two datasets
/// of joints based on their lengths.
/// </summary>
public interface IJointLengthSequencer
{
    /// <summary>
    /// Calculates a list of matches between two datasets of joints
    /// based on their lengths.
    /// </summary>
    /// <param name="baseData">The first dataset of joints.</param>
    /// <param name="targetData">The second dataset of joints.</param>
    /// <param name="pivotPercentile">
    /// The percentage of joints to consider as pivots.
    /// </param>
    /// <param name="tolerance">
    /// The maximum difference in length that is allowed for two
    /// joints to be considered a match.
    /// </param>
    /// <param name="pivotRequired">
    /// The minimum number of pivot joints that must match for the
    /// two datasets to be considered a match.
    /// </param>
    /// <param name="baseLengthCol">
    /// The name of the column containing the lengths of the joints in
    /// the first dataset.
    /// </param>
    /// <param name="targetLengthCol">
    /// The name of the column containing the lengths of the joints in
    /// the second dataset.
    /// </param>
    /// <returns>A list of matches between the two datasets.</returns>
    Task<List<JointMatchResult>> CalculateMatches(
        List<Dictionary<string, object>> baseData,
        List<Dictionary<string, object>> targetData,
        double pivotPercentile = 0.1,
        double tolerance = 1.5,
        int pivotRequired = 10,
        string baseLengthCol = "length",
        string targetLengthCol = "length");
}
