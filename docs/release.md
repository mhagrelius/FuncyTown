# Releasing FuncyTown

The release workflow ([`.github/workflows/release.yml`](../.github/workflows/release.yml))
fires on every `v*` tag push. It builds, tests, packs, smoke-tests the meta-package,
then publishes to **nuget.org** via OIDC trusted publishing and cuts a GitHub release.

This document covers the one-time setup the maintainer must perform on
nuget.org and on GitHub before the workflow can publish.

## One-time setup

### 1. Create a "trusted publisher" entry on nuget.org

1. Sign in to <https://www.nuget.org> as the account that owns the `FuncyTown` /
   `FuncyTown.Abstractions` / `FuncyTown.Generators` / `FuncyTown.Analyzers`
   prefixes.
2. Go to your account settings → **Trusted Publishers**:
   <https://www.nuget.org/account/trustedPublishers>
3. Click **Add new** and configure:

   | Field | Value |
   |---|---|
   | Publisher type | GitHub Actions |
   | Repository owner | `mhagrelius` |
   | Repository name | `FuncyTown` |
   | Workflow file | `release.yml` |
   | Environment | `nuget-publish` |

4. Save. The trusted publisher applies to **all** packages owned by your account
   (or scope it to specific package IDs / prefixes if you prefer).

After this step, the `NuGet/login` action in the release workflow can exchange
the GitHub Actions OIDC token for a short-lived nuget.org API key — no
long-lived `NUGET_API_KEY` secret required.

### 2. Configure the `nuget-publish` GitHub Environment

The release workflow is gated on a protected GitHub Environment so that every
publish requires explicit maintainer approval.

1. Go to <https://github.com/mhagrelius/FuncyTown/settings/environments>.
2. Click **New environment**, name it `nuget-publish`.
3. Under **Deployment protection rules**, enable **Required reviewers** and
   add yourself (and any other authorized maintainers).
4. Optionally set a **Wait timer** (e.g. 0 minutes — manual approval is the gate)
   and a **Deployment branch policy** restricted to `main` and `v*` tags.

When a tag is pushed, the workflow pauses at the `publish` job until a
reviewer approves it from the Actions UI.

### 3. (Optional) Remove the old `NUGET_API_KEY` repo secret

Once trusted publishing is verified, delete the `NUGET_API_KEY` repository
secret to eliminate any chance of an accidental fallback to long-lived keys.

## Cutting a release

1. Update [`CHANGELOG.md`](../CHANGELOG.md): move the contents of `[Unreleased]`
   under a new `[X.Y.Z]` heading, update the comparison link refs at the bottom.
2. Promote new public surface from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt`
   in `src/FuncyTown.Abstractions`.
3. Promote new diagnostic descriptors from `AnalyzerReleases.Unshipped.md` to
   `AnalyzerReleases.Shipped.md` in `src/FuncyTown.Analyzers` and
   `src/FuncyTown.Generators`.
4. Commit those changes.
5. Tag the commit:

   ```bash
   git tag v0.X.Y
   ```

6. Push the tag — but only after explicit user confirmation (the tag push
   triggers the publish workflow):

   ```bash
   git push origin v0.X.Y
   ```

7. Approve the `nuget-publish` environment in the Actions UI.
8. Verify the package appears at <https://www.nuget.org/packages/FuncyTown>.

## Versioning scheme

Versions are computed from git tags via [MinVer](https://github.com/adamralph/minver):

- Tag format: `v<Major>.<Minor>.<Patch>[-<prerelease>]`, e.g. `v0.5.0-beta.1`,
  `v1.0.0`.
- Untagged commits are stamped `vNext-alpha.0.<height>` automatically.
- Pre-1.0 we ship `*-alpha.*` and `*-beta.*` suffixes.
- v1.0.0 will be the first stable release.

See [`Directory.Build.props`](../Directory.Build.props) for the MinVer config
(`MinVerTagPrefix=v`, `MinVerDefaultPreReleaseIdentifiers=alpha.0`).
