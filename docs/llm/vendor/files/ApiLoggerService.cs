using System.Text;
using System.Text.Json;

namespace CollabsKus.BlazorWebAssembly.Services;

public class ApiLoggerService
{
    private readonly HttpClient _httpClient;
    private const string LoggerUrl = "https://my-api.2w7sp317.workers.dev/ui/create";

    public ApiLoggerService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task LogApiRequestAsync(string endpoint, object data, bool fromCache)
    {
        try
        {
            var logData = new
            {
                endpoint,
                timestamp = DateTime.UtcNow.ToString("O"),
                fromCache,
                data,
                userAgent = "Blazor WebAssembly",
                page = "https://collabskus.github.io"
            };

            var logContent = JsonSerializer.Serialize(logData, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            // Truncate to 1000 chars
            if (logContent.Length > 1000)
            {
                logContent = logContent.Substring(0, 997) + "...";
            }

            var formData = new Dictionary<string, string>
            {
                { "title", $"API Request: {endpoint}" },
                { "content", logContent }
            };

            var content = new FormUrlEncodedContent(formData);

            // Fire and forget - don't await
            _ = Task.Run(async () =>
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, LoggerUrl)
                    {
                        Content = content
                    };
                    request.Headers.Add("Accept", "application/json");

                    await _httpClient.SendAsync(request);
                }
                catch
                {
                    // Silent fail - logging is non-critical
                }
            });
        }
        catch
        {
            // Silent fail - don't let logging break the app
        }
    }
}
