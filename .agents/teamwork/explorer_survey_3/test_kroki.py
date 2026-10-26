import urllib.request
import zlib
import base64
import json
import sys

sys.stdout.reconfigure(encoding='utf-8')

# Kroki URL encoding: deflate + base64url
def kroki_encode(source_text):
    compressed = zlib.compress(source_text.encode('utf-8'), 9)
    return base64.urlsafe_b64encode(compressed).decode('ascii')

def test_kroki_render(diagram_type, diagram_source, output_format='png'):
    encoded = kroki_encode(diagram_source)
    url = f"https://kroki.io/{diagram_type}/{output_format}/{encoded}"
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'})
    try:
        with urllib.request.urlopen(req, timeout=10) as resp:
            data = resp.read()
            print(f"[{diagram_type}] Render {output_format.upper()} Success! Received {len(data)} bytes.")
            return data
    except Exception as e:
        print(f"[{diagram_type}] Render failed: {e}")
        return None

if __name__ == '__main__':
    # Test 1: PlantUML
    puml = """
    @startuml
    skinparam monochrome false
    skinparam defaultFontName "Segoe UI"
    class User {
        +int Id
        +string FullName
        +string Role
    }
    class Ticket {
        +int Id
        +string Title
        +string Status
    }
    User "1" -- "*" Ticket : creates >
    @enduml
    """
    test_kroki_render('plantuml', puml, 'png')
    test_kroki_render('plantuml', puml, 'svg')

    # Test 2: Mermaid
    mmd = """
    graph LR
        A[Client] -->|SignalR WebSocket| B(TicketHub)
        B --> C[(SQLite DB)]
    """
    test_kroki_render('mermaid', mmd, 'png')
    test_kroki_render('mermaid', mmd, 'svg')

    # Test 3: Graphviz
    dot = """
    digraph G {
        rankdir=LR;
        node [shape=box, fontname="Segoe UI"];
        New -> Assigned -> InProgress -> Resolved -> Closed;
    }
    """
    test_kroki_render('graphviz', dot, 'png')
    test_kroki_render('graphviz', dot, 'svg')
