using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier.Modules.Admin
{
	[Group("send"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
	public class SendModule : InteractiveBase<SocketCommandContext>
	{
		[Command()]
		public async Task Send(ITextChannel channel, [Remainder]string message)
		{
			await channel.SendMessageAsync(message);
		}
	}

	//[Group("test"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
	//public class TestModule : InteractiveBase<SocketCommandContext>
	//{
	//	public ActivityRoleService RoleService { get; }

	//	public TestModule(ActivityRoleService roleService)
	//	{
	//		RoleService = roleService;
	//	}

	//	[Command("role")]
	//	public async Task Role(SocketGuildUser user)
	//	{
	//		RoleService.ForceUpdate(user);
	//	}
	//}

	[Group("guild"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
	public class GuildModule : InteractiveBase<SocketCommandContext>
	{
		public delegate bool TryGetValue<T>(SocketMessage message, out T value);
		public delegate bool TryGetValueText<T>(string message, out T value);

		const char SkipCharacter = 'x';

		public TownDatabase Database { get; set; }

		[Command("initialize"), Alias("init")]
		public async Task Initialize()
		{
			TownGuild guild = Database.GetGuild(Context.Guild);

			bool isNew = guild == null;

			if (isNew)
			{
				guild = new TownGuild()
				{
					GuildId = Context.Guild.Id
				};

			}

			await ReplyAsync("Initializing Guild...\n" +
				$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");

			await AskString($"What is your desired prefix?", guild.Prefix, value => guild.Prefix = value);

			await AskChannel($"Where should newcomers be announced?", guild.NotificationChannel, value => guild.NotificationChannel = value);

			await AskString("How should newcomers be announced? Use {user} to mention them, and {server} to show the servers name.", guild.WelcomeMessage, value => guild.WelcomeMessage = value);

			await AskChannel($"Where should leavers be announced?", guild.LeaverChannel, value => guild.LeaverChannel = value);

			await ReplyAsync("All configured!");

			if (isNew)
			{
				Database.Guilds.Insert(guild);
			}
			else
			{
				Database.Guilds.Update(guild);
			}
		}

		[Command("wiki")]
		public async Task Wiki()
		{
			TownGuild guild = Database.GetGuild(Context.Guild);

			if (guild == null)
			{
				return;
			}

			await ReplyAsync("Editing wiki...\n" +
				$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");

			await AskString($"What is the url of the wiki?", guild.WikiUrl, value => guild.WikiUrl = value);
			await AskString($"What is the name of the wiki?", guild.WikiName, value => guild.WikiName = value);
			await AskString($"What is the icon of the wiki?", guild.WikiIcon, value => guild.WikiIcon = value);

			Database.Guilds.Update(guild);

			await ReplyAsync("All done!");
		}



		[Group("activity-role"), Alias("activityrole", "activity")]
		public class ActivityRoleConfig : GuildModule
		{
			[Command("edit"), Alias("init", "create")]
			public async Task Add(IRole role)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}

				ActivityRole activity = guild.ActivityRoles.FirstOrDefault(x => x.AssociatedRole == role.Id);

				if (activity == null)
				{
					activity = new ActivityRole() { AssociatedRole = role.Id };

					guild.ActivityRoles.Add(activity);
				}

				await ReplyAsync("Editing activity...\n" +
					$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");

				await AskString($"What is the name of the activity?", activity.ActivityName, value => activity.ActivityName = value);

				await AskEnum("What type of activity? (Playing, Streaming, Listening, Watching)", activity.ActivityType, value =>
				{
					if (value == ActivityFlag.Streaming)
					{
						value |= ActivityFlag.Playing;
					}

					activity.ActivityType = value;
				});

				Database.Guilds.Update(guild);

				await ReplyAsync("Done!");
			}

			[Command("delete"), Alias("remove")]
			public async Task Delete(IRole role)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}

				ActivityRole activity = guild.ActivityRoles.FirstOrDefault(x => x.AssociatedRole == role.Id);

				if (activity == null)
				{
					await ReplyAsync("Alert not found for " + role.Mention);
				}
				else
				{
					await ReplyAsync("Deleted activity for " + role.Mention);

					guild.ActivityRoles.Remove(activity);

					Database.Guilds.Update(guild);
				}
			}
		}

		[Group("cross-alert"), Alias("crossalert", "alert")]
		public class CrossAlertConfig : GuildModule
		{
			[Command("edit"), Alias("init", "create")]
			public async Task Add(IRole role)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}

				CrossAlert alert = guild.CrossAlerts.FirstOrDefault(x => x.Role == role.Id);

				if (alert == null)
				{
					alert = new CrossAlert() { Role = role.Id };

					guild.CrossAlerts.Add(alert);
				}

				await ReplyAsync("Editing cross alert...\n" +
					$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");
				
				await AskChannel($"Which channel should be alerted?", alert.Channel, value => alert.Channel = value);

				Database.Guilds.Update(guild);

				await ReplyAsync("Done!");
			}

			[Command("delete"), Alias("remove")]
			public async Task Delete(IRole role)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}

				CrossAlert alert = guild.CrossAlerts.FirstOrDefault(x => x.Role == role.Id);

				if (alert == null)
				{
					await ReplyAsync("Alert not found for " + role.Mention);
				}
				else
				{
					await ReplyAsync("Deleted filter for " + role.Mention);

					guild.CrossAlerts.Remove(alert);

					Database.Guilds.Update(guild);
				}
			}
		}

		[Group("filter")]
		public class FilterConfig : GuildModule
		{
			[Command("edit"), Alias("init", "create")]
			public async Task Add(ITextChannel channel)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}
				
				ChannelFilter filter = guild.ChannelFilters.FirstOrDefault(x => x.Channel == channel.Id);
				
				if (filter == null)
				{
					filter = new ChannelFilter() { Channel = channel.Id };

					guild.ChannelFilters.Add(filter);
				}

				await ReplyAsync("Editing filter...\n" +
					$"Please answer the following questions ('{SkipCharacter}' to skip / leave default)");

				await AskEnum($"What type of filter? (Heading or Image)", filter.Type, value => filter.Type = value);

				await AskChannel($"Which channel should rejected content be copied to?", filter.AlertChannel, value => filter.AlertChannel = value);

				Database.Guilds.Update(guild);

				await ReplyAsync("Done!");
			}

			[Command("delete"), Alias("remove")]
			public async Task Delete(ITextChannel channel)
			{
				TownGuild guild = Database.GetGuild(Context.Guild);

				if (guild == null)
				{
					return;
				}

				ChannelFilter filter = guild.ChannelFilters.FirstOrDefault(x => x.Channel == channel.Id);

				if (filter == null)
				{
					await ReplyAsync("Filter not found for " + channel.Mention);
				}
				else
				{
					await ReplyAsync("Deleted filter for " + channel.Mention);

					guild.ChannelFilters.Remove(filter);

					Database.Guilds.Update(guild);
				}
			}
		}

		public Task<bool> AskEnum<T>(string question, T defaultValue, Action<T> apply)
			where T : struct
		{
			return Ask<T>(question, defaultValue, apply, Enum.TryParse<T>);
		}

		public Task<bool> AskInt(string question, int defaultValue, Action<int> apply)
		{
			return Ask<int>(question, defaultValue, apply, int.TryParse);
		}

		public Task<bool> AskBool(string question, bool defaultValue, Action<bool> apply)
		{
			return Ask<bool>(question, defaultValue, apply, bool.TryParse);
		}

		public Task<bool> AskString(string question, string defaultValue, Action<string> apply)
		{
			return Ask<string>(question, defaultValue, apply, (SocketMessage message, out string value) =>
			{
				value = message.Content;
				return true;
			});
		}

		public Task<bool> AskChannel(string question, ulong defaultValue, Action<ulong> apply)
		{
			return Ask<ulong>(question, defaultValue, apply, (SocketMessage message, out ulong value) =>
			{
				if (message.MentionedChannels.Count == 1)
				{
					value = message.MentionedChannels.First().Id;
					return true;
				}

				if (ulong.TryParse(message.Content, out value))
				{
					return true;
				}

				message.Channel.SendMessageAsync(message.Content + " is not valid");

				value = 0;
				return false;
			});
		}

		public Task<bool> Ask<T>(string question, T defaultValue, Action<T> apply, TryGetValueText<T> getValue)
		{
			return Ask(question, defaultValue, apply, (SocketMessage message, out T value) => getValue(message.Content, out value));
		}

		public async Task<bool> Ask<T>(string question, T defaultValue, Action<T> apply, TryGetValue<T> getValue)
		{
			if (defaultValue != null)
			{
				question += $" (Default {defaultValue})";
			}

			await ReplyAsync(question);

			SocketMessage response;
			T result;

			do
			{
				response = await Interactive.NextMessageAsync(Context, true, true);

				if (defaultValue != null && response.Content.Length == 1 && response.Content[0] == SkipCharacter)
				{
					return false;
				}

			}
			while (!getValue(response, out result));

			apply(result);

			return true;
		}
	}
}
