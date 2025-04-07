using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace D12Canvas.Tests;

public abstract class ComponentTestBase : TestContext
{
    protected ComponentTestBase()
    {
        // Add any common services or configurations here
        Services.AddScoped<IServiceProvider>(sp => sp);
    }
}
