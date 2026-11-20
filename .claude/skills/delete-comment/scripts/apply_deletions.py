import json, re, sys
from collections import defaultdict
from pathlib import Path

root = Path(sys.argv[1])
rows = {r["id"]: r for r in json.load(open(sys.argv[2], encoding="utf-8"))}
decisions = {}
for f in sys.argv[3:]:
    for d in json.load(open(f, encoding="utf-8")):
        decisions[d["id"]] = d

missing = set(rows) - set(decisions)
if missing:
    sys.exit(f"unclassified ids: {sorted(missing)[:20]} ({len(missing)})")

by_file = defaultdict(list)
for i, d in decisions.items():
    if d["category"] != "NotAComment":
        by_file[rows[i]["file"]].append(rows[i])

problems = []
for rel, comments in by_file.items():
    path = root / rel
    raw = path.read_bytes().decode("utf-8")
    bom = raw.startswith("﻿")
    if bom:
        raw = raw[1:]
    crlf = "\r\n" in raw
    lines = raw.replace("\r\n", "\n").split("\n")
    drop = set()
    for c in sorted(comments, key=lambda c: -c["start"]):
        s, e = c["start"] - 1, c["end"] - 1
        first = c["text"].split("\n")[0]
        if c["text"].startswith("//") and "\n" in c["text"]:
            for k in range(s, e + 1):
                if not lines[k].lstrip().startswith("//"):
                    problems.append(f"{rel}:{k+1} expected // line")
                drop.update(range(s, e + 1))
            continue
        block = "\n".join(lines[s:e + 1])
        idx = block.find(c["text"])
        if idx < 0:
            problems.append(f"{rel}:{c['start']} text not found")
            continue
        new = block[:idx].rstrip(" \t") + block[idx + len(c["text"]):]
        new_lines = new.split("\n")
        if all(not l.strip() for l in new_lines):
            drop.update(range(s, e + 1))
        else:
            lines[s:e + 1] = [l.rstrip() if l.strip() else l for l in new_lines]
            if len(new_lines) != e - s + 1:
                problems.append(f"{rel}:{c['start']} multi-line inline comment changed line count")
    cleaned = []
    k = 0
    while k < len(lines):
        if k not in drop:
            cleaned.append(lines[k])
            k += 1
            continue
        while k < len(lines) and k in drop:
            k += 1
        nxt = lines[k] if k < len(lines) else None
        prev_blank = bool(cleaned) and not cleaned[-1].strip()
        if nxt is not None and not nxt.strip():
            opener_before = bool(cleaned) and re.search(r"[{(\[]\s*$", cleaned[-1])
            closer_after = k + 1 < len(lines) and re.match(r"\s*[})\]]", lines[k + 1])
            if prev_blank or opener_before or not cleaned:
                k += 1
            elif closer_after:
                k += 1
        elif prev_blank and nxt is not None and re.match(r"\s*[})\]]", nxt):
            cleaned.pop()
    text = "\n".join(cleaned)
    if crlf:
        text = text.replace("\n", "\r\n")
    if bom:
        text = "﻿" + text
    path.write_bytes(text.encode("utf-8"))

print("files changed", len(by_file))
print("\n".join(problems) or "no problems")
