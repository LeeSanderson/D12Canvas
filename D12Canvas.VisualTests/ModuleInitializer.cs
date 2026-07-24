using System.Runtime.CompilerServices;
using VerifyTests;

namespace D12Canvas.VisualTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize() => VerifyPlaywright.Initialize(installPlaywright: true);
}
