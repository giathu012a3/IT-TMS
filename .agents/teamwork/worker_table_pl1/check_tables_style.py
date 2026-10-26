import sys
import docx
from docx.oxml.ns import qn

sys.stdout.reconfigure(encoding='utf-8')
doc = docx.Document('c:/IT_System/BaoCao_ITTMS_TKPMHDT (4).docx')
for i in [0, 1, 10, 20, 30, 40, 50, 54]:
    tbl = doc.tables[i]
    header = [c.text.strip().replace('\n', ' ') for c in tbl.rows[0].cells]
    tblW_el = tbl._tbl.tblPr.find(qn('w:tblW'))
    w_val = tblW_el.attrib if tblW_el is not None else None
    print(f'Table {i}: rows={len(tbl.rows)}, cols={len(tbl.columns)}, w={w_val}, header={header}')
