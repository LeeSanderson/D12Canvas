# D12Canvas

*** IMPORTANT: *** Always load the `/unslop` skill to ensure communication between agent and humans is clear

## Agent skills

### Issue tracker

Issues and specs live as markdown files under `.scratch/<feature-slug>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Default label vocabulary (`needs-triage`, `needs-info`, `ready-for-agent`, `ready-for-human`, `wontfix`). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout — `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.

### Code comments

Self-documenting code over comments; never cite tickets/ADRs in source; zero comments in CSS/style blocks. See `docs/agents/code-comments.md`.

### Testing

Build/test/visual-test CLI reference. Before committing any change that touches rendered markup or a shared `<style>` block, run the full Playwright visual-test suite (inside the pinned Docker image, `-parallel none`) and fold in any baseline updates — regardless of whether the change came from `/implement` or a direct prompt. See `docs/agents/testing.md`.
