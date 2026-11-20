# Code comments

No comments in source. That covers `.cs`, `.razor`, `.js`, `.css`, `.ps1`, `.sh`, `.yml`, `.csproj` and `.props` files under `lib/`, `app/`, `test/` and `.github/`, plus `Directory.Build.props`. Vendored files under `wwwroot/lib/` are exempt.

Where the content goes instead:

- What the code does: a better name or a smaller method.
- Why it does it, a constraint, a browser quirk, an ordering dependency: an ADR in `docs/adr/`, or `CONTEXT.md` for a domain term.
- A hack you know is a hack: an issue under `.scratch/`.
- The story of a change and the ticket it came from: the commit message.

## Enforcement

`CommentBudgetTests` in `test/D12Canvas.Tests/CommentBudget` scans those files. `comment-budget.json` at the repo root lists any file still allowed comments, with a count. A file with no entry is allowed none. The test fails when a file has more comments than its entry, and also when it has fewer, so the budget only goes down. To lower the budget after deleting comments, run the test class with `D12_TIGHTEN_COMMENT_BUDGET=1`. Never raise an entry or add one.

`/delete-comment` clears comments that remain and tightens the budget.
