import docx
import sys
sys.stdout.reconfigure(encoding='utf-8')

doc = docx.Document(r'c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx')
for i in range(355, 375):
    p = doc.paragraphs[i]
    has_dw = '<w:drawing' in p._element.xml
    print(f"P{i:03d} | {p.style.name:<15} | dw={has_dw:<5} | {p.text}")
