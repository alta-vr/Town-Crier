using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
	[Group("welcome")]
	public class WelcomeModule : InteractiveBase<SocketCommandContext>
	{
		[Command("test")]
		public async Task Welcome(bool isPrivate)
		{
			foreach (Embed embed in GetEmbeds())
			{
				if (isPrivate)
				{
					await Context.User.SendMessageAsync(embed: embed);
				}
				else
				{ 
					await ReplyAsync(embed: embed);
				}
			}
		}

		public static async Task SendTo(IUser user)
		{
			foreach (Embed embed in GetEmbeds())
			{
				await user.SendMessageAsync(embed: embed);
			}
		}

		public static IEnumerable<Embed> GetEmbeds()
		{
			yield return GetEmbed("Welcome to A Township Tale!",
						"Welcome to the ATT Discord!\nI've added some information below including where to download the game, where to find help, and other bits of information!\n\n" +
						"Be sure to check out the Discord [#rules](https://discord.gg/Jpe9FH9).\n\n" +
						"To play, you'll need to download, complete the tutorial, and the find a server to join.\n\n" +
						"A Township Tale can be downloaded through the [Alta Launcher](https://townshiptale.com/launcher)!",
						"https://townshiptale.com/launcher",
						true,
						new Color(0xC9881E),
						builder =>
						{
							builder.WithImageUrl("https://i.imgur.com/AZpSLmC.png");
						});

			yield return GetEmbed("Finding a server",
						"There are numerous A Township Tale servers!\n" +
						"Some are vanilla, while others have modified the experience in various ways.\n\n" +
						"You can find some in [#community-servers](https://discord.gg/EB5wDhW).\n\n" +
						"You can also search for servers by tags in the Launcher. Click on the groups icon in the bar on the right.",
						"",
						true,
						new Color(0xC9881E));

			yield return GetEmbed("Can I support the game?",
						"Your feedback is extremely helpful!\nCheck out the [Feedback Forum](https://feedback.townshiptale.com), and the [#fbi-discussion](https://discord.gg/eWsbcXb) channel!\n\nIf you would like to support financially, you can check out the cosmetic store (in the Alta Launcher) and/or [become a supporter](https://townshiptale.com/supporter)!",
						"",
						false,
						new Color(0xC9881E));

			yield return GetEmbed("Some useful links!",
						"If you're interested in more information about the game, here are some good places to go!",
						"",
						false,
						new Color(0xC9881E),
						builder =>
						{
							builder.WithFields(
								new EmbedFieldBuilder().WithName("Feedback").WithValue("https://feedback.townshiptale.com").WithIsInline(true),
								new EmbedFieldBuilder().WithName("FAQ").WithValue("https://trello.com/b/Dnaxu0Mk/a-township-tale-faq-help").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Roadmap").WithValue("https://trello.com/b/0rQGM8l4/a-township-tales-roadmap").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Wiki").WithValue("https://townshiptale.gamepedia.com/A_Township_Tale_Wiki").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Youtube").WithValue("https://www.youtube.com/townshiptale").WithIsInline(true));
						});

			yield return GetEmbed("Social Links!",
						"",
						"",
						false,
						new Color(0xC9881E),
						builder =>
						{
							builder.WithFields(
								new EmbedFieldBuilder().WithName("Reddit").WithValue("https://reddit.com/r/TownshipTale/").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Facebook").WithValue("https://www.facebook.com/townshiptale").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Twitter").WithValue("https://twitter.com/townshiptale").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Discord").WithValue("https://discord.gg/townshiptale").WithIsInline(true));
						});
		}

		static Embed GetEmbed(string title, string message, string url, bool isAppendingUrl, Color color)
		{
			return GetEmbed(title, message, url, isAppendingUrl, color, builder => { });
		}

		static Embed GetEmbed(string title, string message, string url, bool isAppendingUrl, Color color, Action<EmbedBuilder> modify)
		{
			var builder = new EmbedBuilder()
			.WithColor(color)
			//.WithThumbnailUrl()
			.WithAuthor(author =>
			{
				author
				.WithName(title)
				.WithUrl("https://discord.gg/townshiptale");
			});

			if (isAppendingUrl)
			{
				message += '\n' + url;
			}

			builder.WithDescription(message);

			//builder.WithUrl(url);
			//builder.AddField("Field Name", "Field Value", true);
			//builder.AddField("Field Name", "Field Value", false);

			modify(builder);


			return builder.Build();
		}
	}
}
