using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace D12Canvas.Registration;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddD12Canvas(
        this IServiceCollection services,
        Action<D12CanvasOptions> configure
    )
    {
        var existingRegistry =
            services
                .FirstOrDefault(descriptor => descriptor.ServiceType == typeof(IComponentRegistry))
                ?.ImplementationInstance as ComponentRegistry;

        var options = existingRegistry is null
            ? new D12CanvasOptions()
            : new D12CanvasOptions(existingRegistry);
        configure(options);

        if (existingRegistry is null)
        {
            services.AddSingleton(options.Registry);
        }

        return services;
    }
}
