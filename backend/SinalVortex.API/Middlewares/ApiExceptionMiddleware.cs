using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SinalVortex.Domain.Exceptions;

namespace SinalVortex.API.Middlewares;

public sealed class ApiExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (ValidationException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Dados inválidos.",
                Detail = string.Join(" ", exception.Errors.Select(x => x.ErrorMessage)),
                Type = "https://httpstatuses.com/400"
            });
        }
        catch (DomainException exception)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Regra de domínio violada.",
                Detail = exception.Message,
                Type = "https://httpstatuses.com/400"
            });
        }
        catch (KeyNotFoundException exception)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Recurso não encontrado.",
                Detail = exception.Message,
                Type = "https://httpstatuses.com/404"
            });
        }
    }
}
