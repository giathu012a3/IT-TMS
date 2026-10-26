import urllib.request, zlib, base64

def check_syntax(s):
    enc = base64.urlsafe_b64encode(zlib.compress(s.encode('utf-8'), 9)).decode('ascii')
    url = f'https://kroki.io/plantuml/png/{enc}'
    try:
        r = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'}))
        print('Syntax OK, bytes:', len(r.read()))
    except urllib.error.HTTPError as e:
        print('HTTPError:', e.code, e.read().decode('utf-8'))

state_puml = """@startuml
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

[*] --> New : Người dùng gửi yêu cầu

state "New (Mới)" as New
state "Assigned (Đã phân công)" as Assigned
state "InProgress (Đang xử lý)" as InProgress
state "Waiting (Chờ thông tin)" as Waiting
state "Resolved (Đã giải quyết)" as Resolved
state "Closed (Đã đóng)" as Closed
state "Rejected (Bị từ chối)" as Rejected

New --> Assigned : Leader phân công KTV / KTV tự nhận
New --> Rejected : Leader từ chối yêu cầu (không hợp lệ)
Assigned --> InProgress : KTV bắt đầu xử lý
InProgress --> Waiting : KTV yêu cầu bổ sung thông tin
Waiting --> InProgress : Người dùng phản hồi thông tin
InProgress --> Resolved : KTV hoàn tất xử lý
Resolved --> Closed : Người dùng xác nhận & đánh giá / Tự động đóng sau 48h
Resolved --> InProgress : Người dùng mở lại (chưa hài lòng)

Closed --> [*]
Rejected --> [*]
@enduml"""

check_syntax(state_puml)
