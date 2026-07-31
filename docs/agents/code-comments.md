# Code Comments

Prefer self-documenting code — clear names, small focused methods — over comments. A comment earns its place only by capturing something the code itself can't: a hidden constraint, a subtle invariant, a workaround for a specific bug or browser/runtime quirk, an ordering or timing dependency, or behavior that would genuinely surprise a future reader. If deleting a comment would lose no information a careful reader couldn't get from the code, delete it.

## Never cite tickets or ADRs in comments

Don't write "Ticket 47: ...", "ADR 0008: ...", or similar attributions in source comments. That belongs in the commit message and PR description, not the code — it rots the moment the ticket closes, and ages badly as the code around it changes.

This matters more than usual in this repo: several components embed their `<style>` block directly in `.razor` markup rather than using `.razor.css` isolation, so anything written inside `<style>` — comments included — renders verbatim into the page and gets baked byte-for-byte into every Playwright visual-test `.verified.html` baseline that includes that component. Editing a single CSS comment can invalidate dozens of screenshot baselines with no functional change behind it. See `.git log` for tickets 60/61 for a real instance of this.

## Style blocks and CSS

Zero comments in `<style>` blocks (inline in `.razor` markup or in `.razor.css`) and standalone `.css` files, full stop — not just ticket citations. Rely on clear class names and rule grouping; if a stacking/ordering dependency is genuinely load-bearing, put the explanation in the commit message, not inline.

## Everywhere else

Same "why, not what" bar. Before adding a comment, check whether a better name or a small extracted method would make it unnecessary instead. When trimming an existing comment that cites a ticket but also carries real WHY content, keep the substance and drop only the citation — don't delete a comment just because it happens to mention a ticket in passing.
