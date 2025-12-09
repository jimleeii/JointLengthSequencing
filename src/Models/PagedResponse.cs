namespace JointLengthSequencing.Models;

/// <summary>
/// Represents a paginated response containing a subset of results.
/// </summary>
/// <typeparam name="T">The type of items in the response.</typeparam>
public class PagedResponse<T>
{
    /// <summary>
    /// Gets or sets the current page number (1-based).
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Gets or sets the number of items per page.
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Gets or sets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Gets or sets the total number of pages.
    /// </summary>
    public int TotalPages { get; set; }

    /// <summary>
    /// Gets or sets whether there is a previous page.
    /// </summary>
    public bool HasPrevious { get; set; }

    /// <summary>
    /// Gets or sets whether there is a next page.
    /// </summary>
    public bool HasNext { get; set; }

    /// <summary>
    /// Gets or sets the items for the current page.
    /// </summary>
    public required IEnumerable<T> Items { get; set; }

    /// <summary>
    /// Creates a paginated response from a full list of items.
    /// </summary>
    /// <param name="items">The full list of items.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated response.</returns>
    public static PagedResponse<T> Create(IEnumerable<T> items, int pageNumber, int pageSize)
    {
        var itemsList = items.ToList();
        var totalCount = itemsList.Count;
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var pagedItems = itemsList
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);

        return new PagedResponse<T>
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPrevious = pageNumber > 1,
            HasNext = pageNumber < totalPages,
            Items = pagedItems
        };
    }
}
