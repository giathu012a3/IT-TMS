import os
import zipfile
import re
import xml.etree.ElementTree as ET

print("=== VICTORY AUDITOR INDEPENDENT WORD XML AUDIT ===")

docs = [
    r"c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx",
    r"c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx"
]

backup_doc = r"c:\IT_System\.agents\teamwork\BaoCao_ITTMS_TKPMHDT (4).backup.docx"

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ns = {"w": W_NS}

def get_headings_and_toc(docx_path):
    with zipfile.ZipFile(docx_path) as z:
        xml_data = z.read("word/document.xml")
        root = ET.fromstring(xml_data)
        items = []
        counts = {}
        for p in root.findall(".//w:p", ns):
            pStyle = p.find(".//w:pStyle", ns)
            if pStyle is not None:
                val = pStyle.attrib.get(f"{{{W_NS}}}val")
                if val in ("Heading1","Heading2","Heading3","TOC1","TOC2","TOC3"):
                    text = "".join(t.text for t in p.findall(".//w:t", ns) if t.text)
                    counts[val] = counts.get(val, 0) + 1
                    items.append((val, text))
        return counts, items, root

print(f"\nChecking backup document: {backup_doc}")
b_counts, b_items, _ = get_headings_and_toc(backup_doc)
print("Backup heading/TOC counts:", b_counts)
b_total = sum(b_counts.values())
print(f"Backup total headings + TOC: {b_total}")

for doc in docs:
    fname = os.path.basename(doc)
    print(f"\n--- Checking {fname} ---")
    if not os.path.exists(doc):
        print(f"ERROR: {doc} does not exist!")
        continue
    
    with zipfile.ZipFile(doc) as z:
        # Check synthetic namespaces
        synthetic_matches = []
        for name in z.namelist():
            if name.endswith(".xml") or name.endswith(".rels"):
                content = z.read(name).decode("utf-8", errors="ignore")
                matches = re.findall(r"(ns\d+:|xmlns:ns\d+)", content)
                if matches:
                    synthetic_matches.append((name, set(matches)))
        print(f"Synthetic prefixes found: {synthetic_matches} (Total files with issue: {len(synthetic_matches)})")
        
        # Check headings
        counts, items, root = get_headings_and_toc(doc)
        print("Style distribution:", counts)
        total = sum(counts.values())
        print(f"Total headings + TOC: {total}")
        
        # Check matching with backup
        if total != b_total:
            print(f"WARNING: Total count mismatch with backup ({total} vs {b_total})")
        else:
            print(f"SUCCESS: Heading/TOC count matches backup perfectly ({total} items)")
            
        # Verify text content
        full_text = "".join(t.text for t in root.findall(".//w:t", ns) if t.text)
        print("Length of full text extracted:", len(full_text))
        
        # Check required keywords
        keywords = [".NET 9", "Clean Architecture", "EF Core 9", "SignalR", "State Pattern", "TC-01", "TC-10"]
        for kw in keywords:
            found = kw in full_text
            print(f"Keyword '{kw}' in text: {found}")
            
        # Check table 7.2
        print("Checking Table 7.2 scenarios in text:")
        for tc_num in range(1, 11):
            tc_code = f"TC-{tc_num:02d}"
            print(f"  {tc_code} present: {tc_code in full_text}")

print("\nIndependent docx XML audit complete.")
