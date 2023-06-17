using System.Net;
using System.Net.Mime;

namespace OnlineLinter
{
  public class ExceptionHandlingMiddleware
  {
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public ExceptionHandlingMiddleware(ILogger<ExceptionHandlingMiddleware> logger,
                                      RequestDelegate next)
    {
      this._logger = logger;
      this._next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
      try
      {
        await _next(httpContext);
      }
      catch (Exception ex)
      {
        httpContext.Response.ContentType = MediaTypeNames.Application.Json;
        httpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        if (ex is ArgumentException)
        {
          httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        }

        await httpContext.Response.WriteAsync($"An error occurred while processing your request. Found exception: {ex.Message}");
      }
    }
  }
}
