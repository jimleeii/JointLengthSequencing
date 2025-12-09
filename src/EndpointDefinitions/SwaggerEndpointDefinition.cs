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

		// Enable OpenAPI endpoint
		app.MapOpenApi();
		
		// Enable Swagger in all environments (with auth required in production)
		app.UseSwagger();
		app.UseSwaggerUI(c =>
		{
			c.SwaggerEndpoint("/swagger/v1/swagger.json", $"{Title} {Version}");
			c.SwaggerEndpoint("/openapi/v1.json", $"{Title} OpenAPI {Version}");
			c.RoutePrefix = string.Empty; // Serve Swagger UI at root
		});
	}

	/// <summary>
	/// Defines the services.
	/// </summary>
	/// <param name="services">The services.</param>
	public void DefineServices(IServiceCollection services)
	{
		// Add OpenAPI support
		services.AddOpenApi();
		
		services.AddEndpointsApiExplorer();
		services.AddSwaggerGen(c =>
		{
			c.SwaggerDoc(Version, new OpenApiInfo
			{
				Title = Title,
				Version = Version,
				Description = @"# Joint Length Sequencing API

Aligns two datasets of joints based on their lengths using dynamic programming algorithms.

## Features
- **Three algorithm versions** (v1, v2, v3) with different optimizations
- **Pagination support** for large result sets
- **Output caching** for improved performance
- **Rate limiting** to prevent abuse
- **OpenTelemetry** for observability

## Authentication
All endpoints require API Key authentication via the `X-API-Key` header.

## Pagination
Use `pageNumber` and `pageSize` query parameters to paginate large result sets:
- Default page size: 20
- Maximum page size: 100

## Rate Limits
- Production: 60 requests/minute, 1000 requests/hour
- Development: 300 requests/minute, 10000 requests/hour",
				Contact = new OpenApiContact
				{
					Name = "Wei Li",
					Email = "wei.li@example.com",
					Url = new Uri("https://github.com/jimleeii/JointLengthSequencing")
				},
				License = new OpenApiLicense
				{
					Name = "MIT",
					Url = new Uri("https://opensource.org/licenses/MIT")
				},
				TermsOfService = new Uri("https://github.com/jimleeii/JointLengthSequencing/blob/main/README.md")
			});

			// Include XML comments
			var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
			var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
			if (File.Exists(xmlPath))
			{
				c.IncludeXmlComments(xmlPath);
			}

			// Add API Key authentication to Swagger
			c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.ApiKey,
				In = ParameterLocation.Header,
				Name = "X-API-Key",
				Description = "API Key Authentication"
			});

			c.AddSecurityRequirement(new OpenApiSecurityRequirement
			{
				{
					new OpenApiSecurityScheme
					{
						Reference = new OpenApiReference
						{
							Type = ReferenceType.SecurityScheme,
							Id = "ApiKey"
						}
					},
					Array.Empty<string>()
				}
			});
		});
	}
}