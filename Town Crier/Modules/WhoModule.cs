using System.Threading.Tasks;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System;
using Discord.Addons.Interactive;
using TownCrier.Database;
using TownCrier.Services;
using Discord.WebSocket;

namespace DiscordBot.Modules
{
	[Group("who")]
	public class WhoModule : InteractiveBase<SocketCommandContext>
	{
		public TownDatabase Database { get; set; }

		//[Command("help")]
		//public async Task Help()
		//{
		//	List<string> commands = new List<string>();
		//	List<string> descriptions = new List<string>();

		//	string message = $"Here are the things you'll need to know:\n\n";

		//	commands.Add("am i");
		//	descriptions.Add("Tells you what you want to hear");

		//	commands.Add("i am is [description]");
		//	descriptions.Add("Defines who you (think) you are");

		//	commands.Add("is [target]");
		//	descriptions.Add("Tells you who someone is");

		//	message += ShowCommands("!who ", commands, descriptions);

		//	await ReplyAsync(message);
		//}

		[Summary("Tells you what you want to hear")]
		[Command("am i")]
		public async Task AmI()
		{
			TownUser user = Database.GetUser(Context.User);

			await ReplyAsync(user.Name + " is " + (user.Description ?? "... You?"));
		}

		[Summary("Defines who you (think) you are")]
		[Command("i am is")]
		public async Task Set([Remainder]string description)
		{
			if (description.Length > 120)
			{
				await ReplyAsync("Limit of 120 characters");
				return;
			}

			TownUser user = Database.GetUser(Context.User);

			user.Description = description;

			Database.Users.Update(user);
		}

		[Summary("Commands to tell you who someone is")]
		[Group("is")]
		public class Is : InteractiveBase<SocketCommandContext>
		{
			public TownDatabase Database { get; set; }


			[Command("tima")]
			public async Task Tima() => await ReplyAsync("Tima is the CEO of Alta. He doesn't do much.");

			[Command("boramy"), Alias("Bossun")]
			public async Task Boramy() => await ReplyAsync("Boramy is the Lead Designer of the game. He dreams up things then expects them to get done.");

			[Command("joel"), Alias("Narmdo")]
			public async Task Joel() => await ReplyAsync("Joel is the Lead Programmer of the game. He gets told to stop dreaming and start programming.");

			[Command("timo")]
			public async Task Timo() => await ReplyAsync("Timo is the Server Infrastructure Programmer of the game. If servers go down, blame him.");

			[Command("victor"), Alias("Vic Eca", "Viceca")]
			public async Task Victor() => await ReplyAsync("Victor is the Tools + Gameplay Programmer of the game. If you find yourself dying in game, it's because of him.");

			[Command("serena"), Alias("Sbanana")]
			public async Task Serena() => await ReplyAsync("Serena is the Technical Artist of the game. She ups the prettiness, and downs the lagginess.");

			[Command("sol"), Alias("eric")]
			public async Task Sol() => await ReplyAsync("Sol is the wielder of the ban hammer. He is here to help, be awesome, and kick you if you spam :)");

			[Command("lefnire"), Alias("tyler")]
			public async Task Lefnire() => await ReplyAsync("Lefnire is a web and server guru. He's helping keep an eye on the servers, and find problems with the game!");

			[Command("ozball")]
			public async Task Ozball() => await ReplyAsync("Ozball is the wielder of Ban Hammer Jr. Ozball's his name, dealing with bad people's his game (though he prefers ATT).");

			[Command("town crier"), Alias("you")]
			public async Task TownCrier() => await ReplyAsync("I'm the trustworthy Town Crier! Some may find me annoying, but I swear, I'm here to help!");

			[Command("alta"), Alias("company", "team", "developer")]
			public async Task Alta() => await ReplyAsync("Alta is a VR game development studio based in Sydney! We are (surprise, surprise) working on A Township Tale. The team consists of me (most importantly), and several others. You can see their names in orange.");

			static readonly string[] UnknownReplies = new string[]
			{
				"Ahh yes... {0}...",
				"Oh gosh, not {0}...",
				"Do we have to talk about {0}?",
				"Do I look like I associate with {0}s?",
				"Let me google that for you... http://lmgtfy.com/?q=who+is+{0}",
				"Someone named their kid {0}? Wow.",
				"What a terrifying name! {0}? Urgh. *shivers*",
				"{0}? Never met them thankfully.",
				"It's been a long time since last talked to {0}. Thank goodness.",
				"Which {0}? The cool one, or the one in this Discord?",
				"{0}? What a coincidence, I was just thinking about them.",
				"The real question is, who is {1}?",
				"{1} asking who {0} is. Classic.",
			};

			[Summary("Tells you who someone is")]
			[Command]
			[Priority(-1)]
			public async Task IsCommand(string person)
			{
				//SocketGuildUser discordUser = null;

				SocketGuildUser discordUser = Context.Guild.Users.FirstOrDefault(item => (item.Nickname == null) ? item.Username.ToLower().Contains(person.ToLower()) : item.Nickname.ToLower().Contains(person.ToLower()));



				//if(discordUser == null)
				//{
				//	discordUser = Context.Guild.Users.FirstOrDefault(item => item.Username.Equals(person, StringComparison.OrdinalIgnoreCase));
				//}
				//TownUser user = Database.Users.FindOne(item => item.Name == person);

				TownUser user = Database.Users.FindById(discordUser.Id);

				if (user == null)
				{
					Random random = new Random();

					await ReplyAsync(string.Format(UnknownReplies[random.Next(UnknownReplies.Length)], (discordUser.Nickname == null) ? discordUser.Username : discordUser.Nickname, Context.User.Mention));
					return;
				}

				if (user.Description == null)
				{
					await ReplyAsync("I've met them, they didn't tell me much about themselves though.");
					return;
				}

				await ReplyAsync((discordUser.Nickname == null) ? discordUser.Username + " is " + user.Description : discordUser.Nickname + " is " + user.Description);
			}
		}
	}
}
