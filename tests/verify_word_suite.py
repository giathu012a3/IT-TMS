import zipfile, xml.etree.ElementTree as ET, re, os

print("======================================================================")
print("CHALLENGER 1: EMPIRICAL WORD REPORT INTEGRITY TEST SUITE (REQUIREMENT R2)")
print("======================================================================")

docx_files = [
    r"c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx",
    r"c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx"
]

W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ns = {"w": W_NS}

for docx_path in docx_files:
    fname = os.path.basename(docx_path)
    print(f"\n--- Testing Document: {fname} ---")
    assert os.path.exists(docx_path), f"File {docx_path} does not exist"
    
    with zipfile.ZipFile(docx_path) as z:
        # 1. Synthetic Prefixes check
        synthetic_found = []
        for name in z.namelist():
            if name.endswith(".xml") or name.endswith(".rels"):
                content = z.read(name).decode("utf-8", errors="ignore")
                matches = re.findall(r"(ns\d+:|xmlns:ns\d+)", content)
                if matches:
                    synthetic_found.append((name, set(matches)))
        print(f"Synthetic namespace prefixes: {synthetic_found} (Count: {len(synthetic_found)})")
        assert len(synthetic_found) == 0, f"Synthetic prefixes found in {fname}"
        
        # 2. Heading and TOC count
        xml_data = z.read("word/document.xml")
        root = ET.fromstring(xml_data)
        
        counts = {}
        for p in root.findall(".//w:p", ns):
            pStyle = p.find(".//w:pStyle", ns)
            if pStyle is not None:
                val = pStyle.attrib.get(f"{{{W_NS}}}val")
                if val in ("Heading1","Heading2","Heading3","TOC1","TOC2","TOC3"):
                    counts[val] = counts.get(val, 0) + 1
        
        print("Style distribution:", counts)
        h1 = counts.get("Heading1", 0)
        h2 = counts.get("Heading2", 0)
        h3 = counts.get("Heading3", 0)
        toc1 = counts.get("TOC1", 0)
        toc2 = counts.get("TOC2", 0)
        toc3 = counts.get("TOC3", 0)
        
        total_headings = h1 + h2 + h3
        total_toc = toc1 + toc2 + toc3
        total_all = total_headings + total_toc
        
        print(f"Heading 1: {h1}, Heading 2: {h2}, Heading 3: {h3} => Total Headings: {total_headings}")
        print(f"TOC 1: {toc1}, TOC 2: {toc2}, TOC 3: {toc3} => Total TOC items: {total_toc}")
        print(f"Total Headings + TOC: {total_all}")
        
        assert h1 == 14, f"Heading1 count mismatch: {h1} != 14"
        assert h2 == 62, f"Heading2 count mismatch: {h2} != 62"
        assert h3 == 73, f"Heading3 count mismatch: {h3} != 73"
        assert toc1 == 102, f"TOC1 count mismatch: {toc1} != 102"
        assert toc2 == 61, f"TOC2 count mismatch: {toc2} != 61"
        assert toc3 == 61, f"TOC3 count mismatch: {toc3} != 61"
        assert total_all == 373, f"Total mismatch: {total_all} != 373"
        
        # 3. Tables and Paragraph count in XML
        all_p = len(root.findall(".//w:p", ns))
        all_tbl = len(root.findall(".//w:tbl", ns))
        print(f"XML total paragraphs: {all_p}, XML total tables: {all_tbl}")
        assert all_tbl == 62, f"Table count mismatch: {all_tbl} != 62"
        
        # 4. Tech stack check
        text_content = "".join(t.text for t in root.findall(".//w:t", ns) if t.text)
        print("Legacy tech check: 'Flask' in text:", "Flask" in text_content)
        print("Legacy tech check: 'SQLAlchemy' in text:", "SQLAlchemy" in text_content)
        assert "Flask" not in text_content, "Legacy tech Flask still present in report"
        assert "SQLAlchemy" not in text_content, "Legacy tech SQLAlchemy still present in report"
        
        print("New tech check: '.NET 9' in text:", ".NET 9" in text_content)
        print("New tech check: 'Clean Architecture' in text:", "Clean Architecture" in text_content)
        print("New tech check: 'SignalR' in text:", "SignalR" in text_content)
        assert ".NET 9" in text_content, "Target tech .NET 9 missing in report"
        assert "SignalR" in text_content, "SignalR missing in report"

print("\n======================================================================")
print("ALL WORD INTEGRITY & CONTENT CHECKS PASSED EMPIRICALLY!")
print("======================================================================\n")
