using System.Collections.Concurrent;
using ChatBackend.Models;

namespace ChatBackend.Data;

public static class ChatsCache
{
    private static readonly ConcurrentDictionary<Guid, ChatHistory> _chats = [];

    public static IEnumerable<object> ListChats() =>
        _chats.Select(kvp => new
        {
            ChatId = kvp.Key,
            LastMessage = new DateTimeOffset(kvp.Value.LastMessageTime ?? DateTime.UnixEpoch).ToUnixTimeSeconds(),
            kvp.Value.Title
        });

    public static bool HistoryExists(Guid key) =>
        _chats.ContainsKey(key);

    public static bool GetChatHistory(Guid key, out ChatHistory? history)
    {
        if (_chats.TryGetValue(key, out var _history))
        {
            history = _history;
            return true;
        }

        history = null;
        return false;
    }

    public static void GetOrCreateChatHistory(Guid key, out ChatHistory history)
    {
        if (!_chats.TryGetValue(key, out var _history))
        {
            _history = new();
            _chats.TryAdd(key, _history);
        }

        history = _history;
    }

    public static bool RemoveChatHistory(Guid key)
    {
        if (_chats.TryRemove(key, out var _))
            return true;

        return false;
    }

}
