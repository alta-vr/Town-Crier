using Discord;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace TownCrier.Services
{
    public class GettingStartedService
	{
		DiscordSocketClient discord;

		public GettingStartedService(DiscordSocketClient discord)
		{
			this.discord = discord;
		}
	}
}
