using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier;

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
						"Welcome to the ATT Discord!\nBewlow is information for getting started, as well as useful information down the line.\n\n" +
						"If you want to participate in the community, please read [#rules](https://discord.gg/Jpe9FH9) first.",
						"https://discord.gg/Jpe9FH9",
						false,
						new Color(0x1D867D));

			yield return GetEmbed("Downloading A Township Tale",
						"A Township Tale is available on both Quest and PC. You can get it here: \nhttps://townshiptale.com/download",
						"https://townshiptale.com/download",
						false,
						new Color(0xE8A623),
						builder =>
						{
							builder.WithImageUrl("https://i.imgur.com/AZpSLmC.png");
						});

			yield return GetEmbed("Finding a server",
						"After completing the tutorial, you will need to find a server to play on!\n" +
						"You can create a server yourself, play on a friends, or find one online.\n\n" +
						"You can find some quest servers in [#communities-quest](https://discord.gg/5eqZ5KP3dG).\n" +
						"You can find some pc servers in [#communities-pc](https://discord.gg/EB5wDhW).\n\n" +
						"You can also search for servers by tags in the Launcher. Click on the groups icon in the bar on the right.",
						"",
						false,
						new Color(0xE8A623));

			yield return GetEmbed("Can I support the game?",
						"There are many ways you can support the game! Here’s our top 6.\n" + 
						"- Being active in the community\n" +
						"- Telling your friends about the game\n" +
						"- Contributing to community efforts such as the [wiki](https://townshiptale.fandom.com/wiki/A_Township_Tale_Wiki)\n" +
						"- [Providing feedback and reporting bugs](https://feedback.townshiptale.com)\n" +
						"- Purchasing cosmetics (in game or in the launcher)\n" +
						"- Becoming a [Supporter](https://townshiptale.com/supporter)",
						"",
						false,
						new Color(0x76d6fd));

			yield return GetEmbed("Link your A Township Tale account with Discord!",
						"To link your account, visit " + AccountModule.LinkUrl, AccountModule.LinkUrl, false, new Color(0x7289da),
						builder =>
						{
							builder.WithImageUrl("https://i.imgur.com/o49Qzvi.png");
						});

			yield return GetEmbed("Some useful links!",
						"If you're interested in more information about the game, here are some good places to go!",
						"",
						false,
						new Color(0x1D867D),
						builder =>
						{
							builder.WithFields(
								new EmbedFieldBuilder().WithName("Quest Store").WithValue("https://www.oculus.com/experiences/quest/2913958855307200").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Tech Support").WithValue("http://tech-support.townshiptale.com/").WithIsInline(true),
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
						new Color(0x1D867D),
						builder =>
						{
							builder.WithFields(
								new EmbedFieldBuilder().WithName("Reddit").WithValue("https://reddit.com/r/TownshipTale/").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Facebook").WithValue("https://www.facebook.com/townshiptale").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Twitter").WithValue("https://twitter.com/townshiptale").WithIsInline(true),
								new EmbedFieldBuilder().WithName("Instagram").WithValue("https://www.instagram.com/townshiptale/").WithIsInline(true),
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
