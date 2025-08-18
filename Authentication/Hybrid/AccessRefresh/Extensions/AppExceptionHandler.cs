using AccessRefresh.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace AccessRefresh.Extensions;

public static class AppExceptionHandler
{
    public static void MapExceptionsHandler(this WebApplication app)
    {
        app.UseExceptionHandler(appBuilder => {
            appBuilder.Run(async context => {
                var exceptionHandlerFeature = context.Features.Get<IExceptionHandlerFeature>();
                var exception = exceptionHandlerFeature?.Error;
                
                var statusCode = StatusCodes.Status500InternalServerError;
                var message = "An unexpected error occurred";

                if (exception is DomainException exc)
                {
                    message = exc.Message;
                    statusCode = (int)exc.StatusCode;
                }
                
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new {
                    error = message
                });
            });
        });
    }
}