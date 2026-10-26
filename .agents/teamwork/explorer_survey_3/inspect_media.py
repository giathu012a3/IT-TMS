import zipfile
import os
import sys
from PIL import Image

sys.stdout.reconfigure(encoding='utf-8')

def inspect_media(docx_path):
    print(f"\n--- MEDIA IN {os.path.basename(docx_path)} ---")
    with zipfile.ZipFile(docx_path, 'r') as z:
        for info in sorted(z.infolist(), key=lambda x: x.filename):
            if info.filename.startswith('word/media/'):
                fn = os.path.basename(info.filename)
                data = z.read(info.filename)
                size_kb = len(data) / 1024
                # Try opening with PIL
                dim = "N/A"
                fmt = "N/A"
                if not fn.endswith('.emf'):
                    try:
                        import io
                        im = Image.open(io.BytesIO(data))
                        dim = f"{im.width}x{im.height}"
                        fmt = im.format
                    except Exception as e:
                        dim = f"Err: {e}"
                else:
                    fmt = "EMF (Windows Enhanced Metafile)"
                    
                print(f"  {fn:<18} | Size: {size_kb:6.1f} KB | Format: {fmt:<8} | Dim: {dim}")

if __name__ == '__main__':
    inspect_media(r"c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx")
    inspect_media(r"c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx")
