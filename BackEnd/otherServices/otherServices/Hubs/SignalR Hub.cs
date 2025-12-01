using Microsoft.AspNetCore.SignalR;
using otherServices.Models.DTOs;
using RentMate.Services;
using System;
using System.Threading.Tasks;

namespace RentMate.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IMessageService _messageService;
        private static readonly ConnectionMapping<string> _connections = new ConnectionMapping<string>();

        public ChatHub(IMessageService messageService)
        {
            _messageService = messageService;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                _connections.Add(userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var userId = Context.GetHttpContext()?.Request.Query["userId"];

            if (!string.IsNullOrEmpty(userId))
            {
                _connections.Remove(userId, Context.ConnectionId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(long senderId, CreateMessageDto messageDto, long receiverId)
        {
            var message = await _messageService.CreateMessageAsync(senderId, messageDto, receiverId);

            string senderKey = senderId.ToString();
            string receiverKey = receiverId.ToString();

            foreach (var connectionId in _connections.GetConnections(senderKey))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
            }

            foreach (var connectionId in _connections.GetConnections(receiverKey))
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", message);
            }
        }
    }

    public class ConnectionMapping<T>
    {
        private readonly Dictionary<T, HashSet<string>> _connections = new Dictionary<T, HashSet<string>>();

        public int Count => _connections.Count;

        public void Add(T key, string connectionId)
        {
            lock (_connections)
            {
                if (!_connections.TryGetValue(key, out HashSet<string> connections))
                {
                    connections = new HashSet<string>();
                    _connections.Add(key, connections);
                }

                lock (connections)
                {
                    connections.Add(connectionId);
                }
            }
        }

        public IEnumerable<string> GetConnections(T key)
        {
            if (_connections.TryGetValue(key, out HashSet<string> connections))
            {
                return connections;
            }

            return Enumerable.Empty<string>();
        }

        public void Remove(T key, string connectionId)
        {
            lock (_connections)
            {
                if (!_connections.TryGetValue(key, out HashSet<string> connections))
                {
                    return;
                }

                lock (connections)
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.Remove(key);
                    }
                }
            }
        }
    }
}