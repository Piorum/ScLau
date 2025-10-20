using System.Text.Json;
using System.Text.Json.Serialization;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBackend.Data.Configurations;

public class ChatHistoryConfiguration : IEntityTypeConfiguration<ChatHistory>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter() // <-- important for enums
        }
    };

    public void Configure(EntityTypeBuilder<ChatHistory> builder)
    {
        builder.HasKey(ch => ch.Id);

        builder.Property(ch => ch.Messages)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<ChatMessage>>(v, _jsonOptions)!
            )
            .HasColumnType("jsonb");

        builder.Property(ch => ch.LastChatOptions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<ChatOptions>(v, _jsonOptions)!
            )
            .HasColumnType("jsonb");

        builder.Property(ch => ch.Title).HasField("_title").UsePropertyAccessMode(PropertyAccessMode.Field).IsRequired(false);

        builder.Ignore(ch => ch.LastMessageTime);
        
    }
}
