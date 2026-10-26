import urllib.request
import zlib
import base64
import sys
import os
from PIL import Image
import io

sys.stdout.reconfigure(encoding='utf-8')

def kroki_encode(source_text):
    compressed = zlib.compress(source_text.encode('utf-8'), 9)
    return base64.urlsafe_b64encode(compressed).decode('ascii')

def render_puml_to_file(puml_code, out_path, dpi=300):
    encoded = kroki_encode(puml_code)
    url = f"https://kroki.io/plantuml/png/{encoded}"
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, timeout=15) as resp:
        data = resp.read()
    
    # Save with PIL and embed 300 DPI metadata
    im = Image.open(io.BytesIO(data))
    im.save(out_path, 'PNG', dpi=(dpi, dpi))
    print(f"Rendered: {os.path.basename(out_path)} | Size: {im.width}x{im.height} | File: {len(data)/1024:.1f} KB")

erd_puml = """
@startuml
skinparam dpi 300
skinparam monochrome false
skinparam shadowing false
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 11
skinparam roundcorner 6
skinparam linetype ortho

skinparam class {
    BackgroundColor #FFFFFF
    BorderColor #475569
    HeaderBackgroundColor #F1F5F9
    ArrowColor #2563EB
    BorderThickness 1.2
}

entity "Users" as users {
    * **Id** : int [PK]
    --
    * Username : string [UQ]
    * Email : string [UQ]
    * PasswordHash : string
    * FullName : string
    * Role : string
    * IsActive : bool
    * CreatedAt : DateTime
}

entity "TicketStatuses" as statuses {
    * **Id** : int [PK]
    --
    * Code : string [UQ]
    * Name : string [UQ]
    * ColorCode : string
    * SortOrder : int
}

entity "Tickets" as tickets {
    * **Id** : int [PK]
    --
    * Title : string
    * Description : string
    * Priority : TicketPriority
    * StatusId : int [FK]
    * CreatorId : int [FK]
    AssignedToId : int? [FK]
    * CreatedAt : DateTime
    DueAt : DateTime?
    ResolvedAt : DateTime?
    ClosedAt : DateTime?
    * RowVersion : Guid
}

entity "Comments" as comments {
    * **Id** : int [PK]
    --
    * TicketId : int [FK]
    * UserId : int [FK]
    * Content : string
    * IsInternal : bool
    * CreatedAt : DateTime
}

entity "Feedbacks" as feedbacks {
    * **Id** : int [PK]
    --
    * TicketId : int [FK, UQ]
    * UserId : int [FK]
    * Rating : int
    Comment : string?
    * CreatedAt : DateTime
}

entity "Attachments" as attachments {
    * **Id** : int [PK]
    --
    * TicketId : int [FK]
    * UploadedById : int [FK]
    * FileName : string
    * FilePath : string
    * FileSize : long
    * ContentType : string
    * CreatedAt : DateTime
}

entity "Notifications" as notifications {
    * **Id** : int [PK]
    --
    * UserId : int [FK]
    TicketId : int? [FK]
    * Title : string
    * Message : string
    * IsRead : bool
    * CreatedAt : DateTime
}

entity "SystemLogs" as logs {
    * **Id** : int [PK]
    --
    UserId : int? [FK]
    TicketId : int? [FK]
    * Action : string
    Details : string?
    IpAddress : string?
    * CreatedAt : DateTime
}

users ||--o{ tickets : "creates (1..*)"
users ||--o{ tickets : "assigned_to (0..*)"
statuses ||--o{ tickets : "classifies (1..*)"
tickets ||--o{ comments : "has (0..*)"
users ||--o{ comments : "authors (0..*)"
tickets ||--|| feedbacks : "evaluated_by (0..1)"
users ||--o{ feedbacks : "submits (0..*)"
tickets ||--o{ attachments : "attaches (0..*)"
users ||--o{ attachments : "uploads (0..*)"
users ||--o{ notifications : "receives (0..*)"
tickets ||--o{ notifications : "relates_to (0..*)"
users ||--o{ logs : "performs (0..*)"
tickets ||--o{ logs : "targets (0..*)"
@enduml
"""

state_puml = """
@startuml
skinparam dpi 300
skinparam monochrome false
skinparam shadowing false
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 11
skinparam roundcorner 8

skinparam state {
    BackgroundColor #FFFFFF
    BorderColor #334155
    ArrowColor #2563EB
    StartColor #0F172A
    EndColor #0F172A
    BorderThickness 1.2
}

[*] --> New : User submits ticket

state New #F8FAFC
state Assigned #EFF6FF
state InProgress #F0FDF4
state Waiting #FEFCE8
state Resolved #FAF5FF
state Closed #F1F5F9
state Rejected #FEF2F2

New --> Assigned : Leader assigns Staff\n/ Staff self-claims
New --> Rejected : Leader rejects (invalid/spam)
Assigned --> InProgress : Staff starts work
InProgress --> Waiting : Staff requests info from User
Waiting --> InProgress : User provides info
InProgress --> Resolved : Staff resolves issue
Resolved --> Closed : User confirms & rates\n/ Auto-close after 48h
Resolved --> InProgress : User reopens (unresolved)

Closed --> [*]
Rejected --> [*]
@enduml
"""

samples_dir = r"c:\IT_System\.agents\teamwork\explorer_survey_3\samples"
render_puml_to_file(erd_puml, os.path.join(samples_dir, "sample_fig_3_1_erd.png"))
render_puml_to_file(state_puml, os.path.join(samples_dir, "sample_fig_5_2_state_machine.png"))
