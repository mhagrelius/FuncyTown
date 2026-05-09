# Naming Equivalence

FuncyTown intentionally supports several familiar names for the same operation. The canonical name is the one used in docs when there is no migration reason to prefer another spelling.

Tap-style names never transform values. `OnSuccess`, `OnFailure`, `Tap`, `TapError`, `IfSuccessful`, and `IfFailed` run side effects and return the same Result shape that entered the method.

## Sync Methods

| Semantic group | Canonical name | Aliases | What it does |
|---|---|---|---|
| Transform | `Map` | `Transform`, `Select` | Changes the success value; failure passes through. |
| Transform | `MapTry` | none | Changes the success value with an exception-throwing function; caught exceptions become failures via `IExceptionalError<TSelf>`. |
| Chain | `Bind` | `Then`, `AndThen`, `SelectMany` | Calls a function that returns another Result; failure passes through. |
| Chain | `ThenTry` | none | Calls an exception-throwing function that returns another Result; caught exceptions become failures via `IExceptionalError<TSelf>`. |
| Recovery | `MapError` | `TransformError` | Changes the error value; success passes through. |
| Recovery | `Recover` | `OrElse` | Replaces a failure with a success value. |
| Recovery | `RecoverWith` | `OrElseThen` | Replaces a failure with another Result. |
| Side-effect | `OnSuccess` | `IfSuccessful`, `Tap` | Runs an action for success and returns the same Result. |
| Side-effect | `OnFailure` | `IfFailed`, `TapError` | Runs an action for failure and returns the same Result. |
| Validation | `Ensure` | `Validate` | Requires a successful value to satisfy a predicate, otherwise returns the provided error. |
| Termination | `Match` | none | Collapses the Result into a non-Result value. |

## Async Methods

| Semantic group | Canonical name | Aliases | What it does |
|---|---|---|---|
| Transform | `MapAsync` | `TransformAsync`, `SelectAsync` | Awaits a success-value transform. |
| Transform | `MapTryAsync` | none | Awaits a success-value transform; caught exceptions become failures via `IExceptionalError<TSelf>`. |
| Chain | `BindAsync` | `ThenAsync`, `AndThenAsync`, `SelectManyAsync` | Awaits a Result-returning operation. |
| Chain | `ThenTryAsync` | none | Awaits a Result-returning operation; caught exceptions become failures via `IExceptionalError<TSelf>`. |
| Recovery | `MapErrorAsync` | `TransformErrorAsync` | Awaits an error transform. |
| Recovery | `RecoverAsync` | `OrElseAsync` | Awaits a fallback success value. |
| Recovery | `RecoverWithAsync` | `OrElseThenAsync` | Awaits a fallback Result. |
| Side-effect | `OnSuccessAsync` | `IfSuccessfulAsync`, `TapAsync` | Awaits a success side effect and returns the same Result. |
| Side-effect | `OnFailureAsync` | `IfFailedAsync`, `TapErrorAsync` | Awaits a failure side effect and returns the same Result. |
| Validation | `EnsureAsync` | `ValidateAsync` | Awaits a predicate and may turn success into failure. |
| Termination | `MatchAsync` | none | Awaits the selected branch and returns a non-Result value. |

## LINQ Names

`Select` and one-argument `SelectMany` are included as method-name equivalents for transform and chain semantics. They do not introduce a separate behavior model.

Multi-`from` LINQ query syntax requires the result-selector `SelectMany` shape and is not part of the v1 surface yet. Prefer direct `.Then(...)` / `.Bind(...)` chains for multi-step Result workflows.

## Alias Return Rules

Generated Result aliases return the alias type when the operation stays inside that alias. Operations that change the success type or error type return `Result<TNew, TError>` or `Result<T, TNewError>` unless a generated cross-alias overload exists for the target alias.
