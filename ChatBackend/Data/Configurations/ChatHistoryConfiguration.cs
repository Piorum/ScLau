using System.Text.Json;
using System.Text.Json.Serialization;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBackend.Data.Configurations;

public class ChatHistoryConfiguration : IEntityTypeConfiguration<ChatHistory>
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    public void Configure(EntityTypeBuilder<ChatHistory> builder)
    {
        builder.HasKey(ch => ch.ChatId);

        builder.Property(ch => ch.Messages)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<List<ChatMessage>>(v, _jsonOptions)!
            )
            .HasColumnType("jsonb")        
            .Metadata.SetValueComparer(
                new ValueComparer<List<ChatMessage>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                )
            );

        builder.Property(ch => ch.LastChatOptions)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<ChatOptions>(v, _jsonOptions)!
            )
            .HasColumnType("jsonb");

        builder.Property(ch => ch.Title).HasField("_title").UsePropertyAccessMode(PropertyAccessMode.Field).IsRequired(false);

        builder.Property(ch => ch.LastMessageTime)
            .HasColumnType("timestamp with time zone");
        
    }
}
