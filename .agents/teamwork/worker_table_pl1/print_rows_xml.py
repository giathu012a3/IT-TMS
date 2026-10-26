import sys
import docx

sys.stdout.reconfigure(encoding='utf-8')
doc = docx.Document('c:/IT_System/BaoCao_ITTMS_TKPMHDT (4).docx')
tbl = doc.tables[-1]

print("=== ROW 0 XML ===")
print(tbl.rows[0]._tr.xml)
print("\n=== ROW 1 XML ===")
print(tbl.rows[1]._tr.xml)
