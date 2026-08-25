using System.Text.Json;

namespace TestCode.Services
{
    public class BusinessExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public BusinessExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (BusinessException ex)
            {
                context.Response.StatusCode = GetStatusCode(ex.Code);
                context.Response.ContentType = "application/json";

                var response = new
                {
                    code = ex.Code,
                    message = ex.Message
                };

                await context.Response.WriteAsync(
                    JsonSerializer.Serialize(response));
            }
        }

        private static int GetStatusCode(string code)
        {
            return code switch
            {
                "CONCURRENCY_CONFLICT" => StatusCodes.Status409Conflict,
                _ => StatusCodes.Status400BadRequest
            };
        }
    }
}