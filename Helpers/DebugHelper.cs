using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace StudentPortalAPI.Helpers
{
    public static class DebugHelper
    {
        public static string GetFullExceptionMessage(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== EXCEPTION DETAILS ===");
            sb.AppendLine($"Type: {ex.GetType().FullName}");
            sb.AppendLine($"Message: {ex.Message}");
            sb.AppendLine($"Stack Trace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine("--- INNER EXCEPTION ---");
                sb.AppendLine($"Type: {ex.InnerException.GetType().FullName}");
                sb.AppendLine($"Message: {ex.InnerException.Message}");
                sb.AppendLine($"Stack Trace: {ex.InnerException.StackTrace}");
            }

            if (ex is DbUpdateException dbEx)
            {
                sb.AppendLine("--- DATABASE ERROR ---");
                sb.AppendLine($"Entity: {dbEx.Entries?.FirstOrDefault()?.Entity?.GetType().Name}");
                sb.AppendLine($"State: {dbEx.Entries?.FirstOrDefault()?.State}");
            }

            return sb.ToString();
        }

        public static string SerializeObject(object obj)
        {
            var options = new JsonSerializerOptions { WriteIndented = true, MaxDepth = 10 };
            return JsonSerializer.Serialize(obj, options);
        }

        public static void LogRequestDetails(ILogger logger, HttpRequest request)
        {
            logger.LogInformation($"=== REQUEST DETAILS ===");
            logger.LogInformation($"Method: {request.Method}");
            logger.LogInformation($"Path: {request.Path}");
            logger.LogInformation($"QueryString: {request.QueryString}");
            logger.LogInformation($"ContentType: {request.ContentType}");
        }
    }
}