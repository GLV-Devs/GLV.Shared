using GLV.Shared.Common;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Discord;
using Discord.Commands;

namespace GLV.Shared.ChatBot.Discord;

public static class DiscordExtensions
{
    public static void UnpackDiscordGuildConversationId(this Guid conversationId, out ulong guild, out ulong channel)
    {
        var span = MemoryMarshal.Cast<Guid, ulong>(MemoryMarshal.CreateSpan(ref conversationId, 1));
        guild = span[0];
        channel = span[1];
    }

    public static Guid PackDiscordGuildConversationId(this ICommandContext context)
        => PackDiscordGuildConversationId(context.Guild, context.Channel);

    public static Guid PackDiscordGuildConversationId(IGuild guild, IChannel channel) 
        => MemoryMarshal.Cast<ulong, Guid>( [ guild.Id, channel.Id ] )[0];
}
