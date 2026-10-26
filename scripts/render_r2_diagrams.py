import os
import io
import time
import urllib.request
from PIL import Image

DIAGRAMS_DIR = r"c:\IT_System\docs\diagrams"
KROKI_URL = "https://kroki.io/plantuml/png"

DIAGRAM_FILES = [
    "Hinh3_1_ERD_8Bang",
    "Hinh5_1_BPMN_TicketLifecycle",
    "Hinh5_2_StateMachine_Ticket",
    "Hinh5_3_ClassDiagram_MVC",
    "Hinh5_6_Sequence_UC09_PhanCong",
    "Hinh6_1_StatePattern_ClassDiagram"
]

def render_diagram(name):
    puml_path = os.path.join(DIAGRAMS_DIR, f"{name}.puml")
    png_path = os.path.join(DIAGRAMS_DIR, f"{name}.png")
    
    print(f"[*] Reading {puml_path}...")
    with open(puml_path, "r", encoding="utf-8") as f:
        puml_content = f.read()
        
    req = urllib.request.Request(
        KROKI_URL,
        data=puml_content.encode("utf-8"),
        headers={
            "Content-Type": "text/plain; charset=utf-8",
            "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
        }
    )
    
    print(f"[*] Sending request to Kroki API for {name}...")
    start_t = time.time()
    with urllib.request.urlopen(req, timeout=30) as resp:
        if resp.status != 200:
            raise RuntimeError(f"Kroki error: {resp.status} {resp.reason}")
        raw_png = resp.read()
    elapsed = time.time() - start_t
    print(f"[+] Received {len(raw_png)} bytes in {elapsed:.2f}s")
    
    img = Image.open(io.BytesIO(raw_png))
    w, h = img.size
    print(f"[*] Dimensions: {w} x {h} px")
    
    # Save with 300 DPI metadata
    img.save(png_path, "PNG", dpi=(300, 300))
    file_size = os.path.getsize(png_path)
    print(f"[SUCCESS] Saved {png_path} ({file_size:,} bytes, {w}x{h} px, 300 DPI)\n")
    return w, h, file_size

def main():
    results = {}
    for name in DIAGRAM_FILES:
        try:
            w, h, size = render_diagram(name)
            results[name] = {"status": "SUCCESS", "width": w, "height": h, "size": size}
        except Exception as e:
            print(f"[ERROR] Failed {name}: {e}\n")
            results[name] = {"status": "ERROR", "error": str(e)}
            
    print("="*60)
    print("SUMMARY RESULTS:")
    all_ok = True
    for name, r in results.items():
        if r["status"] == "SUCCESS":
            print(f"  {name:35}: {r['width']:4} x {r['height']:4} px ({r['size']:,} bytes)")
        else:
            print(f"  {name:35}: FAILED - {r.get('error')}")
            all_ok = False
    print("="*60)
    if not all_ok:
        exit(1)

if __name__ == "__main__":
    main()
