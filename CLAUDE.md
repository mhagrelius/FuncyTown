# CLAUDE.md

Onboarding for agentic contributors lives in [`AGENTS.md`](AGENTS.md). That file is vendor-neutral and authoritative; everything below is just Claude-Code-specific glue.

## Claude Code specifics

- **Skills**: when applicable, use the `superpowers:*` skills your harness loads automatically — particularly `superpowers:test-driven-development`, `superpowers:writing-plans`, `superpowers:executing-plans` or `superpowers:subagent-driven-development`, `superpowers:systematic-debugging`, and `superpowers:verification-before-completion`. The conventions in `AGENTS.md` are the underlying processes; the skills are how you invoke them in this harness.
- **Plugins**: this repo has the `dotnet`, `dotnet-nuget`, and `dotnet-test` skill plugins enabled in `.claude/settings.json`. Prefer those over ad-hoc `dotnet` invocations when they apply.
- **Auto mode**: if the user enables auto mode, follow the standard auto-mode rules — execute autonomously, minimize interruptions, prefer action over planning, but still confirm before destructive or hard-to-reverse actions (especially `git push`, tag pushes that trigger NuGet publishing, and anything that modifies shared state).
- **`/schedule`**: when finishing work that has a clear future follow-up (e.g. "tag for release in 2 weeks"), offer to `/schedule` it.

For everything else — project context, tech stack, naming discipline, source-generator ground rules, git operations, common pitfalls — read [`AGENTS.md`](AGENTS.md).
