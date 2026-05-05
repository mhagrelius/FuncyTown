# FuncyTown: Results Without the Generic Soup

Most .NET error handling conversations eventually land in the same place:
exceptions are too invisible for expected failures, but plain `Result<T, TError>`
can get noisy fast.

I wanted a Result library that made the happy path obvious, kept failures typed,
and still felt natural in real application code. FuncyTown is my answer to that:
a C# 14 / .NET 10 functional-programming library centered on `Result<T, TError>`,
with source generators that let each domain speak in its own Result types.

The goal is simple: keep the correctness of typed Results without making every
business workflow read like nested generic plumbing.

## The Problem I Was Trying To Fix

Exceptions are useful for exceptional conditions. They are not great as the
main modeling tool for expected business outcomes:

```csharp
public User LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        throw new ValidationException("id must not be empty");
    }

    var user = repository.Find(id);
    if (user is null)
    {
        throw new NotFoundException(id);
    }

    return user;
}
```

The method signature says "returns `User`", but the real contract is "returns a
user, or fails validation, or fails because the user was not found." Callers have
to know the hidden exception contract from documentation, convention, or pain.

Result types fix that by putting success and failure in the signature:

```csharp
public Result<User, UserError> LoadUser(Guid id)
```

That is better. But once a real workflow grows, raw Results can turn into generic
noise:

```csharp
Result<Email, UserError> result = LoadUser(id)
    .Bind(user => ValidateUser(user))
    .Bind(user => CreateEmail(user))
    .Map(email => Normalize(email));
```

This is correct, but it is still talking in infrastructure terms. The domain has
users and emails. The code has `Result<User, UserError>` and
`Result<Email, UserError>` everywhere.

I wanted the compiler-enforced safety of Result without making every method
advertise the generic machinery.

## FuncyTown's Core Move: Domain Result Aliases

With FuncyTown, you still have a real `Result<T, TError>` underneath. The
difference is that the public surface can use generated, domain-specific Result
aliases:

```csharp
using FuncyTown;

public sealed record User(Guid Id, string Name, bool IsActive);
public sealed record Email(string Value);

[ErrorUnion]
public partial class UserError;

[ErrorCase]
public sealed partial class NotFound(Guid id) : UserError
{
    public Guid Id { get; } = id;
    public override string Message => $"User {Id} was not found.";
}

[ErrorCase]
public sealed partial class Validation(string field, string reason) : UserError
{
    public string Field { get; } = field;
    public string Reason { get; } = reason;
    public override string Message => $"{Field}: {Reason}";
}

[Result<User, UserError>]
public readonly partial record struct UserResult;

[Result<Email, UserError>]
public readonly partial record struct EmailResult;
```

Those two small declarations generate the factory methods, conversions,
accessors, chain methods, async methods, JSON converters, and cross-alias
overloads.

Now application code can return and chain domain-named Results:

```csharp
public UserResult LoadUser(Guid id)
{
    if (id == Guid.Empty)
    {
        return new Validation("id", "must not be empty");
    }

    var user = repository.Find(id);
    if (user is null)
    {
        return new NotFound(id);
    }

    return user;
}
```

The signature is honest, but it is still readable. It says `UserResult`, not
`Result<User, UserError>`.

## The Chain Reads Like The Workflow

Because `UserResult` and `EmailResult` share the same `UserError`, FuncyTown
generates direct cross-alias chain methods:

```csharp
public EmailResult CreateEmail(User user)
{
    if (!user.IsActive)
    {
        return new Validation("user", "inactive users cannot receive email");
    }

    return new Email($"{user.Name}@example.com");
}

EmailResult result = LoadUser(id)
    .Then(CreateEmail);
```

There is no nested generic type in sight. The compiler still knows exactly what
is happening:

- `LoadUser` returns `UserResult`
- `CreateEmail` returns `EmailResult`
- both use `UserError`
- the chain is allowed because the error family is shared

If you try to chain into a different error type, that is no longer silently
blurred. You convert the error explicitly with `MapError`, which makes the domain
boundary visible.

## Names Mean One Thing

One of the design lines I cared about most was method naming.

In some Result-style APIs, names like `OnSuccess` can mean different things in
different contexts. Sometimes they transform. Sometimes they chain. Sometimes
they run a side effect. That makes fluent code easy to write but harder to trust.

FuncyTown keeps the names separated by behavior:

```csharp
var displayName = LoadUser(id)
    .Ensure(user => user.IsActive, new Validation("user", "inactive"))
    .Map(user => user.Name)
    .Recover(error => "anonymous");
```

`Map` transforms the success value.

```csharp
var email = LoadUser(id)
    .Then(CreateEmail);
```

`Then` chains another Result-returning operation.

```csharp
var audited = LoadUser(id)
    .OnSuccess(user => logger.LogInformation("Loaded {UserId}", user.Id))
    .OnFailure(error => logger.LogWarning("{Code}: {Message}", error.Code, error.Message));
```

`OnSuccess` and `OnFailure` are tap-style. They run side effects and return the
same Result. They do not transform the value. They do not replace the error.

That discipline is intentional. The code should tell you whether it is changing
the pipeline or merely observing it.

## Error Objects Become A Real Domain Model

FuncyTown does not force errors to be strings. The optional error-union generator
lets you model the set of failures for a domain:

```csharp
[ErrorUnion]
public partial class UserError;

[ErrorCase]
public sealed partial class NotFound(Guid id) : UserError
{
    public Guid Id { get; } = id;
}

[ErrorCase]
public sealed partial class Validation(string field, string reason) : UserError
{
    public string Field { get; } = field;
    public string Reason { get; } = reason;
}
```

Consumers can match on the outer Result, then match on the exact error case:

```csharp
string message = LoadUser(id).Match(
    onSuccess: user => $"Hello {user.Name}",
    onFailure: error => error.Match(
        notFound: e => $"No user exists for {e.Id}.",
        validation: e => $"{e.Field}: {e.Reason}",
        many: e => string.Join("; ", e.Errors.Select(x => x.Message))));
```

That is the difference between "something went wrong" and "this workflow can
fail in these known ways, and the compiler knows them too."

Generated error unions also get a synthetic `Many` case, which supports
validation-style accumulation:

```csharp
var result = Result.Combine(
    ValidateName(name),
    ValidateEmail(email),
    ValidateAge(age));

if (result.IsFailure)
{
    var message = result.Error.Match(
        notFound: e => e.Message,
        validation: e => $"{e.Field}: {e.Reason}",
        many: e => string.Join("; ", e.Errors.Select(x => x.Message)));
}
```

Use first-failure chains when later steps depend on earlier success. Use
`Combine` when independent validations should report all failures together.

## Async Stops Being A Tax On Readability

Async workflows are where many fluent APIs start to fall apart. You either get
inline `await` everywhere, or you end up with separate helper names that make the
pipeline harder to scan.

FuncyTown generates extension-block methods for `Task<YourResult>`:

```csharp
var emailText = await LoadUserAsync(id)
    .Then(user => LoadEmailAsync(user.Id))
    .OnSuccess(email => audit.Record(email.Value))
    .Match(
        email => email.Value,
        error => "unknown");
```

That is still an async pipeline. It is still typed. It still short-circuits on
failure. But it reads top-to-bottom like the synchronous version.

This was one of the experience goals from the start: async should not force
every Result workflow to become a punctuation puzzle.

## The Analyzer Catches The Easy Mistake

The easiest Result bug is also the most boring one:

```csharp
LoadUser(id);
```

That compiles in many APIs, but it probably means the caller ignored a possible
failure. FuncyTown ships analyzers with the package. The discarded Result above
is flagged as `FT0001`.

If the discard is intentional, make that visible:

```csharp
_ = LoadUser(id);
```

That tiny bit of ceremony matters. It separates "I forgot to handle this" from
"I am intentionally ignoring this."

The analyzer package also checks error-union rules, non-exhaustive matching
patterns, confusing implicit widening, and chains that cross error types without
an explicit `MapError`.

## JSON Works Like A Consumer Expects

Result values serialize with a small, predictable shape:

```json
{ "isSuccess": true, "value": { } }
```

or:

```json
{ "isSuccess": false, "error": { } }
```

Generated aliases are decorated with converters by default, so consumers do not
need to manually register a converter for every alias:

```csharp
var json = JsonSerializer.Serialize(UserResult.Success(user));
var roundtrip = JsonSerializer.Deserialize<UserResult>(json);
```

If a project wants to opt out of generated JSON support for an alias, it can:

```csharp
[Result<User, UserError>(Json = false)]
public readonly partial record struct UserResult;
```

## What Changes For The Consumer

The practical change is not that FuncyTown invents Result. It does not. The
change is that Result becomes easier to use as a normal application design tool.

Instead of this:

```csharp
Task<Result<Email, UserError>> SendWelcomeEmail(Guid id)
```

you can expose this:

```csharp
Task<EmailResult> SendWelcomeEmail(Guid id)
```

Instead of this:

```csharp
return await LoadUser(id)
    .Bind(user => Validate(user))
    .Bind(user => CreateEmail(user))
    .Bind(email => Send(email));
```

you can write this:

```csharp
return await LoadUserAsync(id)
    .Then(Validate)
    .Then(CreateEmail)
    .Then(Send)
    .OnFailure(LogUserError);
```

The generic Result is still there. The strong typing is still there. The error
model is still explicit. But the code now speaks in the language of the workflow.

## What FuncyTown Is Not Trying To Be

FuncyTown is not trying to be a full functional-programming universe. It is not
shipping `Option`, `Either`, reactive integration, or a validation DSL in v1.

It is focused on one thing:

> Make expected failure explicit, strongly typed, and pleasant enough that people
> will actually use it in everyday C# code.

That is the game-changer for me. The best error-handling model is not the one
with the most abstract power. It is the one teams can read, trust, and repeat
across a codebase without the style collapsing under real-world complexity.

FuncyTown makes Result feel like part of the domain instead of a generic wrapper
around it.
