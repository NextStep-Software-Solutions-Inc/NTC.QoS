using FluentValidation;
using MediatR;
using NTC.QoS.Domain.Models.DTOs;

namespace NTC.QoS.Application.Behaviors;
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);
            var validationResults = await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)));
            var failures = validationResults.SelectMany(r => r.Errors).Where(f => f != null).ToList();

            if (failures.Count != 0)
            {
                var errors = failures.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                // Create an error response of the correct type.
                if (typeof(TResponse).GetGenericArguments().Length > 0)
                {
                    var genericType = typeof(TResponse).GetGenericArguments()[0];
                    var apiResponseType = typeof(ApiResponse<>).MakeGenericType(genericType);
                    var errorResponse = (TResponse?)Activator.CreateInstance(apiResponseType, false, null, "Validation failed", errors)
                        ?? throw new InvalidOperationException("Failed to create error response instance.");
                    return errorResponse;
                }
                else
                {
                    // Fallback to ApiResponse<object> if TResponse is not a generic ApiResponse.
                    var errorResponse = (TResponse)(object)Task.FromResult(new ApiResponse<object>
                    {
                        Data = null,
                        Succeeded = false,
                        Message = "Validation failed",
                        Errors = errors,
                    }).Result;
                    return errorResponse;
                }
            }
        }
        return await next();
    }
}
