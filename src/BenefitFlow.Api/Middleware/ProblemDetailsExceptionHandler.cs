using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BenefitFlow.Api.Middleware;

public static partial class ProblemDetailsExceptionHandler
{
    public static IApplicationBuilder UseProblemDetailsExceptionHandler(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseExceptionHandler(errorHandler =>
            errorHandler.Run(async context =>
            {
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                if (exception is null)
                {
                    return;
                }

                var logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("BenefitFlow.Api.ExceptionHandler");
                LogUnhandledRequestError(logger, exception, context.Request.Path.Value ?? string.Empty);

                var validationException = exception as ValidationException;
                var statusCode = validationException is null
                    ? StatusCodes.Status500InternalServerError
                    : StatusCodes.Status400BadRequest;
                var problem = new ProblemDetails
                {
                    Status = statusCode,
                    Title = validationException is null ? "An unexpected error occurred." : "Request validation failed.",
                    Detail = validationException is null
                        ? "The request could not be completed."
                        : string.Join("; ", validationException.Errors.Select(error => error.ErrorMessage))
                };

                context.Response.StatusCode = statusCode;
                await context.Response.WriteAsJsonAsync(problem);
            }));
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled request error for {Path}")]
    private static partial void LogUnhandledRequestError(ILogger logger, Exception exception, string path);
}
