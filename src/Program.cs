using AspNetCoreRateLimit;
using EndpointDefinition;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Text.Json.Serialization;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/jointlength-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "JointLengthSequencing")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add OpenTelemetry
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService(
            serviceName: "JointLengthSequencing",
            serviceVersion: "1.0.0"))
        .WithTracing(tracing => tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddConsoleExporter());

    // Add Output Caching
    builder.Services.AddOutputCache(options =>
    {
        options.AddBasePolicy(policyBuilder => policyBuilder
            .Expire(TimeSpan.FromMinutes(5))
            .Tag("default"));

        options.AddPolicy("ApiResponses", policyBuilder => policyBuilder
            .Expire(TimeSpan.FromMinutes(10))
            .SetVaryByQuery("pageNumber", "pageSize")
            .Tag("api"));
    });

    // Add Response Compression
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // Add Memory Cache
    builder.Services.AddMemoryCache();

    // Add Response Caching
    builder.Services.AddResponseCaching();

    // Add Rate Limiting
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.Configure<IpRateLimitPolicies>(builder.Configuration.GetSection("IpRateLimitPolicies"));
    builder.Services.AddInMemoryRateLimiting();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DefaultPolicy", policy =>
        {
            policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["*"])
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .WithExposedHeaders("X-Correlation-Id");
        });
    });

    // Add Authentication
    builder.Services.AddAuthentication("ApiKey")
        .AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>("ApiKey", options => { });
    builder.Services.AddAuthorization();

    builder.Services.AddHealthChecks();

    // Configure request size limits
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
    {
        options.MultipartBodyLengthLimit = 10485760; // 10 MB
    });

    // Configure graceful shutdown
    builder.Services.Configure<HostOptions>(options =>
    {
        options.ShutdownTimeout = TimeSpan.FromSeconds(30);
    });

    builder.WebHost.ConfigureKestrel(serverOptions =>
    {
        serverOptions.Limits.MaxRequestBodySize = 10485760; // 10 MB
    });

    builder.Services.AddEndpointDefinitions(typeof(Program));
    builder.Services.ConfigureHttpJsonOptions(options =>
    {
        options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = ApiVersionReader.Combine(
            new UrlSegmentApiVersionReader(),
            new HeaderApiVersionReader("X-API-Version")
        );
    });

    var app = builder.Build();

    // Global exception handling middleware
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Correlation ID middleware
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Enable IP rate limiting
    app.UseIpRateLimiting();

    // Enable output caching
    app.UseOutputCache();

    // Enable response compression
    app.UseResponseCompression();

    // Enable response caching
    app.UseResponseCaching();

    // Enable CORS
    app.UseCors("DefaultPolicy");

    // Enable HTTPS redirection in production
    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }

    // Enable authentication and authorization
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpointDefinitions(builder.Environment);

    app.MapHealthChecks("/health");
    app.MapGet("/", () => "Hello JointLengthSequencing!").AllowAnonymous();

    // Register graceful shutdown handler
    var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
    lifetime.ApplicationStopping.Register(() =>
    {
        Log.Information("Application is shutting down gracefully...");
    });
    lifetime.ApplicationStopped.Register(() =>
    {
        Log.Information("Application has stopped");
    });

    Log.Information("Starting JointLengthSequencing API");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}