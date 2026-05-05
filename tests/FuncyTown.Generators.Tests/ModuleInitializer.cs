using System.Runtime.CompilerServices;

namespace FuncyTown.Generators.Tests;

internal static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();
    }
}
