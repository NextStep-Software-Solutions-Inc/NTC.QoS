using MediatR;
using NTC.QoS.Application.Features.Cells.Requests;
using NTC.QoS.Domain.Aggregates;
using NTC.QoS.Domain.Enums;
using NTC.QoS.Domain.Helper;
using NTC.QoS.Domain.Interfaces;
using NTC.QoS.Domain.Models.DTOs;
using NTC.QoS.Domain.Models.DTOs.Cell;
using System.Reflection;
using NTC.QoS.Domain.Models.DTOs.Zone;
using System.Linq;
using System.Linq.Expressions;

namespace NTC.QoS.Application.Features.Cells.Handlers.Queries;

public class GetAllCellsHandler(ICellRepository repository) : IRequestHandler<GetAllCellsRequest, PaginatedResponse<CellDto>>
{
    private readonly ICellRepository _repository = repository;

    public async Task<PaginatedResponse<CellDto>> Handle(GetAllCellsRequest request, CancellationToken cancellationToken)
    {
        IQueryable<Cell> cellQuery = _repository.Query(c => !c.IsDeleted);

        if (!string.IsNullOrEmpty(request.Search))
        {
            var searchExpression = BuildNullableKeywordSearchExpression<Cell>(
               request.Search,
               c => c.Name,
               c => c.Description,
               c => c.Group,
               c => c.Zone!.Name,
               c => c.Zone!.Barangay!.Name,
               c => c.Zone!.Barangay!.Municipality!.Name,
               c => c.Zone!.Barangay!.Municipality!.Province!.Name,
               c => c.Zone!.Barangay!.Municipality!.Province!.Region!.Name
           );
            cellQuery = cellQuery.Where(searchExpression);

        }
        cellQuery = cellQuery
        .Where(c => string.IsNullOrEmpty(request.Status) || c.Status == EnumHelper<CellStatusEnum>.ParseUserInput(request.Status))
        .Where(c => !request.FileId.HasValue || c.FileId == request.FileId)
        .Where(c => !request.ZoneId.HasValue || c.ZoneId == request.ZoneId)
        .Where(c => !request.ServiceProviderId.HasValue || c.ServiceProviderId == request.ServiceProviderId)
        .Where(cell => request.ProviderId == null || cell.ServiceProvider!.ProviderId == request.ProviderId)
        .Where(cell => request.ServiceId == null || cell.ServiceProvider!.ServiceId == request.ServiceId);


        var cellDtoQuery = cellQuery.Select(cell => new CellDto
        {
            Id = cell.Id,
            Name = cell.Name,
            Group = cell.Group,
            //File = cell.File != null ? new()
            //{
            //    Id = cell.File.Id,
            //    Name = cell.File.Name,
            //    Path = cell.File.Path
            //} : null,
            Description = cell.Description ?? string.Empty,
            Zone = cell.Zone != null  ? new ZoneDto
            {
                Id = cell.Zone.Id,
                Name = cell.Zone.Name ?? string.Empty,
                Barangay = cell.Zone.Barangay != null ? new ()
                {
                       Id = cell.Zone.Barangay.Id,
                       Name = cell.Zone.Barangay.Name,
                       Description = cell.Zone.Barangay.Description
                }: null
            } : null,
            ServiceProvider = cell.ServiceProvider != null ? new()
            {
                Id = cell.ServiceProviderId,
                Service = cell.ServiceProvider.Service != null ? new()
                {
                    Id = cell.ServiceProvider.ServiceId,
                    Name = cell.ServiceProvider.Service.Name ?? string.Empty
                } : null,
                Provider = cell.ServiceProvider.Provider != null ? new()
                {
                    Id = cell.ServiceProvider.ProviderId,
                    Name = cell.ServiceProvider.Provider.Name ?? string.Empty,
                    Color = cell.ServiceProvider.Provider.Color ?? string.Empty
                } : null,
            }
            : null,
            CID = cell.CID,
            LAC = cell.LAC,
            TAC = cell.TAC,
            SiteId = cell.SiteId,
            TowerType = cell.TowerType,
            Elevation = cell.Elevation,
            CoorLatitude = cell.CoorLatitude,
            CoorLongitude = cell.CoorLongitude,
            IsVerified = cell.IsVerified,
            Status = EnumHelper<CellStatusEnum>.GetName(cell.Status)
        });


        var cells = await _repository.ToListAsync(cellDtoQuery, cancellationToken);

        if (cells.Count is 0)
        {
            return new PaginatedResponse<CellDto>(new List<CellDto>(), request.PageNumber, request.PageSize)
            {
                Message = "No records found.",
                Succeeded = false,
                Errors = new Dictionary<string, string[]>
                {
                    {
                        "NullCells", ["No cells found."]
                    }
                }
            };
        }
        return new PaginatedResponse<CellDto>(cells, request.PageNumber, request.PageSize)
        {
            Data = cells,
            Message = "Cells retrieved successfully.",
            Succeeded = true
        };

    }

    /// <summary>
    /// Builds a LINQ expression for searching a keyword across multiple nullable string properties of a given type.
    /// </summary>
    /// <typeparam name="T">The type of the entity.</typeparam>
    /// <param name="keyword">The keyword to search for.</param>
    /// <param name="propertySelectors">An array of expressions that select the nullable string properties to search in.</param>
    /// <returns>An expression that can be used in a Where clause.</returns>
    public static Expression<Func<T, bool>> BuildNullableKeywordSearchExpression<T>(string keyword, params Expression<Func<T, string?>>[] propertySelectors)
    {
        if (string.IsNullOrEmpty(keyword) || propertySelectors == null || propertySelectors.Length == 0)
        {
            return x => true; // Return a predicate that always evaluates to true (no filter).
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        Expression? combinedExpression = null;

        foreach (var propertySelector in propertySelectors)
        {
            var property = Expression.Invoke(propertySelector, parameter);

            // Check for null before calling Contains to avoid NullReferenceException
            var nullCheck = Expression.NotEqual(property, Expression.Constant(null, typeof(string)));

            // Get the Contains method info
            var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) }) ?? throw new InvalidOperationException("The 'Contains' method was not found on type 'string'.");

            // Convert property value and keyword to lower case for case-insensitive search
            var toLowerMethod = typeof(string).GetMethod("ToLower", Type.EmptyTypes) ?? throw new InvalidOperationException("The 'ToLower' method was not found on type 'string'.");
            var lowerProperty = Expression.Call(property, toLowerMethod);
            var lowerKeyword = keyword.ToLower();

            // Create the Contains expression.
            var containsExpression = Expression.Call(lowerProperty, containsMethod, Expression.Constant(lowerKeyword));
            // Combine null check and contains with AND.
            var safeContains = Expression.AndAlso(nullCheck, containsExpression);

            // Combine the current property's expression with the overall expression using OR.
            combinedExpression = combinedExpression == null ? safeContains : Expression.OrElse(combinedExpression, safeContains);
        }

        return Expression.Lambda<Func<T, bool>>(combinedExpression ?? Expression.Constant(true), parameter);
    }

}
