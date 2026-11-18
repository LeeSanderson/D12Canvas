using System.Linq;
using D12Canvas.BuiltIns;
using D12Canvas.Persistence;
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

        if (existingRegistry is not null)
        {
            configure(new D12CanvasOptions(existingRegistry));
            return services;
        }

        // The built-ins register before the host's types so the Palette lists them first.
        var options = new D12CanvasOptions();
        BuiltInComponents.RegisterAll(options);
        configure(options);

        services.AddSingleton(options.Registry);
        services.AddSingleton<IBoardSerializer, BoardJsonSerializer>();
        return services;
    }
}
