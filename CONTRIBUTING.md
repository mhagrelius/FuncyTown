# Contributing to FuncyTown

Thanks for your interest in contributing! This document covers the basics.

## Getting started

```bash
git clone https://github.com/mhagrelius/FuncyTown.git
cd FuncyTown
dotnet build FuncyTown.sln --configuration Release
dotnet test FuncyTown.sln --configuration Release --no-build
```

You'll need the .NET 10 SDK pinned in [`global.json`](global.json).

## Working in this repo

Conventions are documented in [`AGENTS.md`](AGENTS.md). The short version:

- **TDD always.** Write the failing test first, run it, see it fail, write the minimal code, see it pass, commit.
- **Frequent small commits.** One logical change per commit. Imperative-mood subject lines.
- **Snapshot tests for source generators.** Use `Verify.SourceGenerators`. First-run failures are expected — inspect the `*.received.cs` file, confirm the output is right, then promote to `*.verified.cs`.
- **Run the full solution build before declaring a task done.** A test passing in isolation doesn't mean the rest of the solution still compiles.

## Naming discipline (please don't cross the line)

The whole library exists to keep these semantic groups distinct. Don't give a `Tap`-style name a transform signature, or a transform name a `Tap`-style signature:

| Group | Names | Behavior |
|---|---|---|
| Transform | `Map`, `Transform`, `Select` | Transform success value, error passes through |
| Chain | `Bind`, `Then`, `AndThen`, `SelectMany` | Chain another Result-returning operation |
| Recover | `MapError`/`TransformError`, `Recover`/`OrElse`, `RecoverWith`/`OrElseThen` | Transform or replace the error |
| Side-effect (Tap-style) | `OnSuccess`, `OnFailure`, `IfSuccessful`, `IfFailed`, `Tap`, `TapError` | Run an action; never change the value passing through |
| Validation | `Ensure`, `Validate` | Predicate that may turn success into failure |
| Termination | `Match` | Collapse Result into a non-Result |

## Public API changes

Every shipped project tracks public surface in `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. New public surface goes to `Unshipped` first; promote to `Shipped` only on release commits.

New analyzer or generator diagnostics go in `AnalyzerReleases.Unshipped.md` first.

## Pull requests

- Open an issue first for anything beyond a small fix or doc tweak.
- Keep PRs focused. If you discover an unrelated problem mid-task, file a follow-up issue rather than expanding scope.
- CI must be green before merge.

## Reporting security issues

Please follow [`SECURITY.md`](SECURITY.md) — don't open a public issue for vulnerability reports.

## License

By contributing, you agree your contributions are licensed under the [MIT License](LICENSE).
