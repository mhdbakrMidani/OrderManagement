using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.Exceptions;

namespace OrderManagement.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            await HandleExceptionAsync(context, exception);
        }
    }

    private static async Task HandleExceptionAsync(
        HttpContext context,
        Exception exception)
    {
        var statusCode = exception switch
        {
            NotFoundException => (int)HttpStatusCode.NotFound,
            ValidationException => (int)HttpStatusCode.BadRequest,
            ConflictException => (int)HttpStatusCode.Conflict,

            DbUpdateException dbException
                when IsUniqueConstraintViolation(dbException)
                => (int)HttpStatusCode.Conflict,

            _ => (int)HttpStatusCode.InternalServerError
        };

        context.Response.StatusCode = statusCode;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,

            Title = exception switch
            {
                NotFoundException => "Resource Not Found",
                ValidationException => "Validation Error",
                ConflictException => "Conflict",

                DbUpdateException dbException
                    when IsUniqueConstraintViolation(dbException)
                    => "Conflict",

                _ => "Internal Server Error"
            },

            Detail = exception switch
            {
                NotFoundException
                    => exception.Message,

                ValidationException
                    => exception.Message,

                ConflictException
                    => exception.Message,

                DbUpdateException dbException
                    when IsUniqueConstraintViolation(dbException)
                    => "A resource with the same unique value already exists.",

                _ => "An unexpected error occurred."
            },

            Instance = context.Request.Path
        };

        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }

    private static bool IsUniqueConstraintViolation(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException
            && (sqlException.Number == 2601
                || sqlException.Number == 2627);
    }
}