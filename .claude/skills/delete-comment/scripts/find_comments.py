import json, os, sys
from pathlib import Path

ROOTS = ("lib", "app", "test", ".github")
ROOT_FILES = ("Directory.Build.props",)
SKIPPED_DIRS = {"bin", "obj", "node_modules"}
SLASH = {".cs", ".js", ".css", ".razor"}
HASH = {".ps1", ".sh", ".yml", ".yaml"}
XML = {".csproj", ".props"}


def scanned_files(root):
    for f in ROOT_FILES:
        yield f
    for top in ROOTS:
        for dirpath, dirnames, filenames in os.walk(root / top):
            dirnames[:] = [d for d in dirnames if d not in SKIPPED_DIRS]
            for name in filenames:
                rel = (Path(dirpath) / name).relative_to(root).as_posix()
                if Path(name).suffix in SLASH | HASH | XML and "/wwwroot/lib/" not in "/" + rel:
                    yield rel


def count_newlines(text, i, j):
    return text.count("\n", i, j)


def find_slash(text, razor):
    out, i, n, line = [], 0, len(text), 1

    def delimited(i, close, line):
        j = text.find(close, i + 2)
        j = n if j < 0 else j + len(close)
        nl = count_newlines(text, i, j)
        out.append((line, line + nl, text[i:j]))
        return j, line + nl

    while i < n:
        c = text[i]
        if c == "\n":
            line += 1; i += 1
        elif razor and text.startswith("@*", i):
            i, line = delimited(i, "*@", line)
        elif razor and text.startswith("<!--", i):
            i, line = delimited(i, "-->", line)
        elif text.startswith("//", i):
            if razor and i > 0 and text[i - 1] == ":":
                i += 2; continue
            j = text.find("\n", i); j = n if j < 0 else j
            out.append((line, line, text[i:j])); i = j
        elif text.startswith("/*", i):
            i, line = delimited(i, "*/", line)
        elif text.startswith('"""', i):
            q = 0
            while i + q < n and text[i + q] == '"':
                q += 1
            j = text.find('"' * q, i + q); j = n if j < 0 else j + q
            line += count_newlines(text, i, j); i = j
        elif c in '"`' or (c == "'" and not razor):
            verbatim = i > 0 and text[i - 1] == "@"
            j = i + 1
            while j < n:
                if text[j] == "\\" and not verbatim:
                    j += 2; continue
                if text[j] == c:
                    if verbatim and j + 1 < n and text[j + 1] == c:
                        j += 2; continue
                    break
                if text[j] == "\n" and c != "`" and not verbatim:
                    break
                j += 1
            j = min(j, n)
            line += count_newlines(text, i, j); i = j + 1
        else:
            i += 1
    return out


def find_hash(text):
    out = []
    for k, l in enumerate(text.split("\n"), 1):
        t = l.lstrip()
        if t.startswith("#") and not t.startswith("#!"):
            out.append((k, k, t))
    return out


def find_xml(text):
    out, i, line = [], 0, 1
    while True:
        j = text.find("<!--", i)
        if j < 0:
            return out
        line += count_newlines(text, i, j)
        k = text.find("-->", j); k = len(text) if k < 0 else k + 3
        nl = count_newlines(text, j, k)
        out.append((line, line + nl, text[j:k])); line += nl; i = k


def find(rel, text):
    ext = Path(rel).suffix
    raw = find_hash(text) if ext in HASH else find_xml(text) if ext in XML else find_slash(text, ext == ".razor")
    lines = text.split("\n")
    blocks = []
    for s, e, body in raw:
        own = lines[s - 1].lstrip().startswith(body.lstrip()[:2])
        line_comment = body.startswith(("//", "#"))
        if blocks and own and line_comment and blocks[-1]["own_line"] and blocks[-1]["end"] == s - 1 and blocks[-1]["text"].startswith(("//", "#")):
            blocks[-1]["end"] = e; blocks[-1]["text"] += "\n" + body.strip(); continue
        blocks.append({"file": rel, "start": s, "end": e, "own_line": own, "text": body.strip()})
    for b in blocks:
        b["lines_shed"] = b["end"] - b["start"] + 1 if b["own_line"] else 0
    return blocks


def main():
    root = Path(sys.argv[1]).resolve()
    only = [a.replace("\\", "/").rstrip("/") for a in sys.argv[3:]]
    rows = []
    for rel in scanned_files(root):
        if only and not any(rel == o or rel.startswith(o + "/") for o in only):
            continue
        text = (root / rel).read_bytes().decode("utf-8-sig", errors="replace").replace("\r\n", "\n")
        rows.extend(find(rel, text))
    for i, r in enumerate(rows):
        r["id"] = i
    json.dump(rows, open(sys.argv[2], "w", encoding="utf-8"), indent=1)
    print(f"{len(rows)} comments in {len({r['file'] for r in rows})} files, {sum(r['lines_shed'] for r in rows)} comment-only lines")


if __name__ == "__main__":
    main()
