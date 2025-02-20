
namespace NTC.QoS.Domain.Models.DTOs;

public class PaginatedResponse<T> : ApiResponse<List<T>>
{
    public int PageSize { get; private set; }
    public int PageNumber { get; private set; }
    public int TotalPageNumber { get; private set; }
    public bool LoadMore { get; private set; }
    public int TotalRows { get; private set; }


    public PaginatedResponse(List<T> paginatedData, int pageNumber, int pageSize, int totalRows)
        : base(paginatedData, true, "")
    {
        TotalRows = totalRows;
        PageSize = pageSize;
        PageNumber = pageNumber;
        TotalPageNumber = (int)Math.Ceiling((double)TotalRows / PageSize);
        LoadMore = PageNumber < TotalPageNumber;
    }

    public PaginatedResponse(int pageNumber, int pageSize, int totalRows)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
        TotalRows = totalRows;
    }

    public PaginatedResponse(List<Entities.Provider> allProviders, int pageNumber, int pageSize)
    {
        PageNumber = pageNumber;
        PageSize = pageSize;
    }
}
