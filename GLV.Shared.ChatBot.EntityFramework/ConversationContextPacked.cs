using GLV.Shared.Data;
using GLV.Shared.EntityFramework;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace GLV.Shared.ChatBot.EntityFramework;

public sealed class ConversationContextPacked : IDbModel<ConversationContextPacked, Guid>
{
    private static readonly ConcurrentDictionary<string, Type> TypeCache = [];
    private static void RegisterType(Type type)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type.AssemblyQualifiedName, nameof(type));
        TypeCache.TryAdd(type.AssemblyQualifiedName, type);
    }

    private static Type FetchType(string assemblyQualifiedName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assemblyQualifiedName, nameof(assemblyQualifiedName));
        if (TypeCache.TryGetValue(assemblyQualifiedName, out var type) is false)
        {
            type = AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(x => x.GetTypes())
                                          .Where(x => x.AssemblyQualifiedName == assemblyQualifiedName)
                                          .SingleOrDefault();
            if (type is null)
                throw new KeyNotFoundException($"Could not find a type by the name of '{assemblyQualifiedName}'");
            TypeCache.TryAdd(assemblyQualifiedName, type);
        }
        return type;
    }

    public required Guid ConversationId { get; init; }
    public required long Step { get; init; }
    public required string? ActiveAction { get; init; }
    public required string Encoding { get; init; }
    public required string AssemblyQualifiedContextTypeName { get; init; }
    public required string JsonData { get; init; }

    public static ConversationContextPacked Pack(ConversationContext context)
    {
        var contextType = context.GetType();
        var json = JsonSerializer.Serialize(context, contextType);
        RegisterType(contextType);
        return new ConversationContextPacked()
        {
            ConversationId = context.ConversationId,
            Step = context.Step,
            ActiveAction = context.ActiveAction,
            Encoding = "json",
            AssemblyQualifiedContextTypeName = contextType.AssemblyQualifiedName!,
            JsonData = json,
        };
    }

    public ConversationContext Unpack()
    {
        Debug.Assert(string.IsNullOrWhiteSpace(AssemblyQualifiedContextTypeName) is false);
        var contextType = FetchType(AssemblyQualifiedContextTypeName);
        var context = (ConversationContext)JsonSerializer.Deserialize(JsonData, contextType)!;
        context.SetState(Step, ActiveAction);
        return context;
    }

    Guid IKeyed<ConversationContextPacked, Guid>.Id => ConversationId;

    public static void BuildModel(DbContext context, EntityTypeBuilder<ConversationContextPacked> mb)
    {
        mb.HasKey(x => x.ConversationId);
        mb.HasIndex(x => x.AssemblyQualifiedContextTypeName).IsUnique(false);
        mb.HasIndex(x => x.ActiveAction).IsUnique(false);
    }
}
