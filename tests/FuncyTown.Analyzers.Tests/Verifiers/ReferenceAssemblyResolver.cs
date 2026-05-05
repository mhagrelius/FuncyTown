using Microsoft.CodeAnalysis.Testing;

namespace FuncyTown.Analyzers.Tests.Verifiers;

internal static class ReferenceAssemblyResolver
{
    internal static ReferenceAssemblies Net10 { get; } = CreateNet10ReferenceAssemblies();

    private static ReferenceAssemblies CreateNet10ReferenceAssemblies()
    {
        var packRoot = FindNetCoreAppRefPackRoot();
        var version = Directory.EnumerateDirectories(packRoot)
            .Select(Path.GetFileName)
            .Where(version => !string.IsNullOrWhiteSpace(version))
            .Where(version => Directory.Exists(Path.Combine(packRoot, version!, "ref", "net10.0")))
            .OrderByDescending(ParseVersion)
            .ThenByDescending(static version => version, StringComparer.Ordinal)
            .FirstOrDefault();

        if (version is null)
        {
            throw new InvalidOperationException(
                $"Could not find a net10.0 reference assembly pack under '{packRoot}'. Install the .NET 10 SDK.");
        }

        return new ReferenceAssemblies(
            "net10.0",
            new PackageIdentity("Microsoft.NETCore.App.Ref", version),
            "ref/net10.0");
    }

    private static string FindNetCoreAppRefPackRoot()
    {
        foreach (var dotnetRoot in GetDotnetRoots())
        {
            var packRoot = Path.Combine(dotnetRoot, "packs", "Microsoft.NETCore.App.Ref");
            if (Directory.Exists(packRoot))
            {
                return packRoot;
            }
        }

        throw new InvalidOperationException("Could not find the Microsoft.NETCore.App.Ref pack. Install the .NET 10 SDK.");
    }

    private static IEnumerable<string> GetDotnetRoots()
    {
        foreach (var variable in new[] { "DOTNET_ROOT", "DOTNET_ROOT_X64", "DOTNET_ROOT_ARM64" })
        {
            var value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                yield return value;
            }
        }

        yield return "/usr/local/share/dotnet";
        yield return "/usr/share/dotnet";
    }

    private static Version ParseVersion(string? value)
    {
        var normalized = value?.Split('-', 2)[0];
        return Version.TryParse(normalized, out var version)
            ? version
            : new Version();
    }
}
