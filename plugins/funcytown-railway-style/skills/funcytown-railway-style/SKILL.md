---
name: funcytown-railway-style
description: Use when writing or reviewing C# code that consumes FuncyTown, generated Result aliases, typed Result workflows, railway-oriented programming, typed errors, fluent async Result pipelines, validation, recovery, combinators, or endpoint termination.
---

# FuncyTown Railway Style

## Core Rule

Write the workflow as a typed railway pipeline. Keep success moving forward, keep expected failures in the type system, observe with taps, and collapse the Result only at the boundary that must return something else.

FuncyTown code should read like the business process:

```csharp
public async Task<OrderReceiptResult> PlaceOrderAsync(PlaceOrder command) =>
    await ValidateCommand(command)
        .Then(LoadCustomerAsync)
        .Then(LoadCartAsync)
        .Then(ReserveInventoryAsync)
        .Then(ChargePaymentAsync)
        .Then(CreateReceiptAsync)
        .OnSuccess(receipt => metrics.RecordOrderPlaced(receipt.OrderId))
        .OnFailure(error => logger.LogWarning("{Code}: {Message}", error.Code, error.Message));
```

Prefer this shape over nested `if`, early `.Value`, exceptions for expected failures, or manually checking `IsSuccess` after every step.

## Result Shape

- Use generated aliases such as `CustomerResult`, `OrderResult`, and `PaymentResult` for public domain and application APIs.
- Use raw `Result<T, TError>` mainly inside low-level helpers, combinators, and generic utilities.
- Model expected failures with `[ErrorUnion]` and sealed `[ErrorCase]` types when the domain has known failure cases.
- Use simple `Error` or a custom `IError` only for prototypes, adapters, or boundaries without a stable domain error model.
- Keep exceptions at infrastructure boundaries. Convert them with `MapTry` / `ThenTry` only when the error type implements `IExceptionalError<TSelf>`.

## Method Semantics

| Need | Use | Do not use |
|---|---|---|
| Transform a success value | `Map` | `Then` or `OnSuccess` |
| Call another Result-returning step | `Then` / `Bind` | `Map` |
| Validate a success value | `Ensure` / `Validate` | `Map` returning errors |
| Change error family | `MapError` | implicit conversion or hidden adapter logic |
| Recover with a value | `Recover` | `MapError` |
| Recover with another Result | `RecoverWith` | nested `Match` inside the pipeline |
| Observe or log | `OnSuccess` / `OnFailure` | `Map`, `Then`, or mutation |
| End the railway | `Match` | `.Value`, `.Error`, or throwing |

Tap-style names never transform. `OnSuccess`, `OnFailure`, `Tap`, `TapError`, `IfSuccessful`, and `IfFailed` run side effects and return the same Result shape.

## Pipeline Pathways

### Linear Dependent Workflow

Use `Then` when each step depends on the prior success.

```csharp
public EmailResult BuildWelcomeEmail(Guid userId) =>
    LoadUser(userId)
        .Then(EnsureCanReceiveEmail)
        .Then(CreateEmail)
        .Then(NormalizeEmail);
```

### Success Transform

Use `Map` when the step cannot fail and only changes the success value.

```csharp
public Result<string, UserError> GetDisplayName(Guid id) =>
    LoadUser(id)
        .Map(user => user.Name.Trim())
        .Ensure(name => name.Length > 0, new InvalidUser("name", "must not be empty"));
```

### Validation Gate

Use `Ensure` for a predicate that can turn success into failure.

```csharp
public UserResult RequireActive(UserResult user) =>
    user.Ensure(
        value => value.IsActive,
        new UserInactive("Inactive users cannot continue."));
```

### Independent Validation Accumulation

Use `Result.Combine` when independent validations should report every failure. This requires an error type that can combine failures, such as a generated error union with `Many`.

```csharp
public ValidationResult ValidateRegistration(RegisterUser command) =>
    Result.Combine(
            ValidateName(command.Name),
            ValidateEmail(command.Email),
            ValidatePassword(command.Password))
        .Map(_ => Unit.Value);
```

### Independent Dependency Loading

Use `Result.All` when independent Results share an error type and the next step needs all success values. It stops at the first failure.

```csharp
public CheckoutResult StartCheckout(Guid userId) =>
    Result.All(
            LoadCustomer(userId),
            LoadCart(userId),
            LoadPricingRules())
        .Then(tuple => CreateCheckout(tuple.Item1, tuple.Item2, tuple.Item3));
```

### Collection Processing

Use `Traverse` when turning a sequence of inputs into Results. Use `Sequence` when the sequence already contains Results.

```csharp
public Result<IReadOnlyList<OrderLine>, OrderError> BuildLines(IReadOnlyList<CartLine> cartLines) =>
    Result.Traverse(cartLines, BuildOrderLine);

public Result<IReadOnlyList<OrderLine>, OrderError> CollectLines(IReadOnlyList<OrderLineResult> lines) =>
    Result.Sequence(lines.Select(line => line.Match(
        value => Result<OrderLine, OrderError>.Success(value),
        error => Result<OrderLine, OrderError>.Failure(error))));
```

If the alias conversion makes this noisy, keep the generic `Result<T, TError>` at the combinator boundary and return to aliases after the collection step.

### Async Service Orchestration

Task-returning Results have extension methods. Do not interrupt the railway with inline `await` after every call.

```csharp
public async Task<ShipmentResult> ShipOrderAsync(Guid orderId) =>
    await LoadOrderAsync(orderId)
        .Ensure(order => order.IsPaid, new OrderNotPaid(orderId))
        .Then(order => ReserveCarrierAsync(order))
        .Then(label => PersistShipmentAsync(label))
        .OnSuccess(label => audit.RecordShipment(label.OrderId))
        .OnFailure(error => audit.RecordShipmentFailure(orderId, error.Code));
```

### Adapter Boundary

Use `MapTry` or `ThenTry` at the edge where a throwing API enters the Result world.

```csharp
public DocumentResult ReadDocument(string path) =>
    PathResult.Success(path)
        .Ensure(File.Exists, new DocumentMissing(path))
        .MapTry(File.ReadAllText, code: "ReadFailed")
        .ThenTry(ParseDocument, code: "ParseFailed");
```

Do not put `Try` helpers around normal domain functions that already return Results.

### Cross-Domain Handoff

When moving between error families, make the boundary visible with `MapError`.

```csharp
public async Task<InvoiceResult> CreateInvoiceAsync(Guid orderId) =>
    await LoadOrderAsync(orderId)
        .MapError(OrderToBillingError)
        .Then(order => PriceOrderAsync(order))
        .Then(CreateInvoiceRecordAsync);
```

If two aliases share the same error family, chain directly. If they do not, convert explicitly.

### Recovery And Fallback

Use `Recover` to replace a failure with a success value. Use `RecoverWith` when fallback can also fail.

```csharp
public CustomerProfileResult LoadProfile(Guid customerId) =>
    LoadPreferredProfile(customerId)
        .RecoverWith(error => LoadBasicProfile(customerId))
        .OnFailure(error => logger.LogWarning("{Code}: {Message}", error.Code, error.Message));
```

Recovery should be a deliberate product decision, not a way to hide failures.

### Observability Taps

Use taps for logging, metrics, auditing, and tracing. They must not change the value passing through.

```csharp
public PaymentResult Charge(ChargeRequest request) =>
    ValidateCharge(request)
        .Then(paymentGateway.Authorize)
        .OnSuccess(payment => metrics.RecordPaymentAuthorized(payment.Id))
        .OnFailure(error => logger.LogWarning("{Code}: {Message}", error.Code, error.Message));
```

### Endpoint Or Controller Termination

Use `Match` at the outer boundary to convert the domain Result into HTTP, messages, commands, or UI state.

```csharp
public async Task<IResult> PostOrder(PlaceOrder command)
{
    var result = await orderService.PlaceOrderAsync(command);

    return result.Match(
        receipt => Results.Created($"/orders/{receipt.OrderId}", receipt),
        error => error.Match(
            validation: e => Results.BadRequest(e.Message),
            notFound: e => Results.NotFound(e.Message),
            paymentDeclined: e => Results.Conflict(e.Message),
            many: e => Results.BadRequest(e.Errors.Select(x => x.Message))));
}
```

Keep `Match` near the edge. A deep `Match` in the middle of business logic usually means the railway was broken too early.

### Exhaustive User-Facing Errors

Generated error unions support case matching. Prefer exhaustive error rendering over stringly typed status switches.

```csharp
public static string Describe(OrderError error) =>
    error.Match(
        validation: e => $"{e.Field}: {e.Reason}",
        notFound: e => $"Missing {e.Resource}.",
        paymentDeclined: e => e.Message,
        many: e => string.Join("; ", e.Errors.Select(Describe)));
```

## Good And Bad Shapes

<Good>

```csharp
public async Task<AccountSummaryResult> LoadSummaryAsync(Guid accountId) =>
    await ValidateAccountId(accountId)
        .Then(LoadAccountAsync)
        .Ensure(account => account.IsOpen, new AccountClosed(accountId))
        .Then(LoadBalancesAsync)
        .Map(BuildSummary)
        .OnFailure(error => logger.LogWarning("{Code}: {Message}", error.Code, error.Message));
```

The pipeline names show what changes the value, what can fail, and what only observes.

</Good>

<Bad>

```csharp
public async Task<AccountSummary> LoadSummaryAsync(Guid accountId)
{
    var account = await LoadAccountAsync(accountId);
    if (account.IsFailure)
    {
        throw new InvalidOperationException(account.Error.Message);
    }

    var balances = await LoadBalancesAsync(account.Value);
    return BuildSummary(balances.Value);
}
```

The method hides expected failures, unwraps early, throws for domain flow, and may read `balances.Value` without checking failure.

</Bad>

<Bad>

```csharp
var result = LoadUser(id)
    .OnSuccess(user => new UserDto(user.Id, user.Name));
```

`OnSuccess` is a tap, not a transform. Use `Map`.

</Bad>

<Good>

```csharp
var result = LoadUser(id)
    .Map(user => new UserDto(user.Id, user.Name));
```

</Good>

## Review Checklist

Before finalizing FuncyTown consumer code, check:

- Public domain/application methods return generated aliases, not raw generic Results, unless the generic form is intentionally clearer.
- Expected failures are values in the error model, not thrown exceptions.
- Every chain method name matches the semantic group.
- Taps do not mutate pipeline state or replace values.
- Error-family transitions use `MapError`.
- Collection and validation workflows use the static combinators where they make the flow clearer.
- Async code stays fluent until the final `await`.
- `Match` appears at boundaries, not throughout the domain workflow.
- No Result is discarded unless intentionally assigned to `_`.
