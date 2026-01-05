# Commit Instructions (Conventional Commits)

This repository requires **Conventional Commits** for all commit messages.

## Required format

Use the following structure:

```
<type>(<optional scope>)<optional !>: <description>

<optional body>

<optional footer(s)>
```

- **type**: required, lowercase
- **scope**: optional, lowercase (use a short component name)
- **!**: optional, indicates a breaking change
- **description**: required, imperative mood, no trailing period
- **body**: optional, explain *why* and *what* (not just *how*)
- **footers**: optional, for issue references or breaking change notes

## Allowed types

Use one of these types:

- `feat`: new feature
- `fix`: bug fix
- `docs`: documentation-only changes
- `chore`: maintenance (no production behavior change)
- `refactor`: code change that neither fixes a bug nor adds a feature
- `test`: adding or updating tests
- `ci`: CI/CD pipeline changes
- `build`: build system or dependencies
- `perf`: performance improvement
- `revert`: revert a previous commit

## Scopes

Scopes are optional, but recommended when it improves clarity.

Examples of reasonable scopes for this repo:

- `modules`
- `scripts`
- `environments`
- `container-apps`
- `keyvault`
- `frontdoor`

## Examples

- `docs: add commit message guidelines`
- `feat(modules): add container app env var output`
- `fix(scripts): handle missing resource group name`
- `refactor(container-apps): simplify variable wiring`
- `chore: bump terraform provider versions`
- `feat!: remove deprecated variables`

## Breaking changes

Indicate breaking changes in one of these ways:

- Add `!` after the type/scope: `feat!: remove deprecated variable`
- Or add a footer:

```
BREAKING CHANGE: <explain what changed and required user action>
```

## References

- Conventional Commits specification: https://www.conventionalcommits.org/
