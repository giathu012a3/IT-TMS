import os
import re

base_dir = r"c:\IT_System\tests\ItTms.Tests"
rows = []

for root, dirs, files in os.walk(base_dir):
    if "obj" in root or "bin" in root:
        continue
    for f in sorted(files):
        if not f.endswith(".cs"):
            continue
        full_path = os.path.join(root, f)
        with open(full_path, "r", encoding="utf-8") as fp:
            content = fp.read()
        facts = len(re.findall(r"^\s*\[Fact(\(.*?\))?\]", content, re.MULTILINE))
        theories = len(re.findall(r"^\s*\[Theory(\(.*?\))?\]", content, re.MULTILINE))
        rel = os.path.relpath(full_path, base_dir)
        if facts > 0 or theories > 0:
            rows.append((rel, facts, theories, facts + theories))

print(f"{'Relative File Path':<55} | {'Facts':<6} | {'Theories':<8} | {'Methods':<7}")
print("-" * 85)
for r in sorted(rows):
    print(f"{r[0]:<55} | {r[1]:<6} | {r[2]:<8} | {r[3]:<7}")
print("-" * 85)
print(f"Total files: {len(rows)}, Total test methods: {sum(r[3] for r in rows)}")
