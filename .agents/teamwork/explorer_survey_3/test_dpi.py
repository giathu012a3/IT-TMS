import urllib.request
import zlib
import base64
import sys
from PIL import Image
import io

sys.stdout.reconfigure(encoding='utf-8')

def kroki_encode(source_text):
    compressed = zlib.compress(source_text.encode('utf-8'), 9)
    return base64.urlsafe_b64encode(compressed).decode('ascii')

puml_300dpi = """
@startuml
skinparam dpi 300
skinparam monochrome false
skinparam defaultFontName "Segoe UI"
skinparam roundcorner 8
skinparam class {
    BackgroundColor #F8FAFC
    BorderColor #334155
    ArrowColor #2563EB
}

class User {
    +int Id
    +string FullName
    +string Email
    +string Role
    +bool IsActive
}

class Ticket {
    +int Id
    +string Title
    +string Status
    +DateTime CreatedAt
}

User "1" -- "*" Ticket : creates >
@enduml
"""

encoded = kroki_encode(puml_300dpi)
url = f"https://kroki.io/plantuml/png/{encoded}"
req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
resp = urllib.request.urlopen(req)
data = resp.read()

im = Image.open(io.BytesIO(data))
print(f"Image Format: {im.format}")
print(f"Image Dimensions: {im.width}x{im.height}")
print(f"Image Info/DPI: {im.info.get('dpi', 'Not set')}")
print(f"File Size: {len(data) / 1024:.1f} KB")
