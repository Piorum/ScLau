using ChatBackend.Models;

namespace ChatBackend.Interfaces;

public interface IChatCache
{
    IEnumerable<object> ListChats();
    Task<ChatHistory?> GetChatHistory(Guid key);
    Task<ChatHistory> GetOrCreateChatHistory(Guid key);
    Task<Guid> CreateChatHistory(ChatHistory history);
    Task<bool> RemoveChatHistory(Guid key);
    Task<bool> UpdateChatHistory(ChatHistory history);


}
