using Alta.WebApi.Models;
using Alta.WebApi.Models.DTOs.Responses;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot;
using DiscordBot.Modules.ChatCraft;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot
{
	public class RequireAdminAttribute : PreconditionAttribute
	{
		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
		{
			Player player = ChatCraft.Instance.GetPlayer(context.User);

			if (!player.isAdmin)
			{
				return PreconditionResult.FromError("You are not an admin.");
			}

			return PreconditionResult.FromSuccess();
		}
	}

	public abstract class ChatCraftTypeReader<T> : TypeReader
	{
		public static string LastInput { get; private set; }

		public static T LastValue { get; private set; }

		public ICommandContext Context { get; private set; }

		public override Task<TypeReaderResult> ReadAsync(ICommandContext context, string input, IServiceProvider services)
		{
			Context = context;

			string toLower = input.ToLower();

			string error = null;
			T result = Find(ChatCraft.Instance.State, toLower, ref error);

			if (result == null)
			{
				if (error == null)
				{
					return Task.FromResult(TypeReaderResult.FromError(CommandError.ObjectNotFound, $"That is not {GetName()}."));
				}

				return Task.FromResult(TypeReaderResult.FromError(CommandError.Unsuccessful, error));
			}

			LastInput = input;
			LastValue = result;

			return Task.FromResult(TypeReaderResult.FromSuccess(result));
		}

		public abstract T Find(ChatCraftState state, string nameToLower, ref string error);

		public string GetName()
		{
			string name = typeof(T).Name.ToString().ToLower();

			char first = name[0];

			if (first == 'a' ||
				first == 'e' ||
				first == 'i' ||
				first == 'o' ||
				first == 'u')
			{
				name = "an " + name;
			}
			else
			{
				name = "a " + name;
			}

			return name;
		}
	}

	public abstract class SimpleChatCraftTypeReader<T> : ChatCraftTypeReader<T>
	{
		public override T Find(ChatCraftState state, string nameToLower, ref string error)
		{
			Func<T, bool> check = GetCheck(nameToLower, ref error);

			if (check == null)
			{
				return default(T);
			}

			return GetList(state).FirstOrDefault(check);
		}

		public abstract List<T> GetList(ChatCraftState state);

		public abstract Func<T, bool> GetCheck(string nameLower, ref string error);
	}

	public class SlotTypeReader : SimpleChatCraftTypeReader<Slot>
	{
		public override Func<Slot, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.names.Contains(nameLower);
		}

		public override List<Slot> GetList(ChatCraftState state)
		{
			return state.slots;
		}
	}

	public class UnitTypeReader : ChatCraftTypeReader<Unit>
	{
		public override Unit Find(ChatCraftState state, string nameToLower, ref string error)
		{
			Player player = ChatCraft.Instance.GetPlayer(Context.User);

			if (player.combatState == null)
			{
				error = "You are not in combat!";
				return null;
			}

			if (nameToLower.StartsWith("<@!") && nameToLower.Length > 5)
			{
				string number = nameToLower.Substring(3, nameToLower.Length - 4);

				ulong id;

				if (ulong.TryParse(number, out id))
				{
					IUser user = Context.Guild.GetUserAsync(id).Result;

					if (user != null)
					{
						return ChatCraft.Instance.GetPlayer(user);
					}
				}
			}

			return (from team in player.combatState.instance.teams
					from Unit item in team.currentUnits
					where item.name.ToLower() == nameToLower
					select item).FirstOrDefault();
		}
	}

	public class ItemTypeReader : SimpleChatCraftTypeReader<Item>
	{
		public override Func<Item, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<Item> GetList(ChatCraftState state)
		{
			return state.items;
		}
	}

	public class RecipeTypeReader : SimpleChatCraftTypeReader<Recipe>
	{
		public override Func<Recipe, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<Recipe> GetList(ChatCraftState state)
		{
			return state.recipes;
		}
	}

	public class LocationTypeReader : SimpleChatCraftTypeReader<Location>
	{
		public override Func<Location, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<Location> GetList(ChatCraftState state)
		{
			return state.locations;
		}
	}

	public class EncounterSetTypeReader : SimpleChatCraftTypeReader<EncounterSet>
	{
		public override Func<EncounterSet, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<EncounterSet> GetList(ChatCraftState state)
		{
			return state.encounterSets;
		}
	}

	public class ItemSetTypeReader : SimpleChatCraftTypeReader<ItemSet>
	{
		public override Func<ItemSet, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<ItemSet> GetList(ChatCraftState state)
		{
			return state.itemSets;
		}
	}

	public class RecipeSetTypeReader : SimpleChatCraftTypeReader<RecipeSet>
	{
		public override Func<RecipeSet, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<RecipeSet> GetList(ChatCraftState state)
		{
			return state.recipeSets;
		}
	}

	public class ExploreSetTypeReader : SimpleChatCraftTypeReader<ExploreSet>
	{
		public override Func<ExploreSet, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<ExploreSet> GetList(ChatCraftState state)
		{
			return state.exploreSets;
		}
	}

	public class StatTypeReader : SimpleChatCraftTypeReader<Stat>
	{
		public override Func<Stat, bool> GetCheck(string nameLower, ref string error)
		{
			return test => test.name.ToLower() == nameLower;
		}

		public override List<Stat> GetList(ChatCraftState state)
		{
			return state.stats;
		}
	}

	public abstract class LimitedAttribute : ParameterPreconditionAttribute
	{
		protected abstract Type Type { get; }

		protected virtual IEnumerable<Type> Types { get { return null; } }

		protected ICommandContext Context { get; private set; }

		protected ParameterInfo ParameterInfo { get; private set; }

		protected IServiceProvider Services { get; private set; }

		protected object Value { get; private set; }

		public override async Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, ParameterInfo parameter, object value, IServiceProvider services)
		{
			if (Type != null)
			{
				if (!Type.IsAssignableFrom(parameter.Type))
				{
					return PreconditionResult.FromError($"{GetType().Name} can only be used on {Type.Name} parameters. Not {parameter.Type}.");
				}
			}
			else
			{
				if (!Types.Any(test => test.IsAssignableFrom(parameter.Type)))
				{
					return PreconditionResult.FromError($"{GetType().Name} can only be used on set types. {parameter.Type} is not one of them.");
				}
			}

			Value = value;
			Context = context;
			ParameterInfo = parameter;
			Services = services;

			if (value != null)
			{
				Player player = ChatCraft.Instance.GetPlayer(context.User);

				if (MeetsCondition(player, value))
				{
					return PreconditionResult.FromSuccess();
				}
			}

			return PreconditionResult.FromError(GetError());
		}

		protected abstract bool MeetsCondition(Player player, object value);

		protected abstract string GetError();
	}

	public class InCombatWith : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Unit); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Unit otherUnit = value as Unit;

			return player.combatState != null &&
				player.combatState.instance.teams.Any(team => team.currentUnits.Contains(otherUnit));
		}

		protected override string GetError()
		{
			return $"You are not in combat with { ((IUser)Value).Username }.";
		}
	}

	public class AllyAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Unit); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Unit unit = value as Unit;

			bool isInCombat = player.combatState != null &&
				player.combatState.instance.teams[player.combatState.teamIndex].currentUnits.Contains(unit);

			return isInCombat;
		}

		protected override string GetError()
		{
			return $"They are not an ally!";
		}
	}

	public class EnemyAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Unit); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Unit unit = value as Unit;

			bool isEnemy = player.combatState != null &&
				player.combatState.instance.teams[(player.combatState.teamIndex + 1) % 2].currentUnits.Contains(unit);

			return isEnemy;
		}

		protected override string GetError()
		{
			return $"They are not an ally!";
		}
	}

	public class FoundAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Location); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			return player.locations.Contains(value as Location);
		}

		protected override string GetError()
		{
			return $"You do not know a location called { LocationTypeReader.LastInput }. \nTry typing '!tc location list' for a list of known locations.";
		}
	}

	public class HandAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Slot); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Slot slot = value as Slot;

			return slot.names.Contains("left") || slot.names.Contains("right");
		}

		protected override string GetError()
		{
			return $"You must provide a hand slot. \nTry using right/tool1 or left/tool2.";
		}
	}

	public class LearntAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Recipe); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			return player.recipes.Contains(value as Recipe);
		}

		protected override string GetError()
		{
			return $"You do not know a recipe called { RecipeTypeReader.LastInput }. \nTry typing '!tc recipe list' for a list of learnt recipes.";
		}
	}

	public class InInventoryAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Item); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Item item = value as Item;

			return player.items.Any(test => test.item == item);
		}

		protected override string GetError()
		{
			return $"You do not have an item called { ItemTypeReader.LastInput }. \nTry typing '!tc inventory' for a list of carried items.";
		}
	}

	public class InEquipment : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Item); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Item item = value as Item;

			return player.equipped.Values.Any(test => test != null && test.item == item);
		}

		protected override string GetError()
		{
			return $"You do not have an item called { ItemTypeReader.LastInput } equipped. \nTry typing '!tc equipment' for a list of equipped items.";
		}
	}

	public class InInventoryOrEquipment : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Item); } }

		protected override bool MeetsCondition(Player player, object value)
		{
			Item item = value as Item;

			return player.equipped.Values.Any(test => test != null && test.item == item) ||
					player.items.Any(test => test.item == item);
		}

		protected override string GetError()
		{
			return $"You do not have an item called { ItemTypeReader.LastInput } equipped. \nTry typing '!tc inventory' for a list of carried and equipped items.";
		}
	}

	public class ItemTypeSlot : ItemTypeAttribute
	{
		public override List<ItemType> ItemTypes
		{
			get
			{
				if (SlotTypeReader.LastValue == null)
				{
					return new List<ItemType>();
				}

				return SlotTypeReader.LastValue.allowedTypes;
			}
		}
	}

	public class ItemTypeAttribute : LimitedAttribute
	{
		protected override Type Type { get { return typeof(Item); } }

		public virtual List<ItemType> ItemTypes { get; private set; }

		public string ValidText { get; private set; }

		public ItemTypeAttribute()
		{

		}

		public ItemTypeAttribute(params ItemType[] types)
		{
			ItemTypes = new List<ItemType>(types);

			GetValidText();
		}

		protected void GetValidText()
		{
			ValidText = "a";

			char firstLetterToLower = ItemTypes[0].ToString().ToLower()[0];

			if (firstLetterToLower == 'a' ||
				firstLetterToLower == 'e' ||
				firstLetterToLower == 'i' ||
				firstLetterToLower == 'o' ||
				firstLetterToLower == 'u')
			{
				ValidText += "n";
			}

			ValidText += $" {ItemTypes[0].ToString()}";

			for (int i = 1; i < ItemTypes.Count - 1; i++)
			{
				ValidText += $", {ItemTypes[i].ToString()}";
			}

			if (ItemTypes.Count > 1)
			{
				ValidText += $", or {ItemTypes[ItemTypes.Count - 1].ToString()}";
			}
		}

		protected override bool MeetsCondition(Player player, object value) => (value == null ? false : ItemTypes.Contains((value as Item).itemType));

		protected override string GetError()
		{
			if (Value == null)
			{
				return "Item does not exist.";
			}
			else
			{
				string validText = ValidText;

				if (ValidText == null)
				{
					GetValidText();
					validText = ValidText;
					ValidText = null;
				}

				return $"{(Value as Item).name} is not {validText}.";
			}
		}
	}
}


public class CrierModuleBase : InteractiveBase
{
	public Player GetPlayer()
	{
		return ChatCraft.Instance.GetPlayer(Context.User);
	}

	public static Player GetPlayer(IUser user)
	{
		return ChatCraft.Instance.GetPlayer(user);
	}

	public async Task<IUserMessage> ReplyMentionAsync(string message)
	{
		return await ReplyAsync($"{Context.User.Mention} - {message}");
	}

	public async Task<IUserMessage> ReplyMentionBlockAsync(string message)
	{
		return await ReplyAsync($"{Context.User.Mention}\n{message}");
	}

	public async Task EmojiOption(IUserMessage message, EmojiResponse response, TimeSpan timespan, params Emoji[] emojis)
	{
		foreach (Emoji emoji in emojis)
		{
			await message.AddReactionAsync(emoji);
		}

		Task.Run(async () =>
		{
			Interactive.AddReactionCallback(message, response);

			await Task.Delay(timespan);

			Interactive.RemoveReactionCallback(message);
		});
	}

	public Task DeleteCommand()
	{
		return Context.Message.DeleteAsync();
	}

	public static string ShowCommands(string prefix, List<string> commands, List<string> descriptions)
	{
		string message = "";

		for (int i = 0; i < descriptions.Count; i++)
		{
			message += $"**{descriptions[i]}**\n";

			commands[i] = commands[i].Replace("[", "*[");
			commands[i] = commands[i].Replace("]", "]*");

			message += $"{prefix}{commands[i]}\n\n";
		}

		commands.Clear();
		descriptions.Clear();

		return message;
	}
}



public class InfoModule : CrierModuleBase
{
	[Command("race", RunMode = RunMode.Async)]
	public async Task Race([Remainder]string args)
	{
		string[] parts = args.Split(' ');

		if (parts.Length < 2)
		{
			await ReplyAsync("There must be at least two competitors! Try adding emojis to the command.");
			return;
		}

		Random random = new Random();

		string[] emojis = new string[Math.Min(5, parts.Length)];
		int[] progresses = new int[emojis.Length];
		int[] strengths = new int[emojis.Length];

		for (int i = 0; i < emojis.Length; i++)
		{
			emojis[i] = parts[i];
			strengths[i] = random.Next(5, 8);
		}

		IUserMessage message = await ReplyAsync("Ready...");

		await Task.Delay(500);

		await message.ModifyAsync(properties =>
		{
			properties.Content = "Set...";
		});

		await Task.Delay(500);

		await message.ModifyAsync(properties =>
		{
			properties.Content = "Go!";
		});

		await Task.Delay(500);

		int winner = 0;
		int winnerCount = 0;

		bool isFirst = true;

		while (winner == 0)
		{
			await message.ModifyAsync(properties =>
			{
				if (!isFirst)
				{
					for (int i = 0; i < progresses.Length; i++)
					{
						progresses[i] += random.Next(1, strengths[i]);

						if (progresses[i] >= 50)
						{
							progresses[i] = 50;
							winner |= 1 << i;

							winnerCount++;
						}
					}
				}

				isFirst = false;

				properties.Content = GetString(emojis, progresses);
			});

			await Task.Delay(1000);
		}

		await message.ModifyAsync(properties =>
		{
			if (winnerCount == 1)
			{
				properties.Content = "The winner is " + emojis[GetBitIndices(winner).First()] + "!";
			}
			else
			{
				string winners = "";

				foreach (int index in GetBitIndices(winner))
				{
					if (winners.Length != 0)
					{
						winners += " and ";
					}

					winners += emojis[index];
				}

				properties.Content = "It's a tie between " + winners;
			}
		});
	}

	string GetString(string[] emojis, int[] progresses)
	{
		string response = "";

		for (int i = 0; i < emojis.Length; i++)
		{
			response += new string('ㅤ', progresses[i]) +
				emojis[i] +
				new string('ㅤ', 50 - progresses[i]) +
				"||\n";
		}

		return response;
	}

	IEnumerable<int> GetBitIndices(int number)
	{
		for (int i = 0; i < 32; i++)
		{
			if ((number & (1 << i)) != 0)
			{
				yield return i;
			}
		}
	}



	[Command("blog")]
	public Task Info()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://www.townshiptale.com/blog/\n");

	[Command("wiki")]
	public Task Wiki()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://www.townshiptale.com/wiki/\n");

	[Command("invite")]
	public Task Invite()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://discord.gg/townshiptale\n");

	[Command("reddit")]
	public Task Reddit()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://reddit.com/r/townshiptale\n");

	[Command("resetpassword")]
	public Task ResetPassword()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://townshiptale.com/reset-password\n");

	[Command("launcher")]
	public Task Launcher()
		=> ReplyAsync(
			$"Were you looking for this?\nhttps://townshiptale.com/launcher\n");

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

	[Command("joined")]
	public async Task Joined()
	{
		Player player = GetPlayer();

		if (player.joined == default(DateTime))
		{
			player.joined = (Context.User as IGuildUser).JoinedAt.Value.UtcDateTime;
		}

		await ReplyAsync($"{Context.User.Mention} joined on {player.joined.ToString("dd/MMM/yyyy")}");
	}

	[Command("joined"), RequireAdmin]
	public async Task Joined(IUser user)
	{
		Player player = GetPlayer(user);

		if (player.joined == default(DateTime))
		{
			player.joined = (user as IGuildUser).JoinedAt.Value.UtcDateTime;
		}

		await ReplyAsync($"{user.Username} joined on {player.joined.ToString("dd/MMM/yyyy")}");
	}


	[Command("title"), Alias("heading", "header")]
	public async Task Title([Remainder]string text)
	{
		IUserMessage response = await ReplyAsync("\\```css\n" + text + "\n\\```");
		await Context.Message.DeleteAsync();

		Task _ = Task.Run(async () =>
		{
			await Task.Delay(20000);
			await response.DeleteAsync();
		});
	}

	Dictionary<int, string> minesweeperValues = new Dictionary<int, string>()
	{
		{  -1 , ":bomb:" },
		{  0 , ":white_small_square:" },
		{  1 , ":one:" },
		{  2 , ":two:" },
		{  3 , ":three:" },
		{  4 , ":four:" },
		{  5 , ":five:" },
		{  6 , ":six:" },
		{  7 , ":seven:" },
		{  8 , ":eight:" },
	};

	[Command("minesweeper")]
	public async Task Title(int size = 10, float ratio = 0.15f)
	{
		if (size > 10)
		{
			await ReplyAsync("Can only go up to size 10");
			return;
		}

		int[,] data = new int[size + 2, size + 2];

		Random random = new Random();

		for (int iy = 1; iy <= size; iy++)
		{
			for (int ix = 1; ix <= size; ix++)
			{
				if (random.NextDouble() < ratio)
				{
					data[ix, iy] = -1;

					if (data[ix - 1, iy - 1] >= 0)
					{
						data[ix - 1, iy - 1]++;
					}

					if (data[ix, iy - 1] >= 0)
					{
						data[ix, iy - 1]++;
					}

					if (data[ix + 1, iy - 1] >= 0)
					{
						data[ix + 1, iy - 1]++;
					}

					if (data[ix - 1, iy] >= 0)
					{
						data[ix - 1, iy]++;
					}

					data[ix + 1, iy]++;
					data[ix - 1, iy + 1]++;
					data[ix, iy + 1]++;
					data[ix + 1, iy + 1]++;
				}
			}
		}

		StringBuilder result = new StringBuilder();

		for (int iy = 1; iy <= size; iy++)
		{
			for (int ix = 1; ix <= size; ix++)
			{
				result.Append("||");
				result.Append(minesweeperValues[data[ix, iy]]);
				result.Append("||");
			}

			result.AppendLine();
		}

		await ReplyAsync(result.ToString());
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
			Player player = ChatCraft.Instance.GetExistingPlayer(user);

			result
				.Append(user.Id)
				.Append(',')
				.Append(user.Username.Replace(',', '_'))
				.Append(',')
				.Append(user.Nickname?.Replace(',', '_'))
				.Append(',')
				.Append(user.JoinedAt?.ToString("dd-MM-yy"))
				.Append(',')
				.Append(player?.lastMessage.ToString("dd-MM-yy"))
				.Append(',')
				.Append(player?.score)
				.Append('\n');
		}

		System.IO.File.WriteAllText("D:/Output/Join Dates.txt", result.ToString());

		await ReplyAsync("I'm done now :)");
	}

	[Command("alerton")]
	public async Task AlertOn()
	{
		if (Context.Guild == null)
		{
			return;
		}

		if (!(Context.User as IGuildUser).RoleIds.Contains<ulong>(334935631149137920))
		{
			return;
		}

		IRole role = Context.Guild.Roles.FirstOrDefault(test => test.Name == "followers");

		await role.ModifyAsync(properties => properties.Mentionable = true);
	}

	[Command("alertoff")]
	public async Task AlertOff()
	{
		if (Context.Guild == null)
		{
			return;
		}

		if (!(Context.User as IGuildUser).RoleIds.Contains<ulong>(334935631149137920))
		{
			return;
		}

		IRole role = Context.Guild.Roles.FirstOrDefault(test => test.Name == "followers");

		await role.ModifyAsync(properties => properties.Mentionable = false);
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

	[Command("supporter"), Alias("support", "donate")]
	public async Task Supporter()
	{
		await ReplyAsync("To become a supporter, visit the following URL, or click the 'Become a Supporter' button in the Alta Launcher.\nhttps://townshiptale.com/supporter");
	}

	[Command("help"), Alias("getstarted", "gettingstarted")]
	public async Task GetStarted()
	{
		List<string> commands = new List<string>();
		List<string> descriptions = new List<string>();

		string message = $"Welcome! I am the Town Crier.\n" +
			$"I can help with various tasks.\n\n" +
			$"Here are some useful commands:\n\n";

		commands.Add("help");
		descriptions.Add("In case you get stuck");

		commands.Add("follow");
		descriptions.Add("Get alerted with news.");

		commands.Add("blog");
		descriptions.Add("For a good read");

		commands.Add("whois [developer]");
		descriptions.Add("A brief bio on who a certain developer is");

		commands.Add("flip");
		descriptions.Add("Flip a coin!");

		commands.Add("roll");
		descriptions.Add("Roll a dice!");


		//commands.Add("tc help");
		//descriptions.Add("An introduction to A Chatty Township Tale");

		message += ShowCommands("!", commands, descriptions);

		await ReplyAsync(message);
		//RestUserMessage messageResult = (RestUserMessage)
		//await messageResult.AddReactionAsync(Emote.Parse("<:hand_splayed:360022582428303362>"));
	}

}

[Group("servers"), Alias("s", "server")]
public class Servers : CrierModuleBase
{
	public enum Map
	{
		Town,
		Tutorial,
		TestZone
	}

	[Command(), Alias("online")]
	public async Task Online()
	{
		IEnumerable<GameServerInfo> servers = await ApiAccess.ApiClient.ServerClient.GetOnlineServersAsync();

		StringBuilder response = new StringBuilder();

		response.AppendLine("The following servers are online:");

		foreach (GameServerInfo server in servers)
		{
			response.AppendFormat("{0} - {3} - {1} player{2} online\n",
				server.Name,
				server.OnlinePlayers.Count,
				server.OnlinePlayers.Count == 1 ? "" : "s",
				(Map)server.SceneIndex);
		}

		await ReplyMentionAsync(response.ToString());
	}

	[Command("players"), Alias("player", "p")]
	public async Task Players([Remainder]string serverName)
	{
		serverName = serverName.ToLower();

		IEnumerable<GameServerInfo> servers = await ApiAccess.ApiClient.ServerClient.GetOnlineServersAsync();

		StringBuilder response = new StringBuilder();

		response.AppendLine("Did you mean one of these?");

		foreach (GameServerInfo server in servers)
		{
			response.AppendLine(server.Name);

			if (Regex.Match(server.Name, @"\b" + serverName + @"\b", RegexOptions.IgnoreCase).Success)
			{
				response.Clear();

				if (server.OnlinePlayers.Count > 1)
				{
					response.AppendFormat("These players are online on {0}\n", server.Name);

					foreach (UserInfo user in server.OnlinePlayers)
					{
						MembershipStatusResponse membershipResponse = await ApiAccess.ApiClient.UserClient.GetMembershipStatus(user.Identifier);

						response.AppendFormat("- {1}{0}\n", user.Username, membershipResponse.IsMember ? "<:Supporter:547252984481054733> " : "");
					}
				}
				else if (server.OnlinePlayers.Count == 1)
				{
					response.AppendFormat("Only {0} is on {1}", server.OnlinePlayers.First().Username, server.Name);
				}
				else
				{
					response.AppendFormat("Nobody is on {0}", server.Name);
				}

				break;
			}
		}

		await ReplyMentionAsync(response.ToString());
	}
}


[Group("whois")]
public class WhoIs : CrierModuleBase
{
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

	[Command("sol")]
	public async Task Sol() => await ReplyAsync("Sol is the wielder of the ban hammer. He is here to help, be awesome, and kick you if you spam :)");

	[Command("lefnire")]
	public async Task Lefnire() => await ReplyAsync("Lefnire is a web and server guru. He's helping keep an eye on the servers, and find problems with the game!");

	[Command("ozball")]
	public async Task Ozball() => await ReplyAsync("Ozball is the wielder of Ban Hammer Jr. Ozball's his name, dealing with bad people's his game (though he prefers ATT).");

	[Command("town crier"), Alias("you")]
	public async Task TownCrier() => await ReplyAsync("I'm the trustworthy Town Crier! Some may find me annoying, but I swear, I'm here to help!");

	[Command("alta"), Alias("company", "team", "developer")]
	public async Task Alta() => await ReplyAsync("Alta is a VR game development studio based in Sydney! We are (surprise, surprise) working on A Township Tale.\n Our extended team consists of:\nTima, Boramy, Joel, Timo, Victor, Serena, Sol, and myself (Town Crier).");

	[Command("larry")]
	public async Task Larry() => await ReplyAsync("A very persistent person!");

	[Command("matssu")]
	public async Task Matssu() => await ReplyAsync("An adventurer who explored the depths of the source code... The first contributor!");

	[Command("thelegend27")]
	public async Task TheLegend27() => await ReplyAsync("Some say TheLegend27 is the first Game of War player ever. Born from *fire*...");
	
	[Command("taz"), Alias("taz prime", "mantax64", "mantax")]
	public async Task taz() => await ReplyAsync("Taz Prime? I would advise you to avoid eye contact with them...");
	
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

	[Command(), Priority(-1)]
	public async Task Other([Remainder]string name)
	{
		Random random = new Random();

		await ReplyAsync(string.Format(UnknownReplies[random.Next(UnknownReplies.Length)], name, Context.User.Mention));
	}
}
