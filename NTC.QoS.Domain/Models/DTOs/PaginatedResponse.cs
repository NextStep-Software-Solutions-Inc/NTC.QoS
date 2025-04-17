using NTC.QoS.Domain.Helper;

namespace NTC.QoS.Domain.Models.DTOs;

public class PaginatedResponse<T> : ApiResponse<List<T>>
{
    public int PageSize { get; private set; }
    public int PageNumber { get; private set; }
    public int TotalPageNumber { get; private set; }
    public bool LoadMore { get; private set; }
    public int TotalRows { get; private set; }


    public PaginatedResponse(List<T> allData, int pageNumber, int pageSize)
        : base(allData.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), true, "")
    {
        TotalRows = allData.Count;
        PageSize = pageSize;
        PageNumber = pageNumber;
        TotalPageNumber = (int)Math.Ceiling((double)TotalRows / PageSize);
        LoadMore = PageNumber < TotalPageNumber;

        var entityName = StringHelper.NormalizeText(typeof(T).Name);

        Message = TotalRows > 0 ? $"{entityName} records successfully retreived." : $"No {entityName} record found.";
    }

   
}
