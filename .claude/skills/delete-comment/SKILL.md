---
name: delete-comment
description: Find the code comments left in this repo, sort each into tautology, stale, excuse or decision record, delete them, move decisions into ADRs, file issues for the hacks they excused, and tighten comment-budget.json. Use when the user says /delete-comment, asks to delete or burn down comments, or a commit trips the comment budget test.
---

# Delete comments

Comments are banned in this repo's source. `comment-budget.json` at the repo root lists the files that still have some, with a count per file. `CommentBudgetTests` in `test/D12Canvas.Tests/CommentBudget` fails when a file has more comments than its entry (a file with no entry gets zero), and fails again when a file has fewer, so the budget only ever goes down.

Scope is what the budget test scans: `lib/`, `app/`, `test/`, `.github/` and `Directory.Build.props`, in `.cs`, `.razor`, `.js`, `.css`, `.ps1`, `.sh`, `.yml`, `.csproj` and `.props` files, minus `bin/`, `obj/` and vendored `wwwroot/lib/`.

The user may name a folder or file. Otherwise work on everything in the budget.

## 1. List the comments

```
python -I .claude/skills/delete-comment/scripts/find_comments.py . <scratch>/comments.json [path ...]
```

Pass paths to narrow it. Each row has `id`, `file`, `start`, `end`, `own_line` and `text`. Consecutive `//` or `#` lines come back as one row.

## 2. Classify every row

Open the file around each comment. Pick one:

- **Tautology.** It says what the code, a member name or a test name already says. `// Arrange`, `// redo` and line-by-line narration land here.
- **Stale.** It describes code, names or behaviour that no longer match. Check before you call it current.
- **Excuse.** It explains away a hack: "for now", "workaround", "simpler than", TODO, duplicated logic. The hack should be fixed.
- **Decision record.** It holds a decision, invariant, browser quirk, ordering dependency or reason that the code alone doesn't show.
- **NotAComment.** The scanner misread a string or URL. Leave it alone.

Before calling something a decision record worth keeping, search `docs/adr/` and `CONTEXT.md`. If either already says it, the comment can go without a new ADR.

Write the result as a JSON array, one object per row: `{"id": 3, "category": "Excuse", "reason": "...", "existing": null, "draft": null, "issue": {"title": "...", "body": "..."}}`. More than about 150 rows: split by folder and hand batches to subagents with this file as the brief. Tell them not to edit repo files.

## 3. Act on each category

- **Tautology and stale:** delete.
- **Excuse:** delete, and file an issue under `.scratch/comment-cleanup/issues/NN-<slug>.md` (next free number) with `Status: ready-for-agent`, where the hack is, and what fixing it means. See `docs/agents/issue-tracker.md`.
- **Decision record:** delete, and write it into an ADR. Group related comments into one ADR per subject, never one per comment. Use the next free number in `docs/adr/` and copy the shape of the recent ADRs. If an existing ADR covers the subject, add an addendum to it instead. Never cite tickets.

Delete with:

```
python -I .claude/skills/delete-comment/scripts/apply_deletions.py . <scratch>/comments.json <scratch>/classified.json [more classified files]
```

It removes whole-line comments and their leftover blank lines, trims trailing comments off code lines, keeps line endings and BOMs, and prints anything it couldn't place. Fix those by hand.

## 4. Check and tighten

1. `dotnet csharpier .`, then `dotnet build`. The in-repo build runs `csharpier --check`.
2. Build `test/D12Canvas.Tests`, then tighten the budget:
   ```
   $env:D12_TIGHTEN_COMMENT_BUDGET='1'; ./test/D12Canvas.Tests/bin/Debug/net10.0/D12Canvas.Tests.exe -class D12Canvas.Tests.CommentBudget.CommentBudgetTests
   ```
   Then run the full bUnit suite without the variable.
3. If you touched a `.razor` file's markup or a `<style>` block, run the Playwright visual suite as `docs/agents/testing.md` says before committing.
4. `git diff --stat` and read a sample of the diff. The deletion script works by line numbers; a wrong line means a wrong delete.

Commit to `main` in one commit: deletions, ADRs, issues and the tightened `comment-budget.json` together.

## Rules

- Never raise a number in `comment-budget.json` or add a file to it. If a new comment seems necessary, it belongs in an ADR, an issue or a better name.
- A comment inside a `<style>` block or a `.css` file is always a delete. The reasoning goes in the commit message.
