using Discord;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	public enum ActivityFlag
	{
		Playing = 1 << ActivityType.Playing,
		Streaming = 1 << ActivityType.Streaming,
		Listening = 1 << ActivityType.Listening,
		Watching = 1 << ActivityType.Watching,
	}

	public class ActivityRoleService
	{
		public bool IsEnabled { get; set; }

		readonly DiscordSocketClient client;
		readonly IServiceProvider provider;
		readonly TownDatabase database;

		public ActivityRoleService(IServiceProvider provider, DiscordSocketClient client, TownDatabase database)
		{
			this.provider = provider;
			this.client = client;
			this.database = database;

			this.client.GuildMemberUpdated += UserUpdated;

			this.client.GuildAvailable += Ready;
		}

		async Task Ready(SocketGuild discordGuild)
		{
			if (discordGuild.Id == 334933825383563266) //ATT Guild
			{
				const ulong botlogchannel = 533105660993208332;

				SocketTextChannel logChannel = discordGuild.GetTextChannel(botlogchannel);

				await logChannel.SendMessageAsync("Town Crier has Restarted");
				await discordGuild.GetTextChannel(377248661341470720).SendMessageAsync("Town Crier is back online!");
			}

			Console.WriteLine(discordGuild.Name + " is ready");

			TownGuild guild = database.GetGuild(discordGuild);

			if (guild == null || guild.ActivityRoles.Count == 0)
			{
				return;
			}

			_ = Task.Run(async () =>
		   {
			   try
			   {
				   await discordGuild.DownloadUsersAsync();

				   foreach (SocketGuildUser user in discordGuild.Users)
				   {
					   Console.WriteLine("Update " + user.Username);
					   await UpdateRoles(null, user, guild);
				   }
			   }
			   catch (Exception e)
			   {
				   Console.WriteLine(e);
				   Console.WriteLine(e.StackTrace);
			   }

			   Console.WriteLine("DONE!");
		   });
		}

		async Task UserUpdated(SocketGuildUser oldUser, SocketGuildUser newUser)
		{
			_ = Task.Run(async () =>
			{
				try
				{
					// If both activities are the same (in type & message), return
					if (oldUser.Activity == newUser.Activity) return;

					// Fetch the guild from the Database
					var guild = database.GetGuild(newUser.Guild);

					if (guild != null)
					{
						await UpdateRoles(oldUser, newUser, guild);
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Console.WriteLine("Failed updating user");
				}
			});
		}

		public async void ForceUpdate(SocketGuildUser user)
		{
			await UpdateRoles(null, user, database.GetGuild(user.Guild));
		}

		async Task UpdateRoles(SocketGuildUser oldUser, SocketGuildUser newUser, TownGuild guild)
		{
			if (oldUser == null || oldUser.Activity != null)
			{
				int activityFlag = oldUser == null ? 0 : 1 << (int)oldUser.Activity.Type;
				int newActivityFlag = newUser.Activity == null ? 0 : (1 << (int)newUser.Activity.Type);

				IEnumerable<ActivityRole> oldPotentialRoles = oldUser == null ? guild.ActivityRoles : guild.ActivityRoles.Where(x => ((int)x.ActivityType & activityFlag) != 0);

				// Removing old roles
				foreach (var activity in oldPotentialRoles)
				{
					IRole role = newUser.Roles.FirstOrDefault(x => x.Id == activity.AssociatedRole);

					if (role != null)
					{
						if (newUser.Activity == null || ((int)activity.ActivityType & newActivityFlag) == 0 || !await IsMatch((activity.ActivityType & ActivityFlag.Streaming) != 0, activity.ActivityName, newUser))
						{
							await newUser.RemoveRoleAsync(role);
						}
					}
				}
			}

			if (newUser.Activity != null)
			{
				int activityFlag = 1 << (int)newUser.Activity.Type;

				// Adding new roles
				foreach (var activity in guild.ActivityRoles.Where(x => ((int)x.ActivityType & activityFlag) != 0))
				{
					IRole role = newUser.Guild.GetRole(activity.AssociatedRole);

					if (!newUser.Roles.Contains(role))
					{
						if (await IsMatch((activity.ActivityType & ActivityFlag.Streaming) != 0, activity.ActivityName, newUser))
						{
							await newUser.AddRoleAsync(role);
						}
					}
				}
			}
		}

		async Task<bool> IsMatch(bool isStreaming, string name, SocketGuildUser newUser)
		{
			try
			{
				if (!isStreaming)
				{
					return Regex.IsMatch(newUser.Activity.Name, name, RegexOptions.IgnoreCase);
				}
				else if (Regex.IsMatch(newUser.Activity.Name, name, RegexOptions.IgnoreCase))
				{
					using (HttpClient client = new HttpClient())
					{
						string url;

						if (newUser.Activity is StreamingGame streaming)
						{
							url = streaming.Url;
							url = url.Substring(url.LastIndexOf('/') + 1);
						}
						else
						{
							url = newUser.Username;
						}

						HttpRequestMessage request = new HttpRequestMessage
						{
							Method = HttpMethod.Get,
							RequestUri = new Uri("https://api.twitch.tv/helix/streams?user_login=" + url),
							Headers =
						{
							{ "Client-ID", Environment.GetEnvironmentVariable("TWITCH_CLIENT_ID") }
						}
						};

						HttpResponseMessage response = await client.SendAsync(request);

						if (response.IsSuccessStatusCode)
						{
							string result = await response.Content.ReadAsStringAsync();

							StreamResponse streamInfo = JsonConvert.DeserializeObject<StreamResponse>(result);

							return streamInfo.data.Length > 0 && Regex.IsMatch(streamInfo.data[0].title, name, RegexOptions.IgnoreCase);
						}
					}
				}
			}
			catch
			{

			}

			return false;
		}

		class StreamResponse
		{
			public class StreamItem
			{
				public ulong id;
				public ulong game_id;
				public string title;
				public int viewer_count;

				//https://dev.twitch.tv/docs/v5/reference/streams#get-stream-by-user
			}

			public StreamItem[] data;
		}
	}
}
