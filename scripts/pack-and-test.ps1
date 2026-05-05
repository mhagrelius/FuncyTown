$ErrorActionPreference = 'Stop'
$RepoRoot   = (Resolve-Path "$PSScriptRoot/..").Path
$Artifacts  = Join-Path $RepoRoot 'artifacts'
$Work       = Join-Path ([System.IO.Path]::GetTempPath()) 'funcytown-smoke-consumer'

function Exit-OnLastNativeFailure {
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

if (Test-Path $Work) { Remove-Item -Recurse -Force $Work }
New-Item -ItemType Directory -Force -Path $Artifacts | Out-Null
Remove-Item -Path (Join-Path $Artifacts '*.nupkg') -Force -ErrorAction SilentlyContinue
Remove-Item -Path (Join-Path $Artifacts '*.snupkg') -Force -ErrorAction SilentlyContinue

Write-Host "==> Packing all projects to $Artifacts"
dotnet pack (Join-Path $RepoRoot 'FuncyTown.sln') --configuration Release --output $Artifacts
Exit-OnLastNativeFailure

$pkg = Get-ChildItem -Path $Artifacts -Filter 'FuncyTown.*.nupkg' |
    Where-Object { $_.Name -match '^FuncyTown\.[0-9].*\.nupkg$' } |
    Select-Object -First 1
$Version = ($pkg.Name -replace '^FuncyTown\.', '' -replace '\.nupkg$', '')
Write-Host "    Version: $Version"

Write-Host "==> Creating throwaway consumer in $Work"
New-Item -ItemType Directory -Force -Path $Work | Out-Null
$env:NUGET_PACKAGES = Join-Path $Work '.nuget'
$env:NUGET_HTTP_CACHE_PATH = Join-Path $Work '.nuget-http-cache'
Set-Location $Work
dotnet new console -n SmokeConsumer -f net10.0 --force | Out-Null
Exit-OnLastNativeFailure
Set-Location SmokeConsumer

@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$Artifacts" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
"@ | Set-Content -Path 'nuget.config' -Encoding UTF8

@'
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FuncyTown;

[ErrorUnion]
public partial class FakeError;

[ErrorCase]
public sealed partial class NotFound(int id) : FakeError
{
    public int Id { get; } = id;

    public override string Message => $"missing id {Id}";
}

[ErrorCase]
public sealed partial class Validation(string field) : FakeError
{
    public string Field { get; } = field;

    public override string Message => $"{Field} is invalid";
}

[Result<int, FakeError>]
public readonly partial record struct IntResult;

[Result<string, FakeError>]
public readonly partial record struct StringResult;

public static class Program
{
    public static async Task<int> Main()
    {
        var sync = IntResult.Success(7).Then(v => IntResult.Success(v + 1));
        System.Console.WriteLine($"value = {sync.Value}");

        var asyncResult = await Task.FromResult(IntResult.Success(20))
            .Then(async v =>
            {
                await Task.Yield();
                return IntResult.Success(v + 22);
            });
        System.Console.WriteLine($"async value = {asyncResult.Value}");

        StringResult crossAlias = IntResult.Success(42)
            .Then(v => StringResult.Success($"cross = {v}"));
        System.Console.WriteLine(crossAlias.Value);

        var taskCrossAlias = await Task.FromResult(IntResult.Success(11))
            .Then(v => StringResult.Success($"task cross = {v + 31}"));
        System.Console.WriteLine(taskCrossAlias.Value);

        IntResult validationFailure = new Validation("value");
        var validationText = validationFailure.Match(
            onSuccess: _ => "success",
            onFailure: error => error.Match(
                notFound: notFound => $"not found {notFound.Id}",
                validation: validation => $"validation {validation.Field}",
                many: many => $"many {many.Errors.Count}"));
        System.Console.WriteLine(validationText);

        StringResult notFoundFailure = new NotFound(404);
        var notFoundText = notFoundFailure.Match(
            onSuccess: _ => "success",
            onFailure: error => error.Match(
                notFound: notFound => notFound.Message,
                validation: validation => validation.Message,
                many: many => $"many {many.Errors.Count}"));
        System.Console.WriteLine(notFoundText);

        var combined = FakeError.Combine(new List<FakeError> { new NotFound(1), new Validation("name") });
        var combinedText = combined.Match(
            notFound: notFound => $"not found {notFound.Id}",
            validation: validation => validation.Message,
            many: many => $"many {many.Errors.Count}");
        System.Console.WriteLine(combinedText);

        var all = Result.All(
            Result<int, FakeError>.Success(1),
            Result<string, FakeError>.Success("two"));
        System.Console.WriteLine($"all = {all.Value.Item1}, {all.Value.Item2}");

        var accumulated = Result.Combine(
            Result<int, FakeError>.Failure(new NotFound(1)),
            Result<int, FakeError>.Success(7),
            Result<int, FakeError>.Failure(new Validation("name")));
        var accumulatedText = accumulated.Error.Match(
            notFound: notFound => $"not found {notFound.Id}",
            validation: validation => validation.Message,
            many: many => $"combined many {many.Errors.Count}");
        System.Console.WriteLine(accumulatedText);

        var successfulCombine = Result.Combine(
            Result<int, FakeError>.Success(1),
            Result<int, FakeError>.Success(2));
        System.Console.WriteLine(string.Join(",", successfulCombine.Value));

        var traversed = Result.Traverse(
            new[] { 1, 2, 3 },
            static value => Result<string, FakeError>.Success($"t{value}"));
        System.Console.WriteLine(string.Join(",", traversed.Value));

        var sequenced = Result.Sequence(new[]
        {
            Result<int, FakeError>.Success(4),
            Result<int, FakeError>.Success(5),
            Result<int, FakeError>.Success(6),
        });
        System.Console.WriteLine(string.Join(",", sequenced.Value));

        var rawJson = JsonSerializer.Serialize(Result<int, FakeError>.Success(55));
        var rawRoundtrip = JsonSerializer.Deserialize<Result<int, FakeError>>(rawJson);
        System.Console.WriteLine($"raw json value = {rawRoundtrip.Value}");

        var aliasJson = JsonSerializer.Serialize(IntResult.Success(66));
        var aliasRoundtrip = JsonSerializer.Deserialize<IntResult>(aliasJson);
        System.Console.WriteLine($"alias json value = {aliasRoundtrip.Value}");

        var aliasFailureJson = JsonSerializer.Serialize(IntResult.Failure(new NotFound(777)));
        var aliasFailureRoundtrip = JsonSerializer.Deserialize<IntResult>(aliasFailureJson);
        var aliasFailureText = aliasFailureRoundtrip.Error.Match(
            notFound: notFound => $"alias json failure = {notFound.Id}",
            validation: validation => validation.Message,
            many: many => $"many {many.Errors.Count}");
        System.Console.WriteLine(aliasFailureText);

        IntResult.Success(123);

        return sync.IsSuccess
            && asyncResult.IsSuccess
            && crossAlias.IsSuccess
            && taskCrossAlias.IsSuccess
            && validationText == "validation value"
            && notFoundText == "missing id 404"
            && combinedText == "many 2"
            && all.IsSuccess
            && all.Value == (1, "two")
            && accumulated.IsFailure
            && accumulatedText == "combined many 2"
            && successfulCombine.IsSuccess
            && successfulCombine.Value.SequenceEqual([1, 2])
            && traversed.IsSuccess
            && traversed.Value.SequenceEqual(["t1", "t2", "t3"])
            && sequenced.IsSuccess
            && sequenced.Value.SequenceEqual([4, 5, 6])
            && rawRoundtrip.IsSuccess
            && rawRoundtrip.Value == 55
            && aliasRoundtrip.IsSuccess
            && aliasRoundtrip.Value == 66
            && aliasFailureRoundtrip.IsFailure
            && aliasFailureText == "alias json failure = 777"
                ? 0
                : 1;
    }
}
'@ | Set-Content -Path 'Program.cs' -Encoding UTF8

Write-Host "==> Adding FuncyTown $Version from local feed"
dotnet add package FuncyTown --version $Version --source $Artifacts
Exit-OnLastNativeFailure

Write-Host "==> Building consumer"
$BuildLog = Join-Path $Work 'build.log'
$BuildOutput = dotnet build --configuration Release 2>&1
$BuildOutput | Set-Content -Path $BuildLog -Encoding UTF8
$BuildOutput | ForEach-Object { Write-Host $_ }
Exit-OnLastNativeFailure
if (-not (($BuildOutput | Out-String) -match 'FT0001')) {
    throw 'Expected FT0001 analyzer warning in consumer build output'
}

Write-Host "==> Running consumer"
dotnet run --configuration Release
Exit-OnLastNativeFailure

Write-Host "==> Smoke test passed"
