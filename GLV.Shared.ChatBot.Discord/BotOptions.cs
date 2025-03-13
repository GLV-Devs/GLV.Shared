using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.ChatBot.Discord;

public class DiscordBotCredentials
{
    public ulong ApplicationId { get; set; }
    public string? PublicKey { get; set; }
    public string? BotToken { get; set; }
}