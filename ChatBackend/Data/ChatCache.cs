using System.Collections.Concurrent;
using ChatBackend.Interfaces;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatBackend.Data;

public class ChatCache(IDbContextFactory<ChatContext> contextFactory) : IChatCache
{
    private readonly ConcurrentDictionary<Guid, ChatHistory> _chats = [];
    private readonly IDbContextFactory<ChatContext> _contextFactory = contextFactory;

    public IEnumerable<object> ListChats()
    {
        using var context = _contextFactory.CreateDbContext();

        return context.ChatHistories
            .AsNoTracking()
            .Select(ch => new
            {
                ch.ChatId,
                ch.Title,
                LastMessage = new DateTimeOffset(ch.LastMessageTime ?? DateTime.UnixEpoch).ToUnixTimeSeconds()
            }).ToList();
    }

    public async Task<ChatHistory?> GetChatHistory(Guid ChatId)
    {
        if (_chats.TryGetValue(ChatId, out var cachedHistory))
            return cachedHistory;

        using var context = await _contextFactory.CreateDbContextAsync();
        var history = await context.ChatHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.ChatId == ChatId);

        if (history is not null)
            _chats.TryAdd(ChatId, history);

        return history;
    }

    public async Task<ChatHistory> GetOrCreateChatHistory(Guid ChatId)
    {
        var history = await GetChatHistory(ChatId);
        if (history is not null)
            return history;

        var newHistory = new ChatHistory { ChatId = ChatId };

        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.ChatHistories.Add(newHistory);
            await context.SaveChangesAsync();
        }

        _chats.TryAdd(ChatId, newHistory);

        return newHistory;
    }

    public async Task<Guid> CreateChatHistory(ChatHistory history)
    {
        if (history.ChatId == Guid.Empty)
            history.ChatId = Guid.NewGuid();

        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.ChatHistories.Add(history);
            await context.SaveChangesAsync();
        }

        _chats.TryAdd(history.ChatId, history);

        return history.ChatId;
    }

    public async Task<bool> RemoveChatHistory(Guid ChatId)
    {
        _chats.TryRemove(ChatId, out var _);

        using var context = await _contextFactory.CreateDbContextAsync();
        var historyToRemove = await context.ChatHistories.FindAsync(ChatId);
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
        history.LastMessageTime = history.Messages.LastOrDefault()?.Timestamp;

        using var context = await _contextFactory.CreateDbContextAsync();
        context.ChatHistories.Update(history);

        var changedCount = await context.SaveChangesAsync();
        return changedCount > 0;
    }

}
