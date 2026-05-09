# FuncyTown

[![CI](https://github.com/mhagrelius/FuncyTown/actions/workflows/ci.yml/badge.svg)](https://github.com/mhagrelius/FuncyTown/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/vpre/FuncyTown.svg?logo=nuget)](https://www.nuget.org/packages/FuncyTown)
[![Downloads](https://img.shields.io/nuget/dt/FuncyTown.svg?logo=nuget)](https://www.nuget.org/packages/FuncyTown)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Strongly typed `Result<T, TError>` for C# 14 and .NET 10, with Roslyn generators that emit domain-specific Result aliases.

## Why FuncyTown

Plain `Result<T, TError>` gets noisy once real code starts chaining operations. FuncyTown keeps the runtime type explicit while letting public code speak in domain names:

```csharp
[Result<User, UserError>]
public readonly partial record struct UserResult;

UserResult result = LoadUser(id)
    .Ensure(user => user.IsActive, new UserInactive(id))
    .Then(SendWelcomeEmail)
    .OnFailure(LogUserError);
```

The library separates method names by semantics:

- Transform names (`Map`, `Transform`, `Select`) change success values.
- Chain names (`Bind`, `Then`, `AndThen`, `SelectMany`) call another Result-returning operation.
- Tap-style names (`OnSuccess`, `OnFailure`, `Tap`, `TapError`, `IfSuccessful`, `IfFailed`) run side effects and return the same value.

See the full [naming-equivalence table](docs/naming-equivalence.md).

## Install

Use the meta-package for normal app and library projects:

```bash
dotnet add package FuncyTown --prerelease
```

It brings in the runtime abstractions, source generators, analyzers, and code fixes. The package is prerelease until v1.0; after v1.0, use `dotnet add package FuncyTown`.

Consumer `.csproj` requirements:

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

`LangVersion` must be `latest` (or a specific 14-or-higher version) — generic attributes such as `[Result<T, TError>]` are unavailable on lower language levels. The runtime targets `net10.0` only.

## Agent Skill

FuncyTown includes an optional agent skill for writing consumer code in the intended railway style: [`funcytown-railway-style`](plugins/funcytown-railway-style/skills/funcytown-railway-style/SKILL.md).

The skill is packaged once and can be installed in a few ways:

- **Codex marketplace/plugin**: install the `funcytown-railway-style` plugin from this repo's [`marketplace.json`](.agents/plugins/marketplace.json). The plugin is skill-only and points at `plugins/funcytown-railway-style/skills/`.
- **Codex direct skill install**: install just `plugins/funcytown-railway-style/skills/funcytown-railway-style` from `mhagrelius/FuncyTown`.
- **Claude-compatible skill install**: use the same `plugins/funcytown-railway-style/skills/funcytown-railway-style` directory as the standalone skill folder, for example by copying it to `~/.claude/skills/funcytown-railway-style`.

The skill teaches generated Result aliases, typed error unions, fluent railway pipelines, async orchestration, validation, recovery, observability taps, combinators, and endpoint termination.

## Quick Start

```csharp
using FuncyTown;

public sealed record User(Guid Id, string Name);

[ErrorUnion]
public partial class UserError;

[ErrorCase]
public sealed partial class NotFound(Guid id) : UserError
{
    public Guid Id { get; } = id;
    public override string Message => $"User '{Id}' was not found.";
}

[ErrorCase]
public sealed partial class InvalidUser(string field, string reason) : UserError
{
    public string Field { get; } = field;
    public string Reason { get; } = reason;
    public override string Message => $"{Field}: {Reason}";
}

[Result<User, UserError>]
public readonly partial record struct UserResult;

public static UserResult LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return new InvalidUser("id", "must not be empty");
    }

    return new User(id, "Ada");
}

var name = LoadUser(id)
    .Map(user => user.Name)
    .Recover(error => "anonymous")
    .Value;
```

Generated aliases expose `Success`, `Failure`, `IsSuccess`, `IsFailure`, `Value`, `Error`, `Deconstruct`, `Match`, `ToString`, the chain method set, and implicit conversions from success/error values unless disabled with `[Result<User, UserError>(Implicit = false)]`.

For simple failures, prototypes, or adapter boundaries, FuncyTown also ships a basic `Error` implementation:

```csharp
[Result<int, Error>]
public readonly partial record struct OperationResult;

public static OperationResult Divide(int left, int right)
{
    if (right == 0)
    {
        return new Error("DivideByZero", "Cannot divide by zero.");
    }

    return left / right;
}

public static OperationResult TryRead()
{
    try
    {
        return ReadValue();
    }
    catch (IOException ex)
    {
        return Error.FromException(ex, code: "ReadFailed");
    }
}
```

`Error.Exception` is kept as local diagnostic context and is ignored by `System.Text.Json` serialization. Prefer domain-specific error unions for public domain APIs that need closed cases and exhaustive matching.

Error types can opt in to exception-catching chain helpers by implementing `IExceptionalError<TSelf>`:

```csharp
public sealed record AppError(string Code, string Message, Exception? Exception = null)
    : IExceptionalError<AppError>
{
    public static AppError FromException(Exception exception, string? code = null, string? message = null) =>
        new(code ?? exception.GetType().Name, message ?? exception.Message, exception);
}
```

`MapTry` and `ThenTry` are convenience boundary helpers for calling exception-throwing APIs from a Result pipeline. They catch exceptions thrown by the supplied delegate, including `OperationCanceledException`, and convert them through `TError.FromException(...)`.

```csharp
[Result<string, AppError>]
public readonly partial record struct OperationResult;

[Result<Document, AppError>]
public readonly partial record struct DocumentResult;

var result = OperationResult.Success(path)
    .MapTry(File.ReadAllText, code: "FileReadFailed")
    .ThenTry(ParseDocument, code: "ParseFailed");
```

Use normal `Map`/`Then` for domain logic that already returns Results. `Try`-style helpers are best kept at infrastructure or adapter boundaries.

## Why `public readonly partial record struct`

The default alias declaration is intentionally shaped like this:

```csharp
[Result<User, UserError>]
public readonly partial record struct UserResult;
```

Each keyword has a purpose:

- `public` makes the alias part of the consumer-facing domain API. Use `internal` for aliases that should stay inside one assembly.
- `readonly` keeps Result aliases immutable after construction; success/failure state, value, and error should not drift.
- `partial` is required because the source generator adds the generated members. Non-partial aliases report `FT0007`.
- `record struct` gives lightweight value semantics, generated equality/operators, and avoids a normal wrapper allocation for the common case.

If reference semantics are needed, opt in explicitly:

```csharp
[Result<User, UserError>(Kind = ResultKind.Class)]
public partial class UserResult;
```

## Escape Hatches And Opt-Outs

FuncyTown defaults are designed for the common path, but the generated surface has a few deliberate escape hatches:

| Default | Escape hatch | Use when |
|---|---|---|
| Alias is public API | Declare the alias `internal` | The Result type is only an implementation detail inside one assembly. |
| Alias is a `readonly record struct` | `[Result<T, TError>(Kind = ResultKind.Class)]` with `public partial class` | You need reference semantics or inheritance-oriented integration. |
| Success and error values convert implicitly | `[Result<T, TError>(Implicit = false)]` | You want all `Success(...)` / `Failure(...)` construction to be explicit. |
| Alias gets a generated JSON converter | `[Result<T, TError>(Json = false)]` | Serialization is owned by a custom converter or the alias should not be serialized directly. |
| Error unions generate closed case matching | Use any custom `TError` type instead of `[ErrorUnion]` | You already have an error model, enum, record, or external error type. |
| Domain aliases hide raw generic noise | Use `Result<T, TError>` directly | Low-level helpers, collection combinators, or internal pipelines are clearer with the raw runtime type. |
| Void-success aliases hide `Unit` | Use `[Result<TError>]` | The operation only needs success/failure, not a success value. |
| Chains require a shared `TError` | Call `MapError` before crossing error families | A workflow intentionally moves from one domain error model to another. |

## Naming Equivalence

| Semantic group | Canonical name | Aliases |
|---|---|---|
| Transform success value | `Map` | `Transform`, `Select` |
| Chain Result-returning operation | `Bind` | `Then`, `AndThen`, `SelectMany` |
| Transform error | `MapError` | `TransformError` |
| Recover with value | `Recover` | `OrElse` |
| Recover with Result | `RecoverWith` | `OrElseThen` |
| Side effect on success | `OnSuccess` | `IfSuccessful`, `Tap` |
| Side effect on failure | `OnFailure` | `IfFailed`, `TapError` |
| Validate success value | `Ensure` | `Validate` |
| Terminate Result pipeline | `Match` | none |

For exact sync and async naming, see [docs/naming-equivalence.md](docs/naming-equivalence.md).

## Async

`Result<T, TError>` exposes async variants such as `MapAsync`, `BindAsync`, `RecoverAsync`, `EnsureAsync`, and `MatchAsync`.

Task-producing Result values also get extension methods, so async chains do not need inline `await` between every step:

```csharp
var result = await LoadUserAsync(id)
    .Then(user => LoadProfileAsync(user.Id))
    .OnSuccess(profile => audit.Record(profile.Id))
    .Match(profile => profile.DisplayName, error => "unknown");
```

Generated aliases get the same async method family, returning the alias type where the operation remains in the same alias.

## Cross-Alias Chains

Aliases with the same error type can chain directly into each other:

```csharp
[Result<User, UserError>]
public readonly partial record struct UserResult;

[Result<Email, UserError>]
public readonly partial record struct EmailResult;

EmailResult email = LoadUser(id)
    .Then(user => EmailResult.Success(new Email($"{user.Name}@example.com")));
```

The generator emits overloads so sibling aliases in the same error family return the destination alias directly. If a chain crosses to a different error type, convert explicitly with `MapError` first.

## Error Unions

`[ErrorUnion]` and `[ErrorCase]` generate a closed error hierarchy with exhaustive `Match`/`Switch` methods, default `Code` and `Message` properties, and a generated `Many` case for accumulated validation errors:

```csharp
string message = error.Match(
    notFound: e => e.Message,
    invalidUser: e => e.Message,
    many: e => string.Join("; ", e.Errors.Select(x => x.Message)));
```

Every direct subclass of an error union must be marked `[ErrorCase]` and must be sealed.

## Combinators

`Result.All` combines heterogeneous Results with the same error type and returns a tuple. It stops at the first failure.

```csharp
Result<User, UserError> user = LoadUserResult(id);
Result<Settings, UserError> settings = LoadSettingsResult(id);

var loaded = Result.All(user, settings);
```

Generated aliases are for fluent domain chains. The static combinators operate on the raw `Result<T, TError>` runtime type.

`Result.Combine` combines homogeneous Results and accumulates every failure through `ICombinableError<E>`:

```csharp
var combined = Result.Combine(
    ValidateName(name),
    ValidateEmail(email));
```

`Result.Traverse`, `TraverseAsync`, `Sequence`, and `SequenceAsync` convert collections of values or Result-producing operations into one Result containing all success values, or the first failure.

## JSON

FuncyTown includes `System.Text.Json` support for `Result<T, TError>` and generated aliases. The JSON shape is:

```json
{ "isSuccess": true, "value": { } }
```

or:

```json
{ "isSuccess": false, "error": { } }
```

Aliases are decorated with generated converters by default. Disable alias converter emission with:

```csharp
[Result<User, UserError>(Json = false)]
public readonly partial record struct UserResult;
```

## Analyzers

The `FuncyTown` package includes these diagnostics:

| ID | Severity | Purpose |
|---|---:|---|
| `FT0001` | Warning | Result return value is discarded without being observed or explicitly assigned to `_`. |
| `FT0002` | Info | Result chain crosses error types without an explicit `MapError`. |
| `FT0003` | Info | Match over a generated error union is not exhaustive. |
| `FT0004` | Info | Result alias is implicitly converted at a wide-typed site such as `object`. |
| `FT0005` | Error | A subclass of an `[ErrorUnion]` base is missing `[ErrorCase]`. |
| `FT0006` | Error | An `[ErrorCase]` type is not sealed. |

Generator diagnostics `FT0007` and `FT0008` cover unsupported result declarations, including non-partial aliases and file-local containing types.

## Migrating

- [Migrating from ErrorOr](docs/migration-from-erroror.md)
- [Migrating from LanguageExt](docs/migration-from-languageext.md)

## Resources

- [Introducing FuncyTown](docs/introducing-funcytown.md) — concept-and-rationale primer.
- [Naming equivalence](docs/naming-equivalence.md) — sync and async chain method names by semantic group.
- [CHANGELOG](CHANGELOG.md) — per-release additions, Keep-a-Changelog format.
- [Sample project](samples/FuncyTown.Sample) — runnable demo of typed aliases, error unions, async chains, JSON.
- [Contributing](CONTRIBUTING.md) and [Security policy](SECURITY.md).

## Compatibility

FuncyTown runtime packages target `net10.0` only. Generators and analyzers target `netstandard2.0` because Roslyn loads analyzers through that target, but generated code is intended for C# 14 / .NET 10 projects.

Package versions are managed by git tags through MinVer. Public API is tracked with `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`.

## License

[MIT](LICENSE)
