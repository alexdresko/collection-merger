
## Commit messages

All commits in this repository must follow Conventional Commits.

See: `docs/commit-instructions.md`

## C# file organization

- One type per file: do not put multiple classes in the same `.cs` file.
- Applies to both production code (`src/`) and tests (`test/`).
- If helper/test-only types are needed, place each helper class in its own file under the test project.

