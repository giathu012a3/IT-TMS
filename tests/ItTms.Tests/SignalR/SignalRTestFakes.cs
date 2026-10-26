using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using ItTms.Web.Hubs;

namespace ItTms.Tests.SignalR;

public class TestClientProxy : IClientProxy
{
    public List<(string Method, object?[] Args)> SentMessages { get; } = new();

    public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
    {
        lock (SentMessages)
        {
            SentMessages.Add((method, args));
        }
        return Task.CompletedTask;
    }
}

public class TestHubCallerClients : IHubCallerClients
{
    public TestClientProxy AllClient { get; } = new();
    public TestClientProxy CallerClient { get; } = new();
    public ConcurrentDictionary<string, TestClientProxy> GroupsMap { get; } = new();
    public ConcurrentDictionary<string, TestClientProxy> UsersMap { get; } = new();

    public IClientProxy All => AllClient;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => AllClient;
    public IClientProxy Client(string connectionId) => CallerClient;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => CallerClient;
    public IClientProxy Group(string groupName) => GroupsMap.GetOrAdd(groupName, _ => new TestClientProxy());
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Group(groupName);
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => CallerClient;
    public IClientProxy User(string userId) => UsersMap.GetOrAdd(userId, _ => new TestClientProxy());
    public IClientProxy Users(IReadOnlyList<string> userIds) => CallerClient;
    public IClientProxy Caller => CallerClient;
    public IClientProxy Others => CallerClient;
    public IClientProxy OthersInGroup(string groupName) => Group(groupName);
}

public class TestHubClients : IHubClients
{
    public TestClientProxy AllClient { get; } = new();
    public ConcurrentDictionary<string, TestClientProxy> GroupsMap { get; } = new();
    public ConcurrentDictionary<string, TestClientProxy> UsersMap { get; } = new();

    public IClientProxy All => AllClient;
    public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => AllClient;
    public IClientProxy Client(string connectionId) => AllClient;
    public IClientProxy Clients(IReadOnlyList<string> connectionIds) => AllClient;
    public IClientProxy Group(string groupName) => GroupsMap.GetOrAdd(groupName, _ => new TestClientProxy());
    public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => Group(groupName);
    public IClientProxy Groups(IReadOnlyList<string> groupNames) => AllClient;
    public IClientProxy User(string userId) => UsersMap.GetOrAdd(userId, _ => new TestClientProxy());
    public IClientProxy Users(IReadOnlyList<string> userIds) => AllClient;
}

public class TestGroupManager : IGroupManager
{
    public List<(string ConnectionId, string GroupName)> Added { get; } = new();
    public List<(string ConnectionId, string GroupName)> Removed { get; } = new();

    public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        lock (Added)
        {
            Added.Add((connectionId, groupName));
        }
        return Task.CompletedTask;
    }

    public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
    {
        lock (Removed)
        {
            Removed.Add((connectionId, groupName));
        }
        return Task.CompletedTask;
    }
}

public class TestHubContext : IHubContext<TicketHub>
{
    public TestHubClients HubClients { get; } = new();
    public TestGroupManager GroupManager { get; } = new();

    public IHubClients Clients => HubClients;
    public IGroupManager Groups => GroupManager;
}
