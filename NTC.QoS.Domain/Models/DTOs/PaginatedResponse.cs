using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Models.DTOs;

public class PaginatedResponse<T> : ApiResponse<List<T>>
{
    public int PageSize { get; private set; }
    public int PageNumber { get; private set; }
    public int TotalPageNumber { get; private set; }
    public bool LoadMore { get; private set; }
    public int TotalRows { get; set; }

    public PaginatedResponse(List<T> paginatedData, int pageNumber, int pageSize, int totalRows)
        : base(paginatedData, true, "")
    {
        TotalRows = totalRows;
        PageSize = pageSize;
        PageNumber = pageNumber;
        TotalPageNumber = (int)Math.Ceiling((double)TotalRows / PageSize);
        LoadMore = PageNumber < TotalPageNumber;

        var entityName = StringHelper.NormalizeText(typeof(T).Name);
        Message = TotalRows > 0 ? $"{entityName} records successfully retrieved." : $"No {entityName} record found.";
    }
}
