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
	/// <param name="evnt">The environment.</param>
	public void DefineEndpoints(WebApplication app, IWebHostEnvironment evnt)
	{
		app.MapPost("api/JointLength", AlignAsync);
	}

	/// <summary>
	/// Configures the necessary services for joint length sequencing.
	/// </summary>
	/// <param name="services">The service collection to which the services are added.</param>
	public void DefineServices(IServiceCollection services)
	{
		services.AddSingleton<IJointLengthSequencer, JointLengthSequencer3>();
	}

	/// <summary>
	/// Aligns two datasets using the joint length sequencing algorithm.
	/// </summary>
	/// <param name="jointLengthSequencer">The joint length sequencer.</param>
	/// <param name="alignmentData">The alignment data.</param>
	/// <returns>The list of aligned points.</returns>
	private static async Task<IResult> AlignAsync(IJointLengthSequencer jointLengthSequencer, AlignmentDataRequest alignmentData)
	{
		var errors = new List<string>();
		if (!alignmentData.Validate(ref errors))
		{
			Results.BadRequest(string.Join("\n", errors!));
		}

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