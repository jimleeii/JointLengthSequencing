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
	private ILogger<SwaggerEndpointDefinition>? Logger;

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

			Logger!.LogInformation("Using Swagger UI");
			app.UseSwagger();
			app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Title} {Version}"));
		}
	}

	/// <summary>
	/// Defines the services.
	/// </summary>
	/// <param name="services">The services.</param>
	public void DefineServices(IServiceCollection services)
	{
		Logger = services.BuildServiceProvider().GetRequiredService<ILogger<SwaggerEndpointDefinition>>();

		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc(Version, new OpenApiInfo { Title = Title, Version = Version });
		});
	}
}