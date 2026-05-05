# Migrating from LanguageExt

FuncyTown covers the `Result<T, TError>` style workflow. It is not a replacement for all LanguageExt types such as `Option`, `Either`, `Validation`, immutable collections, effects, or typeclass APIs.

## Basic Mapping

| If you wrote in LanguageExt | Write in FuncyTown |
|---|---|
| `Either<UserError, User>` | `Result<User, UserError>` or a generated `UserResult` alias |
| `Fin<User>` | `Result<User, UserError>` with your own error type |
| `Right<UserError, User>(user)` | `Result<User, UserError>.Success(user)` or `UserResult.Success(user)` |
| `Left<UserError, User>(error)` | `Result<User, UserError>.Failure(error)` or `UserResult.Failure(error)` |
| `either.IsRight` | `result.IsSuccess` |
| `either.IsLeft` | `result.IsFailure` |
| `either.Match(Right: ..., Left: ...)` | `result.Match(onSuccess: ..., onFailure: ...)` |
| `either.Map(f)` | `result.Map(f)` |
| `either.Bind(f)` | `result.Bind(f)` or `result.Then(f)` |
| `either.MapLeft(f)` | `result.MapError(f)` |
| `either.IfLeft(fallback)` | `result.Recover(error => fallback)` |

## Typical Before and After

```csharp
// LanguageExt
Either<UserError, User> LoadUser(Guid id) =>
    id == Guid.Empty
        ? Prelude.Left<UserError, User>(new InvalidUser("id"))
        : Prelude.Right<UserError, User>(new User(id, "Ada"));
```

```csharp
// FuncyTown
[Result<User, UserError>]
public readonly partial record struct UserResult;

UserResult LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return new InvalidUser("id");
    }

    return new User(id, "Ada");
}
```

## Chains

| If you wrote in LanguageExt | Write in FuncyTown |
|---|---|
| `LoadUser(id).Map(user => user.Name)` | `LoadUser(id).Map(user => user.Name)` |
| `LoadUser(id).Bind(SendEmail)` | `LoadUser(id).Bind(SendEmail)` or `LoadUser(id).Then(SendEmail)` |
| LINQ `from user in LoadUser(id) from email in LoadEmail(user) select email` | Use `LoadUser(id).Then(user => LoadEmail(user))` or the one-argument `SelectMany` method form. |
| `either.BiMap(left, right)` | Use `Map` for success and `MapError` for error in separate steps. |
| `either.Tap(...)` for side effects | `OnSuccess(...)` or `Tap(...)`; these never transform the success value. |

## Async

| If you wrote in LanguageExt | Write in FuncyTown |
|---|---|
| `Aff<Either<Error, T>>` or effect stacks | Use `Task<Result<T, TError>>`; FuncyTown does not model effects. |
| `await task.Map(...)` style chains | `await task.Map(...)`, `await task.Then(...)`, or the explicit `MapAsync` / `ThenAsync` forms. |
| `Bind` with async function | `Then(value => NextAsync(value))` on a `Task<Result<...>>` source, or `ThenAsync` on a Result value. |

## Validation

LanguageExt `Validation<Fail, Succ>` can accumulate failures as a first-class applicative type. FuncyTown's accumulation path is narrower:

| If you wrote in LanguageExt | Write in FuncyTown |
|---|---|
| `Validation<Error, User>` | Use individual Results and `Result.Combine(...)` when your error type implements `ICombinableError<E>`. |
| `Seq<Error>` failures | Use an `[ErrorUnion]`; the generated union implements `ICombinableError<E>` with a `Many` case. |
| Applicative validation over many fields | Return field-level Results, then combine them with `Result.Combine` or project with `Result.All` when short-circuiting is acceptable. |

## Notes

- FuncyTown uses `Result<T, TError>` order: success type first, error type second. `Either<L, R>` code usually maps `R` to success and `L` to error.
- FuncyTown does not provide LanguageExt's broad functional runtime. Keep LanguageExt where you need its collections, effects, or typeclasses.
- Generated aliases are the main ergonomic win. Prefer domain names such as `UserResult` over exposing raw `Result<User, UserError>` everywhere.
