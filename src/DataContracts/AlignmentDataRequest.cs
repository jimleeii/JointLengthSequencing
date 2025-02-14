namespace JointLengthSequencing.DataContracts;

/// <summary>
/// Represents a request for alignment data, containing the necessary parameters and datasets.
/// </summary>
public struct AlignmentDataRequest
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AlignmentDataRequest"/> struct with default values.
    /// </summary>
    public AlignmentDataRequest()
    {
        PivotPercentile = 0.1;
        Tolerance = 1.5;
        PivotRequired = 10;
        BaseLengthCol = "length";
        TargetLengthCol = "length";
    }

    /// <summary>
    /// Gets or sets the base data, a list of dictionaries representing the dataset.
    /// </summary>
    public required List<Dictionary<string, object>> BaseData { get; set; }

    /// <summary>
    /// Gets or sets the target data, a list of dictionaries representing the dataset.
    /// </summary>
    public required List<Dictionary<string, object>> TargetData { get; set; }

    /// <summary>
    /// Gets or sets the pivot percentile value used for pivot selection.
    /// </summary>
    public double PivotPercentile { get; set; }

    /// <summary>
    /// Gets or sets the tolerance value for alignment calculations.
    /// </summary>
    public double Tolerance { get; set; }

    /// <summary>
    /// Gets or sets the number of pivots required for alignment.
    /// </summary>
    public int PivotRequired { get; set; }

    /// <summary>
    /// Gets or sets the column name for lengths in the base data.
    /// </summary>
    public string BaseLengthCol { get; set; }

    /// <summary>
    /// Gets or sets the column name for lengths in the target data.
    /// </summary>
    public string TargetLengthCol { get; set; }

    /// <summary>
    /// Validates the alignment data request to ensure all required properties are set and values are within acceptable ranges.
    /// </summary>
    /// <param name="errors">A list to store the error messages. Can be null.</param>
    /// <returns>True if the request is valid; otherwise, false.</returns>
    public readonly bool Validate(ref List<string>? errors)
    {
        errors ??= [];

        if (BaseData == null || BaseData.Count == 0)
        {
            errors.Add("BaseData is null or empty.");
        }
        if (TargetData == null || TargetData.Count == 0)
        {
            errors.Add("TargetData is null or empty.");
        }
        if (PivotPercentile <= 0 || PivotPercentile > 1)
        {
            errors.Add("PivotPercentile should be between 0 and 1.");
        }
        if (Tolerance <= 0)
        {
            errors.Add("Tolerance should be greater than 0.");
        }
        if (PivotRequired <= 0)
        {
            errors.Add("PivotRequired should be greater than 0.");
        }
        if (string.IsNullOrWhiteSpace(BaseLengthCol))
        {
            errors.Add("BaseLengthCol is null or whitespace.");
        }
        if (string.IsNullOrWhiteSpace(TargetLengthCol))
        {
            errors.Add("TargetLengthCol is null or whitespace.");
        }
        if (errors.Count > 0)
        {
            return false;
        }

        return true;
    }
}
