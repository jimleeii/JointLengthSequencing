namespace JointLengthSequencing;

public interface IEndpointDefinition
{
    void DefineServices(IServiceCollection services);

    void DefineEndpoints(WebApplication app, IWebHostEnvironment evnt);
}