import docx
import re
import sys

sys.stdout.reconfigure(encoding='utf-8')
doc = docx.Document(r'c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx')
for idx in [357, 362, 481, 564, 567]:
    p = doc.paragraphs[idx]
    xml = p._element.xml
    ext = re.findall(r'<wp:extent\s+cx="(\d+)"\s+cy="(\d+)"', xml)
    blip = re.findall(r'<a:blip\s+[^>]*r:embed="([^"]+)"', xml)
    # convert EMU to inches and cm (1 inch = 914400 EMUs, 1 cm = 360000 EMUs)
    dim_str = ""
    if ext:
        cx, cy = int(ext[0][0]), int(ext[0][1])
        w_in, h_in = cx / 914400.0, cy / 914400.0
        w_cm, h_cm = cx / 360000.0, cy / 360000.0
        dim_str = f"{w_in:.2f}in x {h_in:.2f}in ({w_cm:.2f}cm x {h_cm:.2f}cm)"
    print(f"P#{idx:3d}: extent={dim_str}, blip={blip}, align={p.alignment}, style={p.style.name}")
