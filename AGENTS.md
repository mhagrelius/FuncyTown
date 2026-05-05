# AGENTS.md — Onboarding for AI Coding Agents

This file orients any agentic contributor (Claude Code, GitHub Copilot CLI, Codex CLI, Gemini CLI, Cursor, Aider, etc.) working on FuncyTown. It tells you what the project is, where to find what you need, and how to operate productively here.

If your harness loads a vendor-specific file (e.g. `CLAUDE.md`, `GEMINI.md`), that file should point here. The contents of this file are authoritative.

## What this project is

A C# functional-programming library centered on a strongly-typed `Result<T, TError>`, differentiated by Roslyn source generators that emit per-domain Result aliases (so chained code never shows nested generics). Targets **.NET 10 / C# 14 only**. See [README.md](README.md) for the user-facing pitch.

## Current state

**Pre-1.0.** The runtime, generators, analyzers, code fixes, and meta-package are implemented and tested. Initial public release is `0.1.0`; nothing has shipped to nuget.org yet. The current focus is publishing `0.1.0` and iterating toward a stable `1.0`.

What lives where:

- [`src/FuncyTown.Abstractions/`](src/FuncyTown.Abstractions) — runtime: `Result<T, TError>`, async chain methods, combinators (`All`/`Combine`/`Traverse`/`Sequence`), `[Result<…>]`/`[ErrorUnion]`/`[ErrorCase]` attributes, `IError`/`ICombinableError<TSelf>`, `Unit`, `ResultException`, `System.Text.Json` converter factory.
- [`src/FuncyTown.Generators/`](src/FuncyTown.Generators) — `IIncrementalGenerator`s for typed Result aliases and error unions.
- [`src/FuncyTown.Analyzers/`](src/FuncyTown.Analyzers) — diagnostics `FT0001`–`FT0006`. Generators emit `FT0007`/`FT0008`.
- [`src/FuncyTown.Analyzers.CodeFixes/`](src/FuncyTown.Analyzers.CodeFixes) — code fixes for `FT0001` and `FT0006`.
- [`src/FuncyTown/`](src/FuncyTown) — meta-package bundling all of the above.
- [`tests/`](tests) — xUnit tests (runtime, generators, analyzers) including Verify-based snapshot tests.
- [`samples/FuncyTown.Sample/`](samples/FuncyTown.Sample) — runnable demo.
- [`scripts/pack-and-test.sh`](scripts/pack-and-test.sh) — packs the meta-package and consumes it from a throwaway project (also runs in CI).

If `git status` and `ls src/` disagree with this paragraph, it is out of date — trust the working tree, then update this file.

## Authoritative documents (read these before doing anything)

| Document | Purpose |
|---|---|
| [`README.md`](README.md) | User-facing pitch, install, quick start, naming-equivalence summary. |
| [`CHANGELOG.md`](CHANGELOG.md) | Per-release additions. Keep-a-Changelog format. |
| [`docs/naming-equivalence.md`](docs/naming-equivalence.md) | Sync and async chain method names by semantic group. |
| [`docs/migration-from-erroror.md`](docs/migration-from-erroror.md) / [`docs/migration-from-languageext.md`](docs/migration-from-languageext.md) | Migration guides for users coming from ErrorOr or LanguageExt. |
| [`docs/introducing-funcytown.md`](docs/introducing-funcytown.md) | Concept-and-rationale primer. |
| [`src/FuncyTown.Abstractions/PublicAPI.Shipped.txt`](src/FuncyTown.Abstractions/PublicAPI.Shipped.txt) | The shipped public surface. New surface goes to `PublicAPI.Unshipped.txt` first. |
| [`src/FuncyTown.Analyzers/AnalyzerReleases.Shipped.md`](src/FuncyTown.Analyzers/AnalyzerReleases.Shipped.md) and [`src/FuncyTown.Generators/AnalyzerReleases.Shipped.md`](src/FuncyTown.Generators/AnalyzerReleases.Shipped.md) | Diagnostic IDs, categories, severities. New rules go to `Unshipped` first. |

If the user asks you to add a feature or fix a bug, start by reading the relevant runtime / generator / analyzer source plus the matching test file, then drive a TDD cycle.

## Tech stack constraints

- **Runtime libraries**: `<TargetFramework>net10.0</TargetFramework>`, no multi-target. .NET 8 and 9 are out of scope.
- **Source generators / analyzers**: `<TargetFramework>netstandard2.0</TargetFramework>` — this is forced by the Roslyn analyzer host, not a choice.
- **Language**: `<LangVersion>latest</LangVersion>` (C# 14) on every project.
- **Nullable**: `<Nullable>enable</Nullable>` everywhere.
- **Warnings**: `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` on shipped projects. If a build is failing on warnings, fix the warning — do not suppress.
- **Package management**: Central Package Management (`Directory.Packages.props`). Add a version there, reference the package without a `Version=` in csprojs.
- **Public API tracking**: every shipped project has `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. New public surface goes to `Unshipped` first; the analyzer enforces this. Promote to `Shipped` only on release commits.

## C# 14 features the design uses (use them; don't avoid them)

| Feature | Where |
|---|---|
| Generic attributes (`[Result<T, TError>]`, `[Result<TError>]`) | Public attribute surface in `FuncyTown.Abstractions` |
| Primary constructors | `[ErrorCase]` user code in samples and the README |
| `params ReadOnlySpan<T>` | `Result.Combine(params ReadOnlySpan<…>)` (allocation-free varargs) |
| Extension blocks (`extension<T, TError>(Task<Result<T, TError>>) { … }`) | `ResultTaskExtensions` in `Result.Async.cs` so async chains read as if native |
| `static abstract` interface members | `ICombinableError<TSelf>.Combine(IReadOnlyList<TSelf>)` |
| `record struct` | `Result<T, TError>` itself |
| Target-typed `new()` and collection expressions | Sample code and tests |

Other C# 14 / preview features (`allows ref struct`, the `field` keyword, partial properties + partial constructors, `System.Threading.Lock`) are *fair game* — reach for them when they're the cleanest expression of the design — but the codebase does not currently require them. If you add them, mention it in the relevant CHANGELOG entry.

Don't reach for older patterns when the C# 14 form is clearer.

## How to operate in this repo

### Workflow conventions

1. **TDD always.** Write the failing test, run it, see it fail, write the minimal code, see it pass, commit. The plans encode this loop step by step — follow it.
2. **Frequent small commits.** One logical change per commit. The plans tell you exactly when to commit and what the message should be.
3. **No batched "fix everything" commits.** If you discover three unrelated issues while doing a task, fix one, commit, fix the next, commit.
4. **Snapshot tests for source generators.** Use `Verify.SourceGenerators`. First-run failures are expected — inspect the `*.received.cs` files, confirm the output is right, then promote to `*.verified.cs`.
5. **Run the full solution build before declaring a task done.** A test passing in isolation doesn't mean the rest of the solution still compiles.
6. **Verify before claiming done.** Run the actual test/build commands and read their output before reporting success. Evidence beats assertions.

### Source generator ground rules

- Source generators are `IIncrementalGenerator` only — never the deprecated `ISourceGenerator`.
- Use `ForAttributeWithMetadataName` for attribute-driven discovery, never raw syntax walking.
- Pipeline value types must be equatable (records, structs with proper equality). The generator framework caches based on equality; mutable or reference-equal models defeat caching.
- All emitted source files end in `.g.cs` and start with `// <auto-generated />` then `#nullable enable`.
- Use `global::` prefixes throughout emitted code to avoid ambiguity in consumer namespaces.

### Naming discipline (this is THE design's hard line)

The chain method names in `Result<T, TError>` and on every typed alias divide into semantic groups. Crossing these boundaries is the bug the library exists to prevent — do not do it:

| Group | Names | Behavior |
|---|---|---|
| Transform | `Map`, `Transform`, `Select` | Transform success value, error passes through |
| Chain | `Bind`, `Then`, `AndThen`, `SelectMany` | Chain another Result-returning operation |
| Recover | `MapError`/`TransformError`, `Recover`/`OrElse`, `RecoverWith`/`OrElseThen` | Transform or replace the error |
| Side-effect (Tap-style) | `OnSuccess`, `OnFailure`, `IfSuccessful`, `IfFailed`, `Tap`, `TapError` | Run an action; never change the value passing through |
| Validation | `Ensure`, `Validate` | Predicate that may turn success into failure |
| Termination | `Match` | Collapse Result into a non-Result |

Never give a `Tap`-style name a transform signature. Never give a transform name a `Tap`-style signature.

### When uncertain about scope

- **The spec is the contract.** If the user asks for something not in the spec, ask whether to update the spec first.
- **The plan is the execution contract within a phase.** If a task as written doesn't match reality (e.g. Phase 1 produced a different model name than Phase 2 assumed), fix the plan inline as part of `Task 0: Refinement`.
- **Don't expand scope mid-phase.** If you discover a thing that "really should also be done," note it and propose it as a follow-up — don't quietly add it.

### Git operations

- Commit as Matthew Hagrelius `<matthew@hagreli.us>` (already configured in `git config --global`).
- Commit message style: imperative mood, one-line summary, optional body. The implementation plans give exact suggested messages — use them.
- **Never push tags without explicit user confirmation** — tag pushes trigger NuGet.org publishing.
- **Never amend or force-push without explicit user confirmation.**
- **Never `git reset --hard` or otherwise discard work.** If you find unfamiliar files, investigate before deleting.
- **Never bypass commit hooks** (`--no-verify`, `--no-gpg-sign`, etc.) without explicit user confirmation. If a hook fails, fix the underlying issue.

### Working with the user

- This user prefers terse, action-oriented responses. Don't over-summarize what you did — they can read the diff.
- If your harness has an "auto" or "autonomous" mode signal, honor it — execute autonomously, minimize interruption questions, prefer action over planning.
- Confirm before destructive or hard-to-reverse actions (push, force-push, mass deletion, dropping tables, publishing packages).
- When you finish work that has a natural future follow-up (e.g. "tag for release in 2 weeks once X soaks"), offer to schedule it if your harness supports scheduled / recurring tasks.

## Common pitfalls

- **Don't multi-target.** This project is .NET 10 only. Don't add `<TargetFrameworks>net8.0;net9.0;net10.0</TargetFrameworks>` "for compatibility."
- **Don't skip `PublicAPI.Unshipped.txt` updates.** Build will fail with `RS0016` if you add public API without an entry.
- **Don't let `record struct` accidentally become `record class`.** The performance and identity story relies on struct semantics.
- **Don't accept Verify snapshots without inspecting them.** The whole point of snapshot tests is the human (or you, in this case) reads the generated code.
- **Don't use `LangVersion=preview`.** `latest` is correct — preview is a moving target.

## Tools and processes

These conventions hold regardless of which agent harness you're running under. The harness determines *how* you invoke them; the conventions don't change.

- **Test-driven development** — every behavior change starts with a failing test. The plans embed this; don't shortcut it.
- **Plan-then-execute** — for any task more than a few steps, write or read the plan first. The plans in `docs/superpowers/plans/` exist for exactly this reason.
- **Verification before completion** — before claiming any work is done, run the actual build and test commands and read the output. Evidence before assertions, always.
- **Systematic debugging** — when something fails and the cause isn't obvious, hypothesize, test the hypothesis, narrow down. Don't guess-and-check.
- **`dotnet` CLI** — for build, test, pack, publish. The CI workflow in `.github/workflows/ci.yml` is the authoritative reference for what commands and flags are expected.

If your harness has skills, plugins, or sub-agents that match these processes (e.g. Claude Code's `superpowers:*` skills, Cursor's rules), use them. The processes themselves are vendor-neutral.

## Harness-specific notes

If the user specifies they want behavior tailored to a specific harness, treat that as the highest-priority instruction. Otherwise default to the conventions above.

If your harness has its own onboarding-file convention, see if it points back here:
- Claude Code → `CLAUDE.md`
- Gemini CLI → `GEMINI.md`
- Codex CLI / Copilot CLI → `AGENTS.md` (this file)
- Cursor → `.cursor/rules/`

When in doubt, this file is the source of truth.

## When this file is wrong

If you discover that this file disagrees with the actual repo state, update it as part of the work you're doing. Onboarding docs that drift become worse than no docs.
