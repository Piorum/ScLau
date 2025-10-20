using ChatBackend.Interfaces;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ChatBackend.Data;

public class ChatCache(IDbContextFactory<ChatContext> contextFactory) : IChatCache, IDisposable
{
    private readonly IDbContextFactory<ChatContext> _contextFactory = contextFactory;
    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = 1024
    });

    private readonly MemoryCacheEntryOptions defaultCacheOptions = new() {
        SlidingExpiration = TimeSpan.FromMinutes(5),
        Size = 1
    };

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

    public async Task<ChatHistory?> GetChatHistory(Guid chatId)
    {
        if (_cache.TryGetValue(chatId, out ChatHistory? cachedHistory))
            return cachedHistory;

        using var context = await _contextFactory.CreateDbContextAsync();
        var history = await context.ChatHistories
            .AsNoTracking()
            .FirstOrDefaultAsync(ch => ch.ChatId == chatId);

        if (history is not null)
            _cache.Set(chatId, history, defaultCacheOptions);

        return history;
    }

    public async Task<ChatHistory> GetOrCreateChatHistory(Guid chatId)
    {
        var history = await GetChatHistory(chatId);
        if (history is not null)
            return history;

        var newHistory = new ChatHistory { ChatId = chatId };

        using (var context = await _contextFactory.CreateDbContextAsync())
        {
            context.ChatHistories.Add(newHistory);
            await context.SaveChangesAsync();
        }

        _cache.Set(chatId, newHistory, defaultCacheOptions);

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

        _cache.Set(history.ChatId, history, defaultCacheOptions);

        return history.ChatId;
    }

    public async Task<bool> RemoveChatHistory(Guid chatId)
    {
        _cache.Remove(chatId);

        using var context = await _contextFactory.CreateDbContextAsync();
        var historyToRemove = await context.ChatHistories.FindAsync(chatId);
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
        var changed = changedCount > 0;

        if (changed)
            _cache.Set(history.ChatId, history, defaultCacheOptions);

        return changed;
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }
}
