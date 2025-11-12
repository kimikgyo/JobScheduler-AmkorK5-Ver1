using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Text.Encodings.Web;
using System.Text.Json;

internal static class Extensions
{
    public static string ControllerLogPath(this ControllerBase controller, string id = null)
    {
        // HTTP 메서드 (GET, POST 등)
        var httpMethod = controller.ControllerContext.HttpContext.Request.Method;

        // 현재 컨트롤러 이름
        var controllerName = controller.ControllerContext.ActionDescriptor.ControllerName;

        // 경로 구성
        var path = string.IsNullOrWhiteSpace(id)
            ? $"{httpMethod} /api/{controllerName}"
            : $"{httpMethod} /api/{controllerName}/{id}";

        return path;
    }

    public static string GetFullMessage(this Exception ex)
    {
        return ex.InnerException == null
                ? ex.Message
                : ex.Message + " --> " + ex.InnerException.GetFullMessage();
    }

    public static BindingList<T> ToBindingList<T>(this IList<T> source)
    {
        return new BindingList<T>(source);
    }

    private static JsonSerializerOptions options = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions()
    {
        //WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static string ToJson(this object Value)
    {
        try
        {
            return JsonSerializer.Serialize(Value, serializerOptions);
        }
        catch (JsonException)
        {
            return "유효하지 않은 JSON 문자열입니다.";
        }
    }

    public static string BeautifyJson(this string jsonString)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                return JsonSerializer.Serialize(doc.RootElement, options);
            }
        }
        catch (JsonException)
        {
            return "유효하지 않은 JSON 문자열입니다.";
        }
    }

    public static bool IsValidJson(this string jsonString)
    {
        try
        {
            using (JsonDocument doc = JsonDocument.Parse(jsonString))
            {
                return true;
            }
        }
        catch (JsonException)
        {
            return false;
        }
    }
}