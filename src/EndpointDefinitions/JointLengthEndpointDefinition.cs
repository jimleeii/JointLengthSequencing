using EndpointDefinition;
using Microsoft.AspNetCore.Mvc;

namespace JointLengthSequencing.EndpointDefinitions;

/// <summary>
/// The joint length endpoint definition.
/// </summary>
public class JointLengthEndpointDefinition : IEndpointDefinition
{

    /// <summary>
    /// Defines the endpoints.
    /// </summary>
    /// <param name="app">The app.</param>
    /// <param name="env">The environment.</param>
    public void DefineEndpoints(WebApplication app, IWebHostEnvironment env)
    {
        // Define endpoints with authentication required
        app.MapPost("/api/v{version:apiVersion}/JointLength", AlignAsync)
            .RequireAuthorization()
            .WithMetadata(
                new ApiVersionAttribute("1.0"),
                new ApiVersionAttribute("2.0"),
                new ApiVersionAttribute("3.0")
            )
            .WithName("AlignJointLength")
            .WithDescription("Aligns two datasets of joints based on their lengths using dynamic programming. Returns a list of matching joint pairs.")
            .WithSummary("Align joint datasets")
            .Produces<PagedResponse<JointMatchResult>>(StatusCodes.Status200OK, "application/json")
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithTags("JointLength")
            .CacheOutput("ApiResponses");

        // Define different endpoints based on environment
        if (env.IsDevelopment())
        {
            app.MapGet("/api/JointLength/debug", () => new { 
                status = "Debug endpoint active",
                timestamp = DateTime.UtcNow,
                environment = "Development"
            })
                .AllowAnonymous()
                .WithTags("Debug")
                .WithDescription("Debug endpoint for development environment")
                .ExcludeFromDescription();
        }
    }

    /// <summary>
    /// Configures the necessary services for joint length sequencing.
    /// </summary>
    /// <param name="services">The service collection to which the services are added.</param>
    public void DefineServices(IServiceCollection services)
    {
        // Register services as Scoped to avoid potential memory issues with large datasets
        services.AddScoped<JointLengthSequencer>();
        services.AddScoped<JointLengthSequencer2>();
        services.AddScoped<JointLengthSequencer3>();
    }

    /// <summary>
    /// Aligns two datasets using the joint length sequencing algorithm.
    /// </summary>
    /// <param name="version">The API version.</param>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="alignmentData">The alignment data containing base and target datasets.</param>
    /// <param name="pagination">Pagination parameters for the response.</param>
    /// <param name="logger">The logger instance.</param>
    /// <returns>A paginated response containing matched joint pairs.</returns>
    private static async Task<IResult> AlignAsync(
        ApiVersion version, 
        IServiceProvider serviceProvider, 
        AlignmentDataRequest alignmentData,
        [AsParameters] PaginationParams pagination,
        ILogger<JointLengthEndpointDefinition> logger)
    {
        // Validate alignment data
        var errors = new List<string>();
        if (!alignmentData.Validate(ref errors))
        {
            var errorMessage = errors != null ? string.Join(", ", errors) : "Unknown validation error";
            logger.LogWarning("Validation failed: {Errors}", errorMessage);
            return Results.BadRequest(new { errors });
        }

		// Validate pagination
		if (!pagination.Validate(ref errors))
		{
			logger.LogWarning("Pagination validation failed: {Errors}", string.Join(", ", errors ?? []));
			return Results.BadRequest(new { errors });
		}

        logger.LogInformation("Processing alignment request for version {Version} with pagination (Page: {Page}, Size: {Size})", 
            version.MajorVersion, pagination.PageNumber, pagination.PageSize);

        IJointLengthSequencer jointLengthSequencer = version.MajorVersion switch
        {
            1 => serviceProvider.GetRequiredService<JointLengthSequencer>(),
            2 => serviceProvider.GetRequiredService<JointLengthSequencer2>(),
            3 => serviceProvider.GetRequiredService<JointLengthSequencer3>(),
            _ => throw new NotImplementedException($"Version {version.MajorVersion} is not implemented")
        };

        var matches = await jointLengthSequencer.CalculateMatches(
            alignmentData.BaseData,
            alignmentData.TargetData,
            alignmentData.PivotPercentile,
            alignmentData.Tolerance,
            alignmentData.PivotRequired,
            alignmentData.BaseLengthCol,
            alignmentData.TargetLengthCol);

        logger.LogInformation("Successfully processed alignment request with {Count} total matches", matches.Count);

        // Create paginated response
        var pagedResponse = PagedResponse<JointMatchResult>.Create(
            matches, 
            pagination.PageNumber, 
            pagination.PageSize);

        logger.LogInformation("Returning page {Page} of {TotalPages} ({ItemCount} items)", 
            pagedResponse.PageNumber, pagedResponse.TotalPages, pagedResponse.Items.Count());

        return Results.Ok(pagedResponse);
    }
}