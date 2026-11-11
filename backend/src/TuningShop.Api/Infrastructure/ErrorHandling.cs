using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TuningShop.Api.Contracts;

namespace TuningShop.Api.Infrastructure;

public static class ErrorHandling
{
    /// <summary>
    /// Зовнішній обробник: інфраструктурні збої отримують погоджену відповідь ApiError
    /// без внутрішніх деталей сервера. Предметні відмови сюди не потрапляють — їх повертають служби.
    /// </summary>
    public static IApplicationBuilder UseApiErrorHandling(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            try
            {
                await next(context);
            }
            catch (BadHttpRequestException)
            {
                await WriteAsync(context, 400, "invalid_request", "Запит має некоректну форму.");
            }
            catch (Exception ex) when (ex is DbUpdateException or SqliteException)
            {
                context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(ex, "Storage failure");
                await WriteAsync(context, 503, "storage_unavailable", "Не вдалося підтвердити запис у сховищі. Перевірте стан і повторіть дію.");
            }
            catch (Exception ex)
            {
                context.RequestServices.GetRequiredService<ILogger<Program>>().LogError(ex, "Unhandled error");
                await WriteAsync(context, 500, "internal_error", "Внутрішня помилка сервера.");
            }
        });

    /// <summary>Помилки розбору тіла чи параметрів (невірний JSON, тип, значення переліку) → 400 з полями.</summary>
    public static IActionResult InvalidModelState(ActionContext context)
    {
        // Ключ із назвою параметра дії («request») дублює конкретні поля JSON, тому його пропускаємо.
        var parameterNames = context.ActionDescriptor.Parameters.Select(p => p.Name).ToHashSet();
        var fields = context.ModelState
            .Where(e => e.Value?.Errors.Count > 0 && !parameterNames.Contains(e.Key))
            .ToDictionary(
                e => ToFieldName(e.Key),
                _ => "Некоректне значення.");
        if (fields.Count == 0) fields["body"] = "Тіло запиту відсутнє або має некоректну форму.";
        return new BadRequestObjectResult(new ApiError("invalid_request", "Запит містить некоректні дані.", fields));
    }

    public static Task WriteAsync(HttpContext context, int status, string code, string message)
    {
        if (context.Response.HasStarted)
            throw new InvalidOperationException("Відповідь уже надсилається.");
        context.Response.Clear();
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new ApiError(code, message));
    }

    private static string ToFieldName(string key)
    {
        var name = key.StartsWith("$.") ? key[2..] : key.TrimStart('$');
        return name.Length == 0 ? "body" : char.ToLowerInvariant(name[0]) + name[1..];
    }
}
