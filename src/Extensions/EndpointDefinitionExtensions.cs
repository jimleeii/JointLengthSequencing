namespace JointLengthSequencing;

/// <summary>
/// The endpoint definition extensions.
/// </summary>
public static class EndpointDefinitionExtensions
{
    /// <summary>
    /// Add endpoint definitions.
    /// </summary>
    /// <param name="services">The services.</param>
    /// <param name="scanMarkers">The scan markers.</param>
    public static void AddEndpointDefinitions(this IServiceCollection services, params Type[] scanMarkers)
    {
        var endpointDefinitions = scanMarkers
            .SelectMany(marker => marker.Assembly.ExportedTypes
                .Where(type => typeof(IEndpointDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract))
            .Select(static type => (IEndpointDefinition)Activator.CreateInstance(type)!)
            .ToList();

        foreach (var endpointDefinition in endpointDefinitions)
        {
            endpointDefinition.DefineServices(services);
        }

        services.AddSingleton(endpointDefinitions as IReadOnlyCollection<IEndpointDefinition>);
    }

    /// <summary>
    /// Use endpoint definitions.
    /// </summary>
    /// <param name="app">The app.</param>
    /// <param name="evnt">The environment.</param>
    public static void UseEndpointDefinitions(this WebApplication app, IWebHostEnvironment evnt)
    {
        if (app.Services.GetService(typeof(IReadOnlyCollection<IEndpointDefinition>)) is IReadOnlyCollection<IEndpointDefinition> definitions)
        {
            Parallel.ForEach(definitions, endpointDefinition => endpointDefinition.DefineEndpoints(app, evnt));
        }
    }
}