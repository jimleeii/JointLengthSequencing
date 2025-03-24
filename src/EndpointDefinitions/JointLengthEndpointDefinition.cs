using EndpointDefinition;
using Microsoft.AspNetCore.Mvc;

namespace JointLengthSequencing.EndpointDefinitions;

/// <summary>
/// The joint length endpoint definition.
/// </summary>
public class JointLengthEndpointDefinition : IEndpointDefinition
{
    private ILogger<JointLengthEndpointDefinition>? Logger;

    /// <summary>
    /// Defines the endpoints.
    /// </summary>
    /// <param name="app">The app.</param>
    /// <param name="env">The environment.</param>
    public void DefineEndpoints(WebApplication app, IWebHostEnvironment env)
    {
        // Define endpoints
        app.MapPost("/api/v{version:apiVersion}/JointLength", AlignAsync)
            .WithMetadata(
                new ApiVersionAttribute("1.0"),
                new ApiVersionAttribute("2.0"),
                new ApiVersionAttribute("3.0")
            );

        // Define different endpoints based on environment
        if (env.IsDevelopment())
        {
            app.MapGet("/api/JointLength/debug", () => "Debug endpoint");
            // Log configuration
            Logger!.LogInformation("Configuration: {Config}", Config.GetConfig());
        }
    }

    /// <summary>
    /// Configures the necessary services for joint length sequencing.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    public void DefineServices(IServiceCollection services)
    {
        Logger = services.BuildServiceProvider().GetRequiredService<ILogger<JointLengthEndpointDefinition>>();

        services.AddSingleton<JointLengthSequencer>();
        services.AddSingleton<JointLengthSequencer2>();
        services.AddSingleton<JointLengthSequencer3>();
    }

    /// <summary>
    /// Aligns two datasets using the joint length sequencing algorithm.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="alignmentData">The alignment data.</param>
    /// <returns>A Task of type IResult</returns>
    private static async Task<IResult> AlignAsync(ApiVersion version, IServiceProvider serviceProvider, AlignmentDataRequest alignmentData)
    {
        var errors = new List<string>();
        if (!alignmentData.Validate(ref errors))
        {
            Results.BadRequest(string.Join("\n", errors!));
        }

        IJointLengthSequencer jointLengthSequencer = version.MajorVersion switch
        {
            1 => serviceProvider.GetRequiredService<JointLengthSequencer>(),
            2 => serviceProvider.GetRequiredService<JointLengthSequencer2>(),
            3 => serviceProvider.GetRequiredService<JointLengthSequencer3>(),
            _ => throw new NotImplementedException()
        };

        var forecasts = await jointLengthSequencer.CalculateMatches(
            alignmentData.BaseData,
            alignmentData.TargetData,
            alignmentData.PivotPercentile,
            alignmentData.Tolerance,
            alignmentData.PivotRequired,
            alignmentData.BaseLengthCol,
            alignmentData.TargetLengthCol);
        return Results.Ok(forecasts);
    }
}