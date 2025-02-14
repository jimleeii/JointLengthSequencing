using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointDefinitions(typeof(IEndpointDefinition));
builder.Services.ConfigureHttpJsonOptions(options =>
{
	options.SerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
});

var app = builder.Build();
app.UseEndpointDefinitions(builder.Environment);

app.MapGet("/", () => "Hello JointLengthSequencing!");

await app.RunAsync();