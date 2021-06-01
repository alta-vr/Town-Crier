using Discord;
using Discord.WebSocket;
using LiteDB;
using System.Linq;
using System.Threading.Tasks;
using TownCrier.Database;

namespace TownCrier.Services
{
	public class CrossAlerter
	{
		DiscordSocketClient discord;
		TownDatabase database;

		public CrossAlerter(DiscordSocketClient discord, TownDatabase database)
		{
			this.discord = discord;
			this.database = database;

			discord.MessageReceived += Process;
		}

		public async Task Process(SocketMessage message)
		{
			if (message.MentionedRoles.Count == 0 || message.Author.Id == discord.CurrentUser.Id)
			{
				return;
			}

			ITextChannel channel = message.Channel as ITextChannel;

			if (channel == null)
			{
				return;
			}

			TownGuild guild = database.GetGuild(channel.Guild);

			if (guild == null)
			{
				return;
			}

			foreach (CrossAlert crossAlert in guild.CrossAlerts
				.Where(x => x.Channel != channel.Id)
				.Where(x => message.MentionedRoles.Any(role => role.Id == x.Role)))
			{
				ITextChannel targetChannel = (ITextChannel)discord.GetChannel(crossAlert.Channel);

				IUserMessage response = await targetChannel.SendMessageAsync(message.Author.Mention + " in " + channel.Mention + ": " + message.Content + "\n" + message.GetJumpUrl() );

				await response.AddReactionAsync(new Emoji("✔"));
			}
		}
	}
}
