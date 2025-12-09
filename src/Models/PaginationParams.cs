namespace JointLengthSequencing.Models;

/// <summary>
/// Represents pagination parameters for requests.
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    /// <summary>
    /// Gets or sets the page number (1-based). Defaults to 1.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Gets or sets the page size. Defaults to 20, maximum 100.
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Validates the pagination parameters.
    /// </summary>
    /// <param name="errors">List to collect validation errors.</param>
    /// <returns>True if valid, false otherwise.</returns>
    public bool Validate(ref List<string>? errors)
    {
        errors ??= [];

        if (PageNumber < 1)
        {
            errors.Add("PageNumber must be greater than 0.");
        }

        if (PageSize < 1)
        {
            errors.Add("PageSize must be greater than 0.");
        }

        if (PageSize > MaxPageSize)
        {
            errors.Add($"PageSize cannot exceed {MaxPageSize}.");
        }

        return errors.Count == 0;
    }
}
