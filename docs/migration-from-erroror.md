# Migrating from ErrorOr

FuncyTown is not a drop-in replacement for ErrorOr. The main migration is from a general `ErrorOr<T>` shape to domain-specific aliases such as `UserResult` backed by `Result<User, UserError>`.

## Basic Mapping

| If you wrote in ErrorOr | Write in FuncyTown |
|---|---|
| `ErrorOr<User> LoadUser(Guid id)` | `UserResult LoadUser(Guid id)` with `[Result<User, UserError>]` |
| `return user;` | `return user;` when implicit success conversions are enabled, or `return UserResult.Success(user);` |
| `return Error.NotFound(...);` | `return new UserNotFound(id);` when the error case converts to the alias, or `return UserResult.Failure(new UserNotFound(id));` |
| `result.IsError` | `result.IsFailure` |
| `!result.IsError` | `result.IsSuccess` |
| `result.Value` | `result.Value` |
| `result.Errors` | Model one error value. Use an `[ErrorUnion]` and its generated `Many` case when you need accumulated errors. |
| `result.Match(onValue, onError)` | `result.Match(onSuccess, onFailure)` |
| `result.Then(next)` | `result.Then(next)` |
| `result.Else(fallback)` | `result.Recover(error => fallback)` or `result.OrElse(error => fallback)` |

## Typical Before and After

```csharp
// ErrorOr
ErrorOr<User> LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return Error.Validation("User.Id", "id must not be empty");
    }

    return new User(id, "Ada");
}
```

```csharp
// FuncyTown
[ErrorUnion]
public partial class UserError;

[ErrorCase]
public sealed partial class InvalidUser(string field, string reason) : UserError
{
    public string Field { get; } = field;
    public string Reason { get; } = reason;
    public override string Message => $"{Field}: {Reason}";
}

[Result<User, UserError>]
public readonly partial record struct UserResult;

UserResult LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return new InvalidUser("id", "id must not be empty");
    }

    return new User(id, "Ada");
}
```

## Chains

| If you wrote in ErrorOr | Write in FuncyTown |
|---|---|
| `LoadUser(id).Then(SendEmail)` | `LoadUser(id).Then(SendEmail)` |
| `LoadUser(id).Then(user => LoadProfile(user.Id))` | `LoadUser(id).Then(user => LoadProfile(user.Id))` |
| `LoadUser(id).Match(user => ..., errors => ...)` | `LoadUser(id).Match(user => ..., error => ...)` |
| `LoadUser(id).Switch(user => ..., errors => ...)` | Use `Match` for returning values; use `OnSuccess` / `OnFailure` for side effects. |

FuncyTown keeps one error value in each failed Result. For validation scenarios with many errors, use generated error unions plus `Result.Combine` so the error union's generated `Many` case carries the accumulated failures.

## Validation and Accumulation

| If you wrote in ErrorOr | Write in FuncyTown |
|---|---|
| `ErrorOr<User>.From(errors)` | Define an `[ErrorUnion]` and return its generated `Many` case when combining failures. |
| Multiple validation calls returning `ErrorOr<T>` | Return raw `Result<T, UserError>` values and combine them with `Result.Combine(...)`. |
| `result.FailIf(predicate, error)` | `result.Ensure(value => !predicate(value), error)` |

## Notes

- ErrorOr's built-in error categories are not mirrored. Model domain errors as explicit `[ErrorCase]` types.
- `Result.Combine` is defined for raw `Result<T, E>` inputs. If a migration is combinator-heavy, keep those validation steps as raw Results until alias-specific combinator helpers exist.
- FuncyTown analyzers expect Results to be observed. Assign intentional discards to `_`.
- Use `[Result<..., Implicit = false>]` if you want all success and failure construction to be explicit during migration.
