namespace StudentPortalAPI.DTOs;

/// <summary>
/// Generic paged result wrapper for any collection type.
/// Used by all paginated API endpoints.
/// </summary>
/// <typeparam name="T">The type of items in the collection.</typeparam>
public class PagedResult<T>
{
    /// <summary>The items for the current page.</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>Total number of matching records across all pages.</summary>
    public int TotalCount { get; set; }

    /// <summary>Current page number (1-based).</summary>
    public int Page { get; set; }

    /// <summary>Number of items per page.</summary>
    public int PageSize { get; set; }

    /// <summary>Total number of pages.</summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;

    /// <summary>Whether there is a next page available.</summary>
    public bool HasNextPage => Page < TotalPages;

    /// <summary>Whether there is a previous page available.</summary>
    public bool HasPreviousPage => Page > 1;
}
