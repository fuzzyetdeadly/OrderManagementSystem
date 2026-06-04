using ErrorOr;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.API.Constants;
using OrderManagement.Application.Common;

namespace OrderManagement.API.Controllers;

public abstract class ApiControllerBase : ControllerBase
{
    // Collection expression with '[...]' doesn't support
    // key indices like '[someKey]' in .NET 10. Require 'new()' 
    private static readonly Dictionary<string, string> _details = new()
    {
        [ErrorCodes.CustomerNotFound] = Errors.Customer.NotFound,
        [ErrorCodes.OrderNotFound] = Errors.Order.NotFound
    };

    // Helper override to construct problems from ErrorOn errors
    protected IActionResult Problem(List<Error> errors)
    {
        var firstError = errors.First();
        _ = _details.TryGetValue(firstError.Code, out var detail);
        var statusCode = firstError.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
                title: firstError.Code,
                detail: detail ?? Errors.General.UnexpectedError,
                statusCode: statusCode
            );
    }
}
