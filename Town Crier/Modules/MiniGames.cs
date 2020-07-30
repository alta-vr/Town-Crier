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
	//public class LocalInteractiveBase : InteractiveBase<SocketCommandContext>
	//{
	//	public async Task<IUserMessage> ReplyMentionAsync(string message)
	//	{
	//		return await ReplyAsync($"{Context.User.Mention} - {message}");
	//	}

	//	public async Task<IUserMessage> ReplyMentionBlockAsync(string message)
	//	{
	//		return await ReplyAsync($"{Context.User.Mention}\n{message}");
	//	}

	//	public async Task EmojiOption(IUserMessage message, EmojiResponse response, TimeSpan timespan, params Emoji[] emojis)
	//	{
	//		foreach (Emoji emoji in emojis)
	//		{
	//			await message.AddReactionAsync(emoji);
	//		}

	//		await Task.Run(async () =>
	//		{
	//			Interactive.AddReactionCallback(message, response);
	//			await Task.Delay(timespan);
	//			Interactive.RemoveReactionCallback(message);
	//		});
	//	}

	//	public Task DeleteCommand()
	//	{
	//		return Context.Message.DeleteAsync();
	//	}

	//	public static string ShowCommands(string prefix, List<string> commands, List<string> descriptions)
	//	{
	//		string message = "";

	//		for (int i = 0; i < descriptions.Count; i++)
	//		{
	//			message += $"**{descriptions[i]}**\n";

	//			commands[i] = commands[i].Replace("[", "*[");
	//			commands[i] = commands[i].Replace("]", "]*");

	//			message += $"{prefix}{commands[i]}\n\n";
	//		}

	//		commands.Clear();
	//		descriptions.Clear();

	//		return message;
	//	}
	//}

	public class MiniGames : InteractiveBase<SocketCommandContext>
	{
		string GetString(string[] emojis, int[] progresses)
		{
			string response = "";

			for (int i = 0; i < emojis.Length; i++)
			{
				response += new string('ㅤ', progresses[i]) +
					emojis[i] +
					new string('ㅤ', 50 - progresses[i]) +
					"\\|\\|\n";
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
	}
}
