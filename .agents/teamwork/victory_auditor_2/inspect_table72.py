import sys
import zipfile
import xml.etree.ElementTree as ET

sys.stdout.reconfigure(encoding='utf-8')

doc = r"c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx"
W_NS = "http://schemas.openxmlformats.org/wordprocessingml/2006/main"
ns = {"w": W_NS}

with zipfile.ZipFile(doc) as z:
    xml_data = z.read("word/document.xml")
    root = ET.fromstring(xml_data)
    
    tables = root.findall(".//w:tbl", ns)
    print(f"Total tables: {len(tables)}")
    
    for idx, tbl in enumerate(tables):
        tbl_text = ""
        for cell in tbl.findall(".//w:tc", ns):
            cell_text = "".join(t.text for t in cell.findall(".//w:t", ns) if t.text)
            tbl_text += " " + cell_text
        if "7.2" in tbl_text or "kịch bản" in tbl_text.lower() or "đăng nhập" in tbl_text.lower() or "tc-" in tbl_text.lower():
            if "bảng 7.2" in tbl_text.lower() or "kết quả" in tbl_text.lower() or "đạt" in tbl_text.lower() or "kịch bản 1" in tbl_text.lower():
                print(f"\n================ TABLE INDEX {idx} ================")
                rows = tbl.findall(".//w:tr", ns)
                print(f"Number of rows: {len(rows)}")
                for r_idx, row in enumerate(rows):
                    row_cells = []
                    for c in row.findall(".//w:tc", ns):
                        txt = "".join(t.text for t in c.findall(".//w:t", ns) if t.text)
                        row_cells.append(txt.strip().replace("\n", " "))
                    print(f"Row {r_idx}: {' | '.join(row_cells)}")
