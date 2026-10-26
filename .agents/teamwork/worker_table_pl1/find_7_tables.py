import sys
import docx

sys.stdout.reconfigure(encoding='utf-8')

doc2 = docx.Document("c:/IT_System/BaoCao_ITTMS_TKPMHDT_DotNet9.docx")
body = doc2._body._element

tables_to_remove = []
for idx, tbl in enumerate(doc2.tables):
    text = "".join(tbl._tbl.itertext()).strip()
    if "[CHÈN HÌNH]" in text:
        pos = body.index(tbl._tbl)
        tables_to_remove.append((idx, pos, text.split('\n')[0]))

print(f"Total [CHÈN HÌNH] tables found: {len(tables_to_remove)}")
for t in tables_to_remove:
    print(f"Table index {t[0]}, Body child index {t[1]}: {t[2]}")
