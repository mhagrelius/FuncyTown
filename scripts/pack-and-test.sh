#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ARTIFACTS="$REPO_ROOT/artifacts"
WORK="${TMPDIR:-/tmp}/funcytown-smoke-consumer"

rm -rf "$WORK"
mkdir -p "$ARTIFACTS"
rm -f "$ARTIFACTS"/*.nupkg "$ARTIFACTS"/*.snupkg

echo "==> Packing all projects to $ARTIFACTS"
dotnet pack "$REPO_ROOT/FuncyTown.sln" --configuration Release --output "$ARTIFACTS"

echo "==> Resolving package version"
PKG_VERSION=$(ls "$ARTIFACTS"/FuncyTown.[0-9]*.nupkg | head -1 | sed -E 's/.*FuncyTown\.([0-9].*)\.nupkg/\1/')
echo "    Version: $PKG_VERSION"

echo "==> Creating throwaway consumer in $WORK"
mkdir -p "$WORK"
export NUGET_PACKAGES="$WORK/.nuget"
export NUGET_HTTP_CACHE_PATH="$WORK/.nuget-http-cache"
cd "$WORK"
dotnet new console -n SmokeConsumer -f net10.0 --force >/dev/null
cd SmokeConsumer

cat > nuget.config <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="local" value="$ARTIFACTS" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
EOF

cat > Program.cs <<'EOF'
using System;
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

public sealed record AppError(string Code, string Message, Exception? Exception = null) : IExceptionalError<AppError>
{
    public static AppError FromException(Exception exception, string? code = null, string? message = null) =>
        new(code ?? exception.GetType().Name, message ?? exception.Message, exception);
}

[Result<string, AppError>]
public readonly partial record struct OperationResult;

[Result<int, AppError>]
public readonly partial record struct ParsedResult;

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

        var parsed = OperationResult.Success(" 41 ")
            .MapTry(static text => text.Trim(), code: "TrimFailed")
            .ThenTry(static text => ParsedResult.Success(int.Parse(text) + 1), code: "ParseFailed");
        System.Console.WriteLine($"parsed = {parsed.Value}");

        var parsedAsync = await OperationResult.Success(" 40 ")
            .MapTryAsync(static text => Task.FromResult(text.Trim()), code: "TrimFailed")
            .ThenTryAsync(static text => Task.FromResult(ParsedResult.Success(int.Parse(text) + 2)), code: "ParseFailed");
        System.Console.WriteLine($"parsed async = {parsedAsync.Value}");

        var parseFailure = OperationResult.Success("not an int")
            .ThenTry(static text => ParsedResult.Success(int.Parse(text)), code: "ParseFailed");
        System.Console.WriteLine($"parse failure = {parseFailure.Error.Code}");

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
            && parsed.IsSuccess
            && parsed.Value == 42
            && parsedAsync.IsSuccess
            && parsedAsync.Value == 42
            && parseFailure.IsFailure
            && parseFailure.Error.Code == "ParseFailed"
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
EOF

echo "==> Adding FuncyTown $PKG_VERSION from local feed"
dotnet add package FuncyTown --version "$PKG_VERSION" --source "$ARTIFACTS"

echo "==> Building consumer"
BUILD_LOG="$WORK/build.log"
dotnet build --configuration Release 2>&1 | tee "$BUILD_LOG"
if ! grep -q "FT0001" "$BUILD_LOG"; then
  echo "Expected FT0001 analyzer warning in consumer build output" >&2
  exit 1
fi

echo "==> Running consumer"
dotnet run --configuration Release

echo "==> Smoke test passed"
