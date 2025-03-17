using GLV.Shared.Common;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;
using System.Threading.Channels;

namespace GLV.Shared.ChatBot.Discord;

public static class DiscordExtensions
{
    public static void UnpackDiscordUserId(this Guid userId, out ulong channel)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref userId, 1));
        channel = span[1];
    }

    public static Guid PackDiscordUserId(this IUser user)
        => MemoryMarshal.Cast<ulong, Guid>([0, user.Id])[0];

    public static void UnpackDiscordConversationId(this Guid conversationId, out ulong channel)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref conversationId, 1));
        channel = span[1];
    }

    public static Guid PackDiscordConversationId(this ICommandContext context)
        => PackDiscordConversationId(context.Channel);

    public static Guid PackDiscordConversationId(this IChannel channel)
        => MemoryMarshal.Cast<ulong, Guid>([0, channel.Id])[0];
}
