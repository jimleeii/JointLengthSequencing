using System.Text.Json;

namespace JointLengthSequencing;

/// <summary>
/// Configuration class for defining data contracts.
/// </summary>
public static class Config
{
    /// <summary>
    /// Returns the configuration data as a JSON string.
    /// This configuration consists of a base run and a target run, as well as parameters for the algorithm.
    /// </summary>
    /// <returns>The configuration data as a JSON string.</returns>
    public static string GetConfig()
    {
        // Define Base Run
        var baseData = new List<Dictionary<string, object>>
        {
            new() { { "length", 2 } },
            new() { { "length", 4.5 } },
            new() { { "length", 3.5 } },
            new() { { "length", 6 } },
            new() { { "length", 5 } },
            new() { { "length", 7.5 } },
            new() { { "length", 6.5 } },
            new() { { "length", 9 } },
            new() { { "length", 8 } },
            new() { { "length", 10.5 } },
            new() { { "length", 9.5 } },
            new() { { "length", 12 } },
            new() { { "length", 11 } },
            new() { { "length", 13.5 } },
            new() { { "length", 12.5 } },
            new() { { "length", 15 } },
            new() { { "length", 14 } },
            new() { { "length", 16.5 } },
            new() { { "length", 15.5 } },
            new() { { "length", 18 } }
        };

        // Define Target Run
        var targetData = new List<Dictionary<string, object>>
        {
            new() { { "length", 3.6 } },
            new() { { "length", 5.1 } },
            new() { { "length", 2.1 } },
            new() { { "length", 6.6 } },
            new() { { "length", 8.1 } },
            new() { { "length", 14.1 } },
            new() { { "length", 9.6 } },
            new() { { "length", 11.1 } },
            new() { { "length", 12.6 } },
            new() { { "length", 15.6 } },
            new() { { "length", 4.6 } },
            new() { { "length", 7.1 } },
            new() { { "length", 16 } },
            new() { { "length", 10 } },
            new() { { "length", 13 } },
            new() { { "length", 17.5 } },
            new() { { "length", 19 } },
            new() { { "length", 20.5 } },
            new() { { "length", 22 } },
            new() { { "length", 24.5 } }
        };

        // Structure the configuration
        var config = new AlignmentDataRequest
        {
            BaseData = baseData,
            TargetData = targetData,
            PivotPercentile = 0.1,
            Tolerance = 1.5,
            PivotRequired = 10,
            BaseLengthCol = "length",
            TargetLengthCol = "length",
        };

        return JsonSerializer.Serialize(config, options: new JsonSerializerOptions { WriteIndented = true });
    }
}