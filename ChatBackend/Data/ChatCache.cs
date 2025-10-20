using System.Collections.Concurrent;
using ChatBackend.Interfaces;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatBackend.Data;

public class ChatCache : IChatCache
{
    private readonly ConcurrentDictionary<Guid, ChatHistory> _chats = [];
    private readonly IDbContextFactory<ChatContext> _contextFactory;

    public ChatCache(IDbContextFactory<ChatContext> contextFactory)
    {
        _contextFactory = contextFactory;

        IntializeCache();
    }

    private void IntializeCache()
    {
        using var context = _contextFactory.CreateDbContext();
        var histories = context.ChatHistories.AsNoTracking().ToList();
        foreach (var history in histories)
            _chats.TryAdd(history.Id, history);
    }

    public IEnumerable<object> ListChats() =>
        _chats.Select(kvp => new
        {
            ChatId = kvp.Key,
            LastMessage = new DateTimeOffset(kvp.Value.LastMessageTime ?? DateTime.UnixEpoch).ToUnixTimeSeconds(),
            kvp.Value.Title
        });

    public Task<ChatHistory?> GetChatHistory(Guid key)
    {
        _chats.TryGetValue(key, out var history);
        return Task.FromResult(history);
    }

    public async Task<ChatHistory> GetOrCreateChatHistory(Guid key)
    {
        var history = await GetChatHistory(key);
        if (history is not null)
            return history;

        var newHistory = new ChatHistory { Id = key };

        using (var context = _contextFactory.CreateDbContext())
        {
            context.ChatHistories.Add(newHistory);
            await context.SaveChangesAsync();
        }

        _chats.TryAdd(key, newHistory);

        return newHistory;
    }

    public async Task<Guid> CreateChatHistory(ChatHistory history)
    {
        if (history.Id == Guid.Empty)
            history.Id = Guid.NewGuid();

        using (var context = _contextFactory.CreateDbContext())
        {
            context.ChatHistories.Add(history);
            await context.SaveChangesAsync();
        }

        _chats.TryAdd(history.Id, history);

        return history.Id;
    }

    public async Task<bool> RemoveChatHistory(Guid key)
    {
        _chats.TryRemove(key, out var _);

        using var context = _contextFactory.CreateDbContext();
        var historyToRemove = await context.ChatHistories.FindAsync(key);
        if (historyToRemove is not null)
        {
            context.ChatHistories.Remove(historyToRemove);
            await context.SaveChangesAsync();
            return true;
        }

        return false;
    }

    public async Task<bool> UpdateChatHistory(ChatHistory history)
    {
        using var context = _contextFactory.CreateDbContext();

        context.ChatHistories.Update(history);

        var changedCount = await context.SaveChangesAsync();

        return changedCount > 0;
    }

}
