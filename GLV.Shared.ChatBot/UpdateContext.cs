﻿using GLV.Shared.ChatBot.Pipeline;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot;

public abstract class UpdateContext(IChatBotClient client, Guid conversationId, string platform)
{
    private Dictionary<Type, object>? Features;

    public const string TelegramPlatform = "wtelegram-bot";
    public const string WhatsAppPlatform = "whatsapp-bot";
    public const string DiscordPlatform = "discord.net-bot";
    public const string TwitterPlatform = "twitter-bot";

    internal PipelineContext? pipelineContext;

    public virtual bool IsHandledByBotClient { get; set; }

    public string Platform { get; } = platform;
    public Guid ConversationId { get; } = conversationId;
    public IChatBotClient Client { get; } = client;

    /// <summary>
    /// Attempts to add the given feature to the update's collection
    /// </summary>
    /// <returns><see langword="true"/> when it succesfully adds the feature, <see langword="false"/> otherwise</returns>
    public bool TryAddFeature<T>(T feature) where T : notnull
        => (Features ??= []).TryAdd(typeof(T), feature);

    /// <summary>
    /// Attempts to add the feature, and replaces it if a feature with the matching type already exists
    /// </summary>
    /// <returns><see langword="true"/> when it replaces an existing feature, <see langword="false"/> otherwise</returns>
    public bool AddOrReplaceFeature<T>(T feature) where T : notnull
    {
        if ((Features ??= []).TryAdd(typeof(T), feature) is false)
        {
            Features[typeof(T)] = feature;
            return true;
        }

        return false;
    }

    public bool TryGetFeature<T>([MaybeNullWhen(false)] out T? feature) where T : notnull
    {
        if (Features is Dictionary<Type, object?> f && f.TryGetValue(typeof(T), out var obj))
        {
            feature = (T)obj;
            return true;
        }

        feature = default;
        return false;
    }

    public T GetRequiredFeature<T>() where T : notnull
        => Features?.TryGetValue(typeof(T), out var obj) is true ? (T)obj : throw new InvalidOperationException($"Could not locate a feature matching the type {typeof(T)}");

    public string? JumpToActiveAction { get; set; }
    public long? JumpToActiveActionStep { get; set; }

    public abstract KeyboardResponse? KeyboardResponse { get; }
    public abstract Message? Message { get; }
    public abstract MemberEvent? MemberEvent { get; }
    public abstract bool IsDirectMessage { get; }
}
