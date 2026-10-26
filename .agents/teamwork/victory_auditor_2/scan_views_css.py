import os
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')

print("=== VICTORY AUDITOR VIEW AND CSS SCANNER ===")

views_dir = r"c:\IT_System\src\ItTms.Web"
css_file = r"c:\IT_System\src\ItTms.Web\wwwroot\css\site.css"

issues = []
views_scanned = 0

for root, _, files in os.walk(views_dir):
    for f in files:
        if f.endswith(".cshtml") and not f.startswith("_View"):
            views_scanned += 1
            fpath = os.path.join(root, f)
            with open(fpath, "r", encoding="utf-8") as file:
                lines = file.readlines()
                for idx, line in enumerate(lines, 1):
                    # Check text-primary or text-danger on headings
                    if re.search(r"<h[1-6][^>]*class=[\"'][^\"']*\btext-(primary|danger)\b", line):
                        issues.append((fpath, idx, "Heading with text-primary/danger", line.strip()))
                    # Check <footer> tag
                    if "<footer" in line.lower():
                        issues.append((fpath, idx, "Redundant <footer> tag", line.strip()))
                    # Check raw alert()
                    if "alert(" in line and "toast" not in line.lower():
                        issues.append((fpath, idx, "Raw alert() call", line.strip()))

print(f"Total views scanned: {views_scanned}")
print(f"Total issues found in views: {len(issues)}")
for iss in issues:
    print(" ", iss)

# 2. Check CSS site.css rules
with open(css_file, "r", encoding="utf-8") as f:
    css_content = f.read()

checks = {
    "flash alert max-width: 460px": "width: min(92vw, 460px);" in css_content or "max-width: 460px" in css_content,
    "flash alert auto-dismiss 5000ms": "animation-duration: 5000ms;" in css_content,
    "flash alert hover pause": "animation-play-state: paused !important;" in css_content,
    "admin action edit min-width": ".btn-action-edit { min-width: 56px; }" in css_content,
    "admin action reset min-width": ".btn-action-reset { min-width: 84px; }" in css_content,
    "admin action toggle min-width": ".btn-action-toggle { min-width: 90px; }" in css_content,
    "table-light dark mode rule": ".table > tbody > tr.table-light > td" in css_content,
    "quick-fill button styling": ".quick-fill-btn" in css_content,
}

print("\n--- CSS Verification Checks ---")
for check_name, passed in checks.items():
    print(f"[{'PASS' if passed else 'FAIL'}] {check_name}")

if len(issues) == 0 and all(checks.values()):
    print("\nALL VIEW & CSS CHECKS PASSED EMPIRICALLY!")
else:
    print(f"\nCHECK FAILED: {len(issues)} issues in views, CSS passed: {all(checks.values())}")
