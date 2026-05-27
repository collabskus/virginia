using System.Net.Http.Json;
using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public class KathmanduCalendarService
{
    private readonly HttpClient _httpClient;
    private readonly ApiLoggerService _logger;
    private CalendarResponse? _cachedCalendarData;
    private DateTime? _calendarCacheTime;
    private TimeResponse? _cachedTimeData;
    private DateTime? _timeCacheTime;
    private TimeSpan _serverTimeOffset = TimeSpan.Zero;

    private const string TodayApiUrl = "https://calendar.bloggernepal.com/api/today";
    private const string TimeApiUrl = "https://calendar.bloggernepal.com/api/time";

    public KathmanduCalendarService(HttpClient httpClient, ApiLoggerService logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CalendarResponse?> GetTodayDataAsync()
    {
        // Cache for 1 hour
        if (_cachedCalendarData != null && _calendarCacheTime.HasValue &&
            DateTime.UtcNow - _calendarCacheTime.Value < TimeSpan.FromHours(1))
        {
            await _logger.LogApiRequestAsync(TodayApiUrl, _cachedCalendarData, true);
            return _cachedCalendarData;
        }

        try
        {
            var data = await _httpClient.GetFromJsonAsync<CalendarResponse>(TodayApiUrl);
            if (data != null)
            {
                _cachedCalendarData = data;
                _calendarCacheTime = DateTime.UtcNow;
                await _logger.LogApiRequestAsync(TodayApiUrl, data, false);
            }
            return data;
        }
        catch (Exception ex)
        {
            await _logger.LogApiRequestAsync(TodayApiUrl, new { error = ex.Message, failed = true }, false);
            throw;
        }
    }

    public async Task<TimeResponse?> GetTimeDataAsync()
    {
        // Cache for 5 minutes
        if (_cachedTimeData != null && _timeCacheTime.HasValue &&
            DateTime.UtcNow - _timeCacheTime.Value < TimeSpan.FromMinutes(5))
        {
            await _logger.LogApiRequestAsync(TimeApiUrl, _cachedTimeData, true);
            return _cachedTimeData;
        }

        try
        {
            var beforeFetch = DateTime.UtcNow;
            var data = await _httpClient.GetFromJsonAsync<TimeResponse>(TimeApiUrl);
            var afterFetch = DateTime.UtcNow;

            if (data != null)
            {
                _cachedTimeData = data;
                _timeCacheTime = DateTime.UtcNow;

                // Calculate server time offset
                var serverHour = int.Parse(data.Hour);
                var serverMin = int.Parse(data.Min);
                var serverSec = int.Parse(data.Sec);
                var isPM = data.ApOrPm == "PM";

                if (isPM && serverHour != 12) serverHour += 12;
                if (!isPM && serverHour == 12) serverHour = 0;

                var serverTime = new DateTime(
                    DateTime.UtcNow.Year,
                    DateTime.UtcNow.Month,
                    DateTime.UtcNow.Day,
                    serverHour,
                    serverMin,
                    serverSec
                );

                var localTime = beforeFetch + (afterFetch - beforeFetch) / 2;
                _serverTimeOffset = serverTime - localTime;

                await _logger.LogApiRequestAsync(TimeApiUrl, data, false);
            }
            return data;
        }
        catch (Exception ex)
        {
            await _logger.LogApiRequestAsync(TimeApiUrl, new { error = ex.Message, failed = true }, false);
            throw;
        }
    }

    public DateTime GetCurrentKathmanduTime()
    {
        return DateTime.UtcNow + _serverTimeOffset;
    }

    public string ToNepaliDigits(int number)
    {
        var nepaliDigits = new[] { "०", "१", "२", "३", "४", "५", "६", "७", "८", "९" };
        var numStr = number.ToString("D2");
        var result = "";
        foreach (var digit in numStr)
        {
            if (char.IsDigit(digit))
            {
                result += nepaliDigits[digit - '0'];
            }
        }
        return result;
    }
}
