import os
import re

header_pattern = re.compile(r'<h[1-6][^>]*class=[\'"][^\'"]*(text-primary|text-danger)', re.IGNORECASE)
id_pattern = re.compile(r'#(@ticket\.Id|@t\.Id|@item\.Id)[^<]*', re.IGNORECASE)
priority_pill_pattern = re.compile(r'(Low|Medium).*?(bg-primary)', re.IGNORECASE)

views_dir = r"c:\IT_System\src\ItTms.Web"

header_violations = []
id_violations = []
priority_violations = []

for root, dirs, files in os.walk(views_dir):
    for f in files:
        if f.endswith(".cshtml"):
            fpath = os.path.join(root, f)
            with open(fpath, "r", encoding="utf-8") as fp:
                lines = fp.readlines()
            for idx, line in enumerate(lines, 1):
                if header_pattern.search(line):
                    header_violations.append((fpath, idx, line.strip()))
                if "text-primary" in line and ("ticket.Id" in line or "t.Id" in line or "item.Id" in line):
                    id_violations.append((fpath, idx, line.strip()))
                if ("TicketPriority.Low" in line or "TicketPriority.Medium" in line or "Low" in line or "Medium" in line) and "bg-primary" in line:
                    priority_violations.append((fpath, idx, line.strip()))

print("--- HEADER VIOLATIONS (text-primary/text-danger on h1-h6) ---")
print(f"Total: {len(header_violations)}")
for v in header_violations:
    print(f"  {v[0]}:{v[1]}: {v[2]}")

print("\n--- ID COLUMN VIOLATIONS (text-primary on #ID) ---")
print(f"Total: {len(id_violations)}")
for v in id_violations:
    print(f"  {v[0]}:{v[1]}: {v[2]}")

print("\n--- PRIORITY PILL VIOLATIONS (bg-primary on Low/Medium) ---")
print(f"Total: {len(priority_violations)}")
for v in priority_violations:
    print(f"  {v[0]}:{v[1]}: {v[2]}")
