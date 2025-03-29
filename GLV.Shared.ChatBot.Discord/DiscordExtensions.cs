using GLV.Shared.Common;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using System.Threading.Channels;
using System;

namespace GLV.Shared.ChatBot.Discord;

public static class DiscordExtensions
{
    public static UserInfo? GetUserInfo(this IUser user)
        => user is not null
            ? new UserInfo(
                user.Username,
                user.GlobalName,
                user.PackDiscordUserId()
            )
            : null;

    public static void UnpackDiscordGuildChannelId(this Guid guildId, out ulong guild, out ulong channel)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref guildId, 1));
        guild = span[0];
        channel = span[1];
    }

    public static void UnpackDiscordGuildId(this Guid guildId, out ulong guild)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref guildId, 1));
        guild = span[0];
    }

    public static void UnpackDiscordGuildUserId(this Guid userId, out ulong guild, out ulong user)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref userId, 1));
        user = span[1];
        guild = span[0];
    }

    public static void UnpackDiscordUserId(this Guid userId, out ulong user)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref userId, 1));
        user = span[1];
    }

    public static Guid PackDiscordUserId(this IUser user)
        => MemoryMarshal.Cast<ulong, Guid>([0, user.Id])[0];

    public static void UnpackDiscordConversationId(this Guid conversationId, out ulong guild, out ulong channel)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref conversationId, 1));
        channel = span[1];
        guild = span[0];
    }

    public static Guid PackDiscordGuildUserId(this IGuildUser user)
        => MemoryMarshal.Cast<ulong, Guid>([user.GuildId, user.Id])[0];

    public static Guid PackDiscordGuildUserId(this IUser user, IGuild guild)
        => MemoryMarshal.Cast<ulong, Guid>([guild.Id, user.Id])[0];

    public static Guid PackDiscordGuildUserId(this IGuild guild, IUser user)
        => MemoryMarshal.Cast<ulong, Guid>([guild.Id, user.Id])[0];

    public static Guid PackDiscordGuildChannelId(this IChannel channel, IGuild guild)
        => MemoryMarshal.Cast<ulong, Guid>([guild.Id, channel.Id])[0];

    public static Guid PackDiscordGuildChannelId(this IGuild guild, IChannel channel)
        => MemoryMarshal.Cast<ulong, Guid>([guild.Id, channel.Id])[0];

    public static Guid PackDiscordGuildId(this IGuild guild)
        => MemoryMarshal.Cast<ulong, Guid>([guild.Id, 0])[0];

    public static Guid PackDiscordConversationId(this ICommandContext context)
        => PackDiscordConversationId((IGuildChannel)context.Channel);

    public static Guid PackDiscordConversationId(this IGuildChannel channel)
        => MemoryMarshal.Cast<ulong, Guid>([channel.GuildId, channel.Id])[0];

    public static Guid PackDiscordConversationId(this IDMChannel channel)
        => MemoryMarshal.Cast<ulong, Guid>([0, channel.Id])[0];
}
