using System.Reflection;
using EndpointDefinition;
using Microsoft.OpenApi.Models;

namespace JointLengthSequencing.EndpointDefinitions;

/// <summary>
/// The swagger endpoint definition.
/// </summary>
public class SwaggerEndpointDefinition : IEndpointDefinition
{
	private readonly string Title = Assembly.GetEntryAssembly()!.GetName().Name!;
	private const string Version = "v1";

	/// <summary>
	/// Defines the endpoints.
	/// </summary>
	/// <param name="app">The app.</param>
	/// <param name="env">The environment.</param>
	public void DefineEndpoints(WebApplication app, IWebHostEnvironment env)
	{
		if (env.IsDevelopment())
		{
			app.UseDeveloperExceptionPage();
		}

		// Enable Swagger in all environments (with auth required in production)
		app.UseSwagger();
		// Enable OpenAPI endpoint
		app.UseSwaggerUI(c => c.SwaggerEndpoint("/openapi/v1.json", $"{Title} {Version}"));
	}

	/// <summary>
	/// Defines the services.
	/// </summary>
	/// <param name="services">The services.</param>
	public void DefineServices(IServiceCollection services)
	{
		services.AddEndpointsApiExplorer();
		// Add OpenAPI support
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc(Version, new OpenApiInfo { Title = Title, Version = Version });
		});
	}
}