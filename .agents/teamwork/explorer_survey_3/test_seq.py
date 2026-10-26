import urllib.request, zlib, base64, io
from PIL import Image

seq_puml = """@startuml
skinparam dpi 300
skinparam monochrome false
skinparam shadowing false
skinparam defaultFontName "Segoe UI"
skinparam defaultFontSize 11
skinparam roundcorner 6
skinparam sequence {
    ArrowColor #2563EB
    ActorBorderColor #334155
    LifeLineBorderColor #64748B
    LifeLineBackgroundColor #F8FAFC
    ParticipantBorderColor #334155
    ParticipantBackgroundColor #FFFFFF
    ParticipantFontSize 11
    BoxBackgroundColor #F8FAFC
    BoxBorderColor #CBD5E1
}

actor "Trưởng nhóm (Leader)" as Leader
participant "LeaderController" as Controller
box "Core Domain & EF Core" #F8FAFC
participant "ItTmsDbContext" as DbContext
participant "Ticket" as Ticket
participant "NewState" as NewState
participant "AssignedState" as AssignedState
end box
participant "DomainEventDispatcher" as Dispatcher
participant "TicketHub (SignalR)" as Hub
actor "KTV (Staff)" as Staff

Leader -> Controller : POST /Leader/Assign(ticketId, staffId)
activate Controller #EFF6FF

Controller -> DbContext : Tickets.Include(s => s.Status).FirstOrDefaultAsync(id)
activate DbContext #F1F5F9
DbContext --> Controller : ticket
deactivate DbContext

Controller -> Ticket : AssignTo(staffId, currentUserId)
activate Ticket #EFF6FF

Ticket -> NewState : AssignTo(ticket, staffId)
activate NewState #EFF6FF

NewState -> AssignedState : TransitionTo(AssignedState)
NewState -> Ticket : AddDomainEvent(TicketAssignedEvent)
NewState --> Ticket : Result.Success()
deactivate NewState

Ticket --> Controller : Result.Success()
deactivate Ticket

Controller -> DbContext : SaveChangesAsync()
activate DbContext #F1F5F9

DbContext -> Dispatcher : DispatchInTransactionAsync(TicketAssignedEvent)
activate Dispatcher #EFF6FF
Dispatcher -> DbContext : Notifications.Add(staffNotif)
Dispatcher -> DbContext : SystemLogs.Add(log)
deactivate Dispatcher

DbContext --> Controller : rowsAffected > 0

DbContext -> Dispatcher : DispatchPostCommitAsync(TicketAssignedEvent)
activate Dispatcher #EFF6FF
Dispatcher -> Hub : Clients.User(staffId).SendAsync("ReceiveNotification", ...)
activate Hub #EFF6FF
Hub --> Staff : WebSocket Push
deactivate Hub
deactivate Dispatcher
deactivate DbContext

Controller --> Leader : RedirectToAction("Index") (TempData["Success"])
deactivate Controller
@enduml"""

enc = base64.urlsafe_b64encode(zlib.compress(seq_puml.encode('utf-8'), 9)).decode('ascii')
url = f'https://kroki.io/plantuml/png/{enc}'
data = urllib.request.urlopen(urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})).read()
im = Image.open(io.BytesIO(data))
print('Sequence Diagram Dimensions:', im.width, 'x', im.height, 'Size:', len(data)/1024, 'KB')
