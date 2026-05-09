# Changelog

All notable changes to FuncyTown are documented in this file. The format is
based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this
project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-alpha.5] - 2026-05-09

### Added

- `Error`, a basic `IError` implementation with `Code`, `Message`, optional
  non-serialized `Exception`, and `Error.FromException(...)` for simple failures,
  prototypes, and adapter boundaries.
- `IExceptionalError<TSelf>` plus `MapTry`/`ThenTry` and async variants for
  raw Results and generated aliases, for catching exceptions at adapter boundaries
  and converting them to Result failures.

## [0.1.0]

Initial public pre-release on nuget.org.

### Added

- `Result<T, TError>` runtime type with sync chain methods (`Map`/`Transform`/`Select`,
  `Bind`/`Then`/`AndThen`/`SelectMany`, `MapError`/`TransformError`,
  `Recover`/`OrElse`, `RecoverWith`/`OrElseThen`, `OnSuccess`/`OnFailure`/`Tap`/`TapError`/
  `IfSuccessful`/`IfFailed`, `Ensure`/`Validate`, `Match`).
- Async chain method family on `Result<T, TError>` (`MapAsync`, `BindAsync`, `MapErrorAsync`,
  `RecoverAsync`/`RecoverWithAsync`, `OnSuccessAsync`/`OnFailureAsync`, `EnsureAsync`,
  `MatchAsync`) plus `Task<Result<T, TError>>` extension-block chain methods so async
  pipelines never need inline `await` between steps.
- `Result.IsUninitialized` accessor for defensive code paths around `default(Result<>)`.
- `IError` and `ICombinableError<TSelf>` interfaces, `Unit` type, `ResultException`.
- `[Result<T, TError>]` and `[Result<TError>]` source-generated typed Result aliases:
  per-domain alias types with `Success`/`Failure` factories, `IsSuccess`/`IsFailure`/
  `Value`/`Error`, `Match`, the full chain method set (sync + async), `Task<Alias>`
  extension methods, implicit conversions from value/error and from `Result<T,TError>`
  (opt-out via `Implicit = false`), and `Kind = ResultKind.Class` for reference-semantics
  aliases.
- `[ErrorUnion]` / `[ErrorCase]` source-generated closed error hierarchies with
  per-case `Match`/`Switch`, default `Code`/`Message` properties, and a synthetic
  `Many` case for accumulated validation errors via `ICombinableError<TSelf>`.
- Cross-alias chain overloads — sibling aliases that share the same `TError` chain
  directly into each other.
- `Result.All<T1..T8>` heterogeneous tuple combinators; `Result.Combine` accumulating
  combinator (via `ICombinableError<TSelf>`); `Result.Traverse`/`TraverseAsync`,
  `Result.Sequence`/`SequenceAsync` collection combinators.
- `System.Text.Json` converter for `Result<T, TError>` and generated alias types,
  honoring `JsonNamingPolicy` and `PropertyNameCaseInsensitive` from the active
  `JsonSerializerOptions`. Alias-side converter emission is opt-out via
  `[Result<…>(Json = false)]`.
- Analyzers `FT0001`–`FT0006` shipping in the meta-package: discarded Result
  warning (with code-fix), chain-crosses-error-types info, non-exhaustive
  generated-union `Match` info, implicit-conversion-at-wide-site info,
  unattributed `[ErrorUnion]` subclass error, unsealed `[ErrorCase]` error
  (with code-fix that declines to swap `abstract` for `sealed` when descendants
  exist).
- Generator diagnostics `FT0007` (must-be-partial) and `FT0008`
  (file-local containing type unsupported) with stable help-link URIs to
  `docs/rules/FT00xx.md`.
- `FuncyTown` meta-package bundling `FuncyTown.Abstractions` runtime plus the
  Roslyn generators, analyzers, and code fixes under
  `analyzers/dotnet/roslyn5.3/cs/`.
- CI workflow (cross-OS pack/smoke/test, snupkg presence verification, optional
  SourceLink validation), release workflow (SHA-pinned actions, OIDC trusted
  publishing via `NuGet/login`, `nuget-publish` GitHub Environment for manual
  approval, snupkg verification, GitHub release notes).
- Sample project, end-to-end smoke-test scripts (`scripts/pack-and-test.sh` and
  `.ps1`), migration guides (from ErrorOr, from LanguageExt), naming-equivalence
  reference, per-rule documentation, contributor and security policies.

[Unreleased]: https://github.com/mhagrelius/FuncyTown/compare/v0.1.0-alpha.5...HEAD
[0.1.0-alpha.5]: https://github.com/mhagrelius/FuncyTown/compare/v0.1.0-alpha.4...v0.1.0-alpha.5
[0.1.0]: https://github.com/mhagrelius/FuncyTown/releases/tag/v0.1.0
