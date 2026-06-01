using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using OrderManagement.API.Constants;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderManagement.API.Extensions;

public static class ControllerExtensions
{
    public static IServiceCollection AddCustomControllers(this IServiceCollection services)
    {
        services.AddControllers()
            .AddJsonOptions(options =>
            {
                // Make API accept strings for enums, do not allow integers (prevent bad casts)
                options.JsonSerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(allowIntegerValues: false));
            })
            .ConfigureApiBehaviorOptions(options =>
            {
                /* 
                 * Configure return of clean model state errors
                 * e.g. Attempting to cast an invalid enum string.
                 * Note: this is a delegate property.
                 * When ASP.NET Core detects invalid model state,
                 * it calls whatever functions is here automatically.
                 */
                options.InvalidModelStateResponseFactory = context =>
                {
                    // Use ASP.NET's problem details factory to reconstruct the details
                    var factory = context.HttpContext.RequestServices
                            .GetRequiredService<ProblemDetailsFactory>();

                    var problemDetails = factory.CreateValidationProblemDetails(
                            context.HttpContext,
                            context.ModelState,
                            StatusCodes.Status400BadRequest
                        );

                    // Post-process error messages to sanitize away project structure leaks
                    foreach (var key in problemDetails.Errors.Keys.ToList())
                    {
                        problemDetails.Errors[key] = [.. problemDetails.Errors[key]
                            .Select(msg => msg.Contains("System.") | msg.Contains("Path:")
                                ? ErrorMessages.Validation.InvalidValue : msg)];
                    }

                    return new BadRequestObjectResult(problemDetails);
                };
            });

        return services;
    }
}
