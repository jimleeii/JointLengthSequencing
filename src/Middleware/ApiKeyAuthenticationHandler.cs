using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace JointLengthSequencing.Middleware;

/// <summary>
/// Authentication handler for API Key authentication.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
{
    private const string ApiKeyHeaderName = "X-API-Key";
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiKeyAuthenticationHandler"/> class.
    /// </summary>
    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration configuration)
        : base(options, logger, encoder)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Handles the authentication asynchronously.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(ApiKeyHeaderName))
        {
            return AuthenticateResult.Fail("API Key header not found");
        }

        var providedApiKey = Request.Headers[ApiKeyHeaderName].ToString();
        
        // Get API keys from configuration (supports both appsettings and environment variables)
        // Priority: Environment variable > Configuration file
        var validApiKeys = GetValidApiKeys();

        if (validApiKeys == null || validApiKeys.Length == 0)
        {
            Logger.LogWarning("No API keys configured. Authentication will fail for all requests.");
            return AuthenticateResult.Fail("API authentication is not configured");
        }

        if (!validApiKeys.Contains(providedApiKey))
        {
            Logger.LogWarning("Invalid API key attempt from {IpAddress}", Request.HttpContext.Connection.RemoteIpAddress);
            return AuthenticateResult.Fail("Invalid API Key");
        }

        var claims = new[] { new Claim(ClaimTypes.Name, "ApiUser") };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return await Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>
    /// Gets valid API keys from environment variables or configuration.
    /// Environment variable takes precedence over configuration file.
    /// </summary>
    /// <returns>Array of valid API keys.</returns>
    private string[] GetValidApiKeys()
    {
        // Try environment variable first (recommended for production)
        var envApiKeys = Environment.GetEnvironmentVariable("API_KEYS");
        if (!string.IsNullOrWhiteSpace(envApiKeys))
        {
            Logger.LogInformation("Using API keys from environment variable");
            return envApiKeys.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        // Fall back to configuration file
        var configApiKeys = _configuration.GetSection("Authentication:ApiKeys").Get<string[]>();
        if (configApiKeys != null && configApiKeys.Length > 0)
        {
            Logger.LogInformation("Using API keys from configuration file");
            return configApiKeys;
        }

        return Array.Empty<string>();
    }
}

/// <summary>
/// Options for API Key authentication.
/// </summary>
public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
{
}
