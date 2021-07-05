
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier.Modules
{
	public class PlaytimeModule : InteractiveBase<SocketCommandContext>
	{
		AltaAPI AltaAPI { get; set; }

		public TownDatabase Database { get; set; }

		public PlaytimeModule(AltaAPI altaAPI, TownDatabase database)
		{
			AltaAPI = altaAPI;
			Database = database;
		}

		[Command("playtime")]
		public async Task PlayTime()
		{
			await PlayTime2(Context.User);
		}

		[Command("playtime"), RequireUserPermission(GuildPermission.BanMembers)]
		public async Task PlayTime2(IUser userArg)
		{
			TownUser user = Database.GetUser(userArg);

			await Database.RefreshAltaUser(user);

			if (user == null || user.AltaId <= 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account!");
			}
			else
			{
				var stats = await AltaAPI.ApiClient.UserClient.GetUserStatisticsAsync(user.AltaId);

				await ReplyAsync("Play time: " + stats.PlayTime.TotalHours.ToString("0.00") + " hours");
			}
		}
	}

	public class InfoModule : InteractiveBase<SocketCommandContext>
	{
		public TownDatabase Database { get; set;  }

		[Command("website"), Alias("web", "site")]
		public Task Website()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.com/\n");

		[Command("blog"), Alias("news")]
		public Task Blog()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.com/news/\n");

		[Command("wiki")]
		public Task Wiki()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.fandom.com/wiki/A_Township_Tale_Wiki/\n");

		[Command("invite")]
		public Task Invite()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://discord.gg/townshiptale\n");

		[Command("reddit")]
		public Task Reddit()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://reddit.com/r/townshiptale\n");

		[Command("twitter")]
		public Task Twitter()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://twitter.com/townshiptale\n");

		[Command("instagram"), Alias("insta")]
		public Task Instagram()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://www.instagram.com/townshiptale/\n");

		[Command("reset-password")]
		public Task ResetPassword()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.com/reset-password\n");

		[Command("download")]
		public Task Download()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.com/download\n");

		[Command("launcher")]
		public Task Launcher()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://townshiptale.com/launcher\n");


		[Command("link")]
		public Task Link()
			=> ReplyAsync(
				$"Were you looking for this?\n" + AccountModule.LinkUrl);

		[Command("feedback"), Alias("bugs", "fbi", "ideas")]
		public Task Feedback()
			=> ReplyAsync(
				$"Were you looking for this?\nhttps://feedback.townshiptale.com\n");

		[Command("store")]
		public Task Store()
			=> ReplyAsync(
				$"Check out the store in game, or in the Alta Launcher:\n<alta://game/1/shop>\n");

		class TechSupportSearch
		{
			public class Pages
			{
				public int page;
				public int per_page;
				public int total_pages;
			}

			public class Result
			{
				public int id;
				public string slug;
				public string title;
				public string body;
				public string keywords;
				public string title_tag;
				public string meta_description;
				public int category_id;
				public int user_id;
				public bool active;
				public int rank;
				public string version;
				public bool front_page;
				public DateTime created_at;
				public DateTime updated_at;
				public int topics_count;
				bool allow_comments;
				//category - has more fields...
			}

			public Pages pages;

			public Result[] results;
		}

		[Command("tech-support"), Alias("tech")]
		public async Task TechSupport([Remainder]string search = null)
		{
			if (string.IsNullOrEmpty(search))
			{
				await ReplyAsync(
					$"Were you looking for this?\nhttp://tech-support.townshiptale.com\n");
			}
			else
			{
				var client = new RestClient("http://tech-support.townshiptale.com/api/v1/search?type=Doc&token=0869ef8ce3932c22559b97f580cdbcd1&q=" + search);
				var request = new RestRequest(Method.GET);

				IRestResponse response = client.Execute(request);

				TechSupportSearch result = JsonConvert.DeserializeObject<TechSupportSearch>(response.Content);

				StringBuilder message = new StringBuilder();

				if (result.results.Length > 0)
				{
					message.AppendLine("Here are some articles I've found!\n");

					foreach (TechSupportSearch.Result item in result.results)
					{
						message.Append("**");
						message.Append(item.title);
						message.AppendLine("**");
						message.Append("http://tech-support.townshiptale.com/en/docs/");
						message.AppendLine(item.slug);
						message.AppendLine();
					}

					await ReplyAsync(message.ToString());
				}
				else
				{
					await ReplyAsync(
						$"No results found!\nMaybe search manually here:\nhttp://tech-support.townshiptale.com\n");
				}
			}
		}

		[Command("supporter"), Alias("support", "donate")]
		public Task Supporter()
			=> ReplyAsync(
				"To become a supporter, visit the supporter page on the website:\nhttps://townshiptale.com/supporter\nOr in the launcher:\n<alta://game/1/supporter>");

		[Command("meta")]
		public Task Meta()
			=> ReplyAsync("Were you looking for this?\nhttps://discord.gg/GNpmEN2");

		[Command("github")]
		public Task Github()
			=> ReplyAsync("Were you looking for this?\nhttp://github.com/alta-vr");

		[Command("quest")]
		public Task Quest()
			=> ReplyAsync("Coming July 15th. Wishlist now!\nhttps://www.oculus.com/experiences/quest/2913958855307200");

		[Command("dashboard"), Alias("dash")]
		public Task Dashboard()
			=> ReplyAsync("Use the dashboard here:\nhttp://dash.townshiptale.com\n\nOr the old dashboard here:\nhttp://dashboard.townshiptale.com");

		class TrelloCard
		{
			public string name;
			public string url;
		}

		[Command("faq")]
		public async Task Faq([Remainder]string query = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				await ReplyAsync($"Were you looking for this?\n<https://trello.com/b/Dnaxu0Mk/a-township-tale-faq-help>\n");
			}
			else
			{
				query = query.ToLower();

				var client = new RestClient("https://api.trello.com/1/boards/Dnaxu0Mk/cards/visible?key=3e7b77be622f7578d998feb1e663561b&token=83df6272cd4b14650b15fc4d6a9960c6090da2ea1287e5cbce09b99d9549fc61");
				var request = new RestRequest(Method.GET);

				IRestResponse response = client.Execute(request);

				TrelloCard[] cards = JsonConvert.DeserializeObject<TrelloCard[]>(response.Content);

				foreach (TrelloCard card in cards)
				{
					if (card.name.ToLower().Contains(query))
					{
						await ReplyAsync($"Were you looking for this?\n{card.url}\n");
						return;
					}
				}

				await ReplyAsync($"Were you looking for this?\n<https://trello.com/b/Dnaxu0Mk/a-township-tale-faq-help>\n");
			}
		}

		[Command("roadmap")]
		public async Task Roadmap([Remainder]string query = null)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				await ReplyAsync($"Were you looking for this?\n<https://trello.com/b/0rQGM8l4/a-township-tales-roadmap>\n");
			}
			else
			{
				query = query.ToLower();

				var client = new RestClient("https://api.trello.com/1/boards/0rQGM8l4/cards/visible?key=3e7b77be622f7578d998feb1e663561b&token=83df6272cd4b14650b15fc4d6a9960c6090da2ea1287e5cbce09b99d9549fc61");
				var request = new RestRequest(Method.GET);

				IRestResponse response = client.Execute(request);

				TrelloCard[] cards = JsonConvert.DeserializeObject<TrelloCard[]>(response.Content);

				foreach (TrelloCard card in cards)
				{
					if (card.name.ToLower().Contains(query))
					{
						await ReplyAsync($"Were you looking for this?\n{card.url}\n");
						return;
					}
				}

				await ReplyAsync($"Were you looking for this?\n<https://trello.com/b/0rQGM8l4/a-township-tales-roadmap>\n");
			}
		}

		IRole spamRole;

		[Command("joined")]
		public async Task Joined()
		{
			TownUser user = Database.GetUser(Context.User);
						
			await ReplyAsync($"{Context.User.Mention} joined on {user.InitialJoin.ToString("dd/MMM/yyyy")}");
		}

		[Command("joined"), RequireUserPermission(GuildPermission.KickMembers)]
		public async Task Joined(IUser discordUser)
		{
			TownUser user = Database.GetUser(discordUser);

			await ReplyAsync($"{discordUser.Username} joined on {user.InitialJoin.ToString("dd/MMM/yyyy")}");
		}


		[Command("userlist")]
		public async Task UserList()
		{
			if (Context.Guild == null)
			{
				return;
			}

			if (!(Context.User as IGuildUser).RoleIds.Contains<ulong>(334935631149137920))
			{
				return;
			}

			await ReplyAsync("Starting...");

			StringBuilder result = new StringBuilder();

			result
				.Append("ID")
				.Append(',')
				.Append("Username")
				.Append(',')
				.Append("Nickname")
				.Append(',')
				.Append("Joined")
				.Append(',')
				.Append("Last Message")
				.Append(',')
				.Append("Score")
				.Append('\n');

			foreach (IGuildUser user in (Context.Guild as SocketGuild).Users)
			{
				TownUser townUser = Database.GetUser(user);

				result
					.Append(user.Id)
					.Append(',')
					.Append(user.Username.Replace(',', '_'))
					.Append(',')
					.Append(user.Nickname?.Replace(',', '_'))
					.Append(',')
					.Append(townUser.InitialJoin.ToString("dd-MM-yy"))
					.Append(',')
					.Append(townUser.Scoring?.LastMessage.ToString("dd-MM-yy"))
					.Append('\n');
			}

			System.IO.File.WriteAllText("D:/Output/Join Dates.txt", result.ToString());

			await ReplyAsync("I'm done now :)");
		}

		[Command("follow"), Alias("optin", "keepmeposted")]
		public async Task OptIn()
		{
			if (Context.Guild == null)
			{
				await ReplyAsync("You must call this from within a server channel.");
				return;
			}

			IGuildUser user = Context.User as IGuildUser;
			IRole role = Context.Guild.Roles.FirstOrDefault(test => test.Name == "followers");

			if (role == null)
			{
				await ReplyAsync("Role not found");
				return;
			}

			if (user.RoleIds.Contains(role.Id))
			{
				await ReplyAsync("You are already a follower!\nUse !unfollow to stop following.");
				return;
			}

			await user.AddRoleAsync(role);
			await ReplyAsync("You are now a follower!");
		}

		[Command("unfollow"), Alias("optout", "leavemealone")]
		public async Task OptOut()
		{
			if (Context.Guild == null)
			{
				await ReplyAsync("You must call this from within a server channel.");
				return;
			}

			IGuildUser user = Context.User as IGuildUser;
			IRole role = Context.Guild.Roles.FirstOrDefault(test => test.Name == "followers");

			if (role == null)
			{
				await ReplyAsync("Role not found");
				return;
			}

			if (!user.RoleIds.Contains(role.Id))
			{
				await ReplyAsync("You aren't a follower!\nUse !follow to start following.");
				return;
			}

			await user.RemoveRoleAsync(role);
			await ReplyAsync("You are no longer a follower.");
		}

		//[Command("help"), Alias("getstarted", "gettingstarted")]
		//public async Task GetStarted()
		//{
		//	List<string> commands = new List<string>();
		//	List<string> descriptions = new List<string>();

		//	string message = $"Welcome! I am the Town Crier.\n" +
		//		$"I can help with various tasks.\n\n" +
		//		$"Here are some useful commands:\n\n";

		//	commands.Add("help");
		//	descriptions.Add("In case you get stuck");

		//	commands.Add("follow");
		//	descriptions.Add("Get alerted with news.");

		//	commands.Add("blog");
		//	descriptions.Add("For a good read");

		//	commands.Add("whois [developer]");
		//	descriptions.Add("A brief bio on who a certain developer is");

		//	commands.Add("flip");
		//	descriptions.Add("Flip a coin!");

		//	commands.Add("roll");
		//	descriptions.Add("Roll a dice!");


		//	//commands.Add("tc help");
		//	//descriptions.Add("An introduction to A Chatty Township Tale");

		//	message += ShowCommands("!", commands, descriptions);

		//	await ReplyAsync(message);
		//	//RestUserMessage messageResult = (RestUserMessage)
		//	//await messageResult.AddReactionAsync(Emote.Parse("<:hand_splayed:360022582428303362>"));
		//}

	}
}
