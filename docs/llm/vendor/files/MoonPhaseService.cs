using CollabsKus.BlazorWebAssembly.Models;

namespace CollabsKus.BlazorWebAssembly.Services;

public class MoonPhaseService
{
    private readonly List<MoonPhaseDefinition> _phaseDefinitions = new()
    {
        new("New Moon", "🌑", 0, 1.84566),
        new("Waxing Crescent", "🌒", 1.84566, 5.53699),
        new("First Quarter", "🌓", 5.53699, 9.22831),
        new("Waxing Gibbous", "🌔", 9.22831, 12.91963),
        new("Full Moon", "🌕", 12.91963, 16.61096),
        new("Waning Gibbous", "🌖", 16.61096, 20.30228),
        new("Last Quarter", "🌗", 20.30228, 23.99361),
        new("Waning Crescent", "🌘", 23.99361, 29.53059)
    };

    public MoonPhase CalculateMoonPhase(DateTime date)
    {
        // Convert to Julian Day Number
        var year = date.Year;
        var month = date.Month;
        var day = date.Day;

        var a = (14 - month) / 12;
        var y = year + 4800 - a;
        var m = month + 12 * a - 3;

        var jdn = day + (153 * m + 2) / 5 + 365 * y +
                  y / 4 - y / 100 + y / 400 - 32045;

        // Known new moon: January 6, 2000
        const double knownNewMoon = 2451550.1;
        var daysSinceNew = jdn - knownNewMoon;

        // Synodic month
        const double synodicMonth = 29.53058867;

        // Calculate moon age
        var newMoons = daysSinceNew / synodicMonth;
        var moonAge = (newMoons - Math.Floor(newMoons)) * synodicMonth;

        // Calculate illumination
        var moonPhaseAngle = (moonAge / synodicMonth) * 2 * Math.PI;
        var illumination = (1 - Math.Cos(moonPhaseAngle)) / 2;

        var phase = GetMoonPhaseName(moonAge);

        return new MoonPhase
        {
            Name = phase.Name,
            Icon = phase.Icon,
            Illumination = illumination * 100,
            Age = moonAge
        };
    }

    private MoonPhaseDefinition GetMoonPhaseName(double age)
    {
        foreach (var phase in _phaseDefinitions)
        {
            if (age >= phase.Min && age < phase.Max)
            {
                return phase;
            }
        }
        return _phaseDefinitions[0];
    }

    private record MoonPhaseDefinition(string Name, string Icon, double Min, double Max);
}
