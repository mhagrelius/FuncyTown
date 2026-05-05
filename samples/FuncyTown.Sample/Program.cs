namespace FuncyTown.Sample;

using System.Text.Json;

internal sealed record User(System.Guid Id, string Name);
internal sealed record Email(string Address);

[ErrorUnion]
internal partial class UserError;

[ErrorCase]
internal sealed partial class NotFound(System.Guid id) : UserError
{
    public System.Guid Id { get; } = id;

    public override string Message => $"User '{Id}' was not found";
}

[ErrorCase]
internal sealed partial class Validation(string field, string reason) : UserError
{
    public string Field { get; } = field;

    public string Reason { get; } = reason;

    public override string Message => $"{Field}: {Reason}";
}

[Result<User, UserError>]
internal readonly partial record struct UserResult;

[Result<Email, UserError>]
internal readonly partial record struct EmailResult;

[Result<UserError>]
internal readonly partial record struct DeleteUserResult;

internal static class Program
{
    private static readonly System.Guid ExistingUserId = System.Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly System.Guid MissingUserId = System.Guid.Parse("22222222-2222-2222-2222-222222222222");

    public static async Task<int> Main()
    {
        var syncPipelines = SyncPipelines();
        var asyncLoaded = await AsyncPipelineAsync().ConfigureAwait(false);

        return Math.Max(syncPipelines, asyncLoaded);
    }

    private static int SyncPipelines()
    {
        var ok = LoadUser(ExistingUserId)
            .OnSuccess(u => System.Console.WriteLine($"Loaded {u.Name}"))
            .OnFailure(e => System.Console.Error.WriteLine($"Failed: {DescribeError(e)}"))
            .Match(u => 0, _ => 1);

        var validation = LoadUser(System.Guid.Empty)
            .OnFailure(e => System.Console.Error.WriteLine($"Validation failed: {DescribeError(e)}"))
            .Match(
                onSuccess: _ => 1,
                onFailure: error => error.Match(
                    notFound: _ => 1,
                    validation: _ => 0,
                    many: _ => 1));

        var missing = LoadUser(MissingUserId)
            .OnFailure(e => System.Console.Error.WriteLine($"Lookup failed: {DescribeError(e)}"))
            .Match(
                onSuccess: _ => 1,
                onFailure: error => error.Match(
                    notFound: _ => 0,
                    validation: _ => 1,
                    many: _ => 1));

        var deleted = DeleteUser()
            .OnSuccess(() => System.Console.WriteLine("Deleted"))
            .OnFailure(e => System.Console.Error.WriteLine($"Delete failed: {e.Code}"))
            .Match(() => 0, _ => 1);

        var combined = Result.All(
                LoadUser(ExistingUserId).Match(
                    user => Result<User, UserError>.Success(user),
                    error => Result<User, UserError>.Failure(error)),
                Result<Email, UserError>.Success(new Email("sample.user@example.com")))
            .OnSuccess(pair => System.Console.WriteLine($"All: {pair.Item1.Name} <{pair.Item2.Address}>"))
            .OnFailure(e => System.Console.Error.WriteLine($"All failed: {DescribeError(e)}"))
            .Match(_ => 0, _ => 1);

        EmailResult email = LoadUser(ExistingUserId)
            .Then(u => EmailResult.Success(new Email($"{u.Name}@example.com")));

        var emailExitCode = email
            .OnSuccess(e => System.Console.WriteLine($"Email: {e.Address}"))
            .OnFailure(e => System.Console.Error.WriteLine($"Email failed: {DescribeError(e)}"))
            .Match(_ => 0, _ => 1);

        var jsonExitCode = JsonRoundTrip();

        return Max(ok, validation, missing, deleted, combined, emailExitCode, jsonExitCode);
    }

    private static int Max(params ReadOnlySpan<int> exitCodes)
    {
        var max = 0;
        foreach (var code in exitCodes)
        {
            if (code > max)
            {
                max = code;
            }
        }
        return max;
    }

    private static UserResult LoadUser(System.Guid id)
    {
        if (id == System.Guid.Empty)
        {
            return new Validation("id", "must not be empty");
        }

        if (id == MissingUserId)
        {
            return new NotFound(id);
        }

        return new User(id, "Sample User");
    }

    private static DeleteUserResult DeleteUser() => DeleteUserResult.Success();

    private static int JsonRoundTrip()
    {
        var original = LoadUser(ExistingUserId);
        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<UserResult>(json);

        return roundTripped
            .Ensure(
                u => u.Id == ExistingUserId && u.Name == "Sample User",
                new Validation("json", "round-tripped user payload did not match expected value"))
            .OnSuccess(u => System.Console.WriteLine($"JSON: {JsonSerializer.Serialize(u)}"))
            .OnFailure(e => System.Console.Error.WriteLine($"JSON failed: {DescribeError(e)}"))
            .Match(_ => 0, _ => 1);
    }

    private static async Task<int> AsyncPipelineAsync()
    {
        var exitCode = await LoadUserAsync(System.Guid.NewGuid())
            .Then(async u =>
            {
                await Task.Yield();
                return UserResult.Success(u with { Name = u.Name.ToUpperInvariant() });
            })
            .OnSuccess(async u =>
            {
                await System.Console.Out.WriteLineAsync($"Async loaded: {u.Name}").ConfigureAwait(false);
            })
            .OnFailure(async e =>
            {
                await System.Console.Error.WriteLineAsync($"Async failed: {DescribeError(e)}").ConfigureAwait(false);
            })
            .Match(
                async _ =>
                {
                    await Task.Yield();
                    return 0;
                },
                async _ =>
                {
                    await Task.Yield();
                    return 1;
                })
            .ConfigureAwait(false);

        return exitCode;
    }

    private static async Task<UserResult> LoadUserAsync(System.Guid id)
    {
        await Task.Yield();

        if (id == System.Guid.Empty)
        {
            return new Validation("id", "must not be empty");
        }

        return new User(id, "Async User");
    }

    private static string DescribeError(UserError error)
    {
        return error.Match(
            notFound: notFound => $"{notFound.Code}: {notFound.Message}",
            validation: validation => $"{validation.Code}: {validation.Message}",
            many: many => string.Join("; ", many.Errors.Select(DescribeError)));
    }
}
