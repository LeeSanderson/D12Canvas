# No tabstop of heading page

Status: resolved

The H1 on the default page has a tab stop, but its just a heading

![Screenshot](002-no-tabstop-of-heading-page.png)

Suggested 2026-10-15 11:27 from `/board/cc9d78d6-94fc-40b5-93a2-5d3e7a59f6e9`.

## Comments

Done by `.scratch/app-polish/issues/16-bootstrap-in-the-app.md`. The heading was never a Tab stop. `FocusOnNavigate` focuses it from script, and Chromium drew a focus ring on that. `h1:focus { outline: none; }` removes the ring. Focus still moves to the heading on navigation, so screen readers announce the page.
