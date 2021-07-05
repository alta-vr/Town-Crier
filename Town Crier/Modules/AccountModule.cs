using Alta.WebApi.Models;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier
{
	[Group("account")]
	public class AccountModule : InteractiveBase<SocketCommandContext>
	{
		public const string LinkUrl = "https://discord.com/api/oauth2/authorize?client_id=403426544737058816&redirect_uri=https%3A%2F%2Faccounts.townshiptale.com%2Foauth2%2Fui%2Flink%2Fdiscord&response_type=code&scope=guilds%20guilds.join%20identify%20email%20connections";
		public AltaAPI AltaApi { get; set; }
		public TownDatabase Database { get; set; }

		//public AccountService AccountService { get; set; }

		//public class AccountDatabase
		//{
		//	public Dictionary<ulong, AccountInfo> accounts = new Dictionary<ulong, AccountInfo>();

		//	public Dictionary<int, ulong> altaIdMap = new Dictionary<int, ulong>();

		//	public SortedSet<AccountInfo> expiryAccounts = new SortedSet<AccountInfo>(new AccountInfo.Comparer());
		//}

		//public class AccountInfo
		//{
		//	public class Comparer : IComparer<AccountInfo>
		//	{
		//		public int Compare(AccountInfo x, AccountInfo y)
		//		{
		//			return x.supporterExpiry.CompareTo(y.supporterExpiry);
		//		}
		//	}

		//	public ulong discordIdentifier;
		//	public int altaIdentifier;
		//	public DateTime supporterExpiry;
		//	public bool isSupporter;
		//	public string username;
		//}

		[RequireUserPermission(GuildPermission.BanMembers)]
		[Command("investigate")]
		public async Task Investigate(SocketGuildUser user)
		{
			await ReplyAsync(user.Mention + " " + user.JoinedAt.Value.ToLocalTime() + " " + user.CreatedAt.ToLocalTime());
		}

		[RequireUserPermission(GuildPermission.BanMembers)]
		[Command("removethespamplz"), Alias("bots")]
		public async Task RemoveTheSpamPlz(int count = 0)
		{
			await Context.Guild.DownloadUsersAsync();

			foreach (SocketGuildUser user in Context.Guild.Users)
			{
				if (user.JoinedAt.HasValue && DateTime.UtcNow - user.JoinedAt.Value.UtcDateTime < TimeSpan.FromMinutes(80))
				{
					if (DateTime.UtcNow - user.CreatedAt < TimeSpan.FromDays(1))
					{
						count++;

						//Check if guild is A Township Tale
						ulong attguild = 334933825383563266;
						if (Context.Guild.Id == attguild)
						{
							//Send message to bot-log channel
							ulong botlogchannel = 533105660993208332;
							await Context.Guild.GetTextChannel(botlogchannel).SendMessageAsync("Kicking " + user.Mention + $" **(#{count})** - " + user.JoinedAt.Value.ToLocalTime() + " " + user.CreatedAt.ToLocalTime());

						}
						else
						{
							//Send message to where command was executed
							await ReplyAsync("Kicking " + user.Mention + $" **(#{count})** - " + user.JoinedAt.Value.ToLocalTime() + " " + user.CreatedAt.ToLocalTime());
						}

						await user.SendMessageAsync("You have been kicked from " + Context.Guild.Name + " on suspicion of being a bot, if you aren't a bot feel free to rejoin. Sorry for the inconvenience!\nhttps://discord.gg/townshiptale");
						await user.KickAsync("Probably a bot");
					}
				}
			}
		}

		[RequireUserPermission(GuildPermission.BanMembers)]
		[Command("kick-bots")]
		public async Task KickBot(bool isRealRun = false, float days = 1, int kickLimit = -1)
		{
			int count = 0;

			await Context.Guild.DownloadUsersAsync();

			DateTime startCheck = DateTime.Now.AddDays(-days);

			var toKick = new List<SocketGuildUser>();

			foreach (SocketGuildUser user in Context.Guild.Users)
			{
				if (user.JoinedAt.HasValue &&
					user.JoinedAt > startCheck &&
					user.JoinedAt - user.CreatedAt < TimeSpan.FromMinutes(60))
				{
					if (kickLimit > 0 && count > kickLimit)
					{
						break;
					}

					if (string.IsNullOrEmpty(user.AvatarId) && user.Activity == null && user.Status == UserStatus.Offline)
					{
						toKick.Add(user);
					}

					count++;
				}
			}

			const ulong botlogchannel = 533105660993208332;
			SocketTextChannel logChannel = null;

			if (Context.Guild.Id == 334933825383563266) //ATT Guild
			{
				logChannel = Context.Guild.GetTextChannel(botlogchannel);
			}

			for (int i = 0; i < toKick.Count; i++)
			{
				SocketGuildUser user = toKick[i];

				await logChannel?.SendMessageAsync($"{(isRealRun ? "" : "DRY RUN - ")}Kicked {user.Mention} **(#{i + 1})**");

				if (isRealRun)
				{
					await user.SendMessageAsync("You have been kicked from " + Context.Guild.Name + " on suspicion of being a bot, if you aren't a bot feel free to rejoin. Sorry for the inconvenience!\nhttps://discord.gg/townshiptale");
					await user.KickAsync("Probably a bot");
				}
			}

			string finishedMessage = $"{(isRealRun ? "" : "DRY RUN - ")}{Context.User.Mention} I've finished kicking bots that have joined since {startCheck.ToShortDateString()}, Total: {toKick.Count}";

			await base.ReplyAsync(finishedMessage);

			await logChannel?.SendMessageAsync(finishedMessage);
		}

		//[RequireUserPermission(GuildPermission.Administrator)]
		//[Command("test")]
		//public async Task Test()
		//{
		//	await AccountService.SendGeneralMessage(Context.User as SocketGuildUser);
		//}

		[Command]
		public async Task AccountInfo()
		{
			TownUser user = Database.GetUser(Context.User);

			await Database.RefreshAltaUser(user);

			if (user.AltaId <= 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit\n" + LinkUrl);
			}
			else
			{
				var altaUser = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(user.AltaId);

				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo(user.AltaId);

				var stats = await AltaApi.ApiClient.UserClient.GetUserStatisticsAsync(user.AltaId);



				await ReplyAsync($"In Game Username: {altaUser.Username}\nSupporter: {account.MemberStatus.IsSupporter}\nPlay Time: {stats.PlayTime.TotalHours:0.0} hours\nCreated Account: {stats.SignupTime.ToShortDateString()} ({(DateTime.UtcNow - stats.SignupTime).TotalDays:0} days ago) ");
			}
		}

		[Command("full")]
		public async Task AccountInfoFull()
		{
			TownUser user = Database.GetUser(Context.User);

			await Database.RefreshAltaUser(user);

			if (user.AltaId <= 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit\n" + LinkUrl);
			}
			else
			{
				var altaUser = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(user.AltaId);

				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo(user.AltaId);

				var stats = await AltaApi.ApiClient.UserClient.GetUserStatisticsAsync(user.AltaId);

				await ReplyAsync($"In Game Username: {altaUser.Username}\nTalems: {account.ShardBalance}\nSupporter: {account.MemberStatus.IsSupporter}\nSupporter End: {account.MemberStatus.MemberEndDate}\nPlay Time: {stats.PlayTime.TotalHours:0.0} hours\nCreated Account: {stats.SignupTime.ToShortDateString()} ({(DateTime.UtcNow - stats.SignupTime).TotalDays:0} days ago) ");
			}
		}

		[Command("talems")]
		public async Task GetTalems()
		{
			TownUser user = Database.GetUser(Context.User);

			await Database.RefreshAltaUser(user);

			if (user.AltaId <= 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit\n" + LinkUrl);
			}
			else
			{
				var account = await AltaApi.ApiClient.ShopClient.Account.GetShopAccountInfo(user.AltaId);

				await ReplyAsync($"You have {account.ShardBalance} Talems");
			}
		}

		//[Command("fix-names")]
		//[RequireUserPermission(GuildPermission.ManageGuild)]
		//public async Task FixAllUsernames()
		//{
		//	foreach (var user in Database.Users.FindAll())
		//	{
		//		if (user.AltaInfo != null && user.AltaInfo.Identifier > 0 && string.IsNullOrEmpty(user.AltaInfo.Username))
		//		{
		//			await Task.Delay(50);

		//			try
		//			{
		//				var userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(user.AltaInfo.Identifier);

		//				user.AltaInfo.Username = userInfo.Username;

		//				Database.Users.Update(user);

		//				Console.WriteLine("Fixed missing username, DiscordId: {0} AltaId: {1} - {2}", user.UserId, user.AltaInfo.Identifier, user.AltaInfo.Username);
		//			}
		//			catch (Exception e)
		//			{
		//				await ReplyAsync(user.AltaInfo.Identifier + " " + user.UserId);
		//			}
		//		}
		//	}

		//	Console.WriteLine("Done");

		//	await ReplyAsync("Done");
		//}

		//[Command("link-all")]
		//[RequireUserPermission(GuildPermission.ManageGuild)]
		//public async Task LinkAllExisting()
		//{
		//	foreach (var user in Database.Users.FindAll())
		//	{
		//		if (user.AltaInfo != null && user.AltaInfo.Identifier > 0)
		//		{
		//			await Task.Delay(50);

		//			try
		//			{
		//				await AltaApi.ApiClient.Account.LinkDiscordAccountAdmin(user.AltaInfo.Identifier, user.UserId);

		//				Console.WriteLine("Linked account: {0} with {1} - {2}", user.UserId, user.AltaInfo.Identifier, user.AltaInfo.Username);
		//			}
		//			catch (Exception e)
		//			{
		//				await ReplyAsync(user.AltaInfo.Identifier + " " + user.UserId);
		//			}
		//		}
		//	}

		//	Console.WriteLine("Done");
		//	await ReplyAsync("Done");
		//}

		class VerifyData
		{
			public string discord;
		}


		[Command("who-reverse")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task WhoReverse(IUser user)
		{
			TownUser entry = Database.GetUser(user);

			await Database.RefreshAltaUser(entry);

			if (entry.AltaId > 0)
			{
				var altaUser = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(entry.AltaId);

				await ReplyAsync(entry.Name + " is " + altaUser.Username);
			}
			else
			{
				await ReplyAsync(entry.Name + " hasnt linked their alta account");
			}
		}

		[Command("who")]
		[RequireUserPermission(GuildPermission.ManageGuild)]
		public async Task Who(string username)
		{
			var userId = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(username);

			TownUser entry = Database.Users.FindByIndex(userId.Identifier, "alta_id-index", "AltaId");

			if (entry != null)
			{
				await ReplyAsync(username + " is " + entry.Name);
			}
			else
			{
				await ReplyAsync("Couldn't find " + username);
			}
		}

		[Command("link")]
		public async Task Link()
		{
			await ReplyAsync(LinkUrl);
		}

		//async Task Link(IUser discordUser, TownUser user, int altaId, string linkToken)
		//{
		//	if (user.AltaInfo == null)
		//	{
		//		user.AltaInfo = new UserAltaInfo();
		//	}

		//	if (user.AltaInfo.Identifier == altaId)
		//	{
		//		await ReplyAsync(discordUser.Mention + ", " + "Already connected!");
		//		await Context.Message.DeleteAsync();

		//		await AccountService.UpdateAsync(user, (SocketGuildUser)discordUser);
		//		return;
		//	}

		//	if (user.AltaInfo.Identifier != 0)
		//	{
		//		await ReplyAsync(discordUser.Mention + ", " + $"Unlinking your Discord from {user.AltaInfo.Username}...");
		//		await Context.Message.DeleteAsync();

		//		user.Unlink();

		//		Database.Users.Update(user);
		//	}

		//	TownUser existing = Database.Users.FindByIndex(altaId, "alta_id-index", "AltaId");

		//	if (existing != null && existing.UserId != discordUser.Id)
		//	{
		//		var olddiscorduser = Context.Client.GetUser(existing.UserId);

		//		await ReplyAsync(discordUser.Mention + ", " + $"Unlinking your Alta account from {olddiscorduser?.Mention}...");
		//		await Context.Message.DeleteAsync();

		//		existing.Unlink();

		//		Database.Users.Update(existing);
		//	}

		//	var userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(altaId);

		//	user.AltaId = altaId;
		//	user.AltaInfo.Identifier = altaId;
		//	user.AltaInfo.Username = userInfo.Username;

		//	Database.Users.Update(user);

		//	try
		//	{
		//		if (linkToken != null)
		//		{
		//			await AltaApi.ApiClient.Account.LinkDiscordAccount(linkToken, discordUser.Id);
		//		}
		//	}
		//	catch (Exception e)
		//	{
		//		Console.WriteLine("Failed linking to discord in the ATT Database");
		//	}

		//	await ReplyAsync(Context.User.Mention + ", " + $"Successfully linked to your Alta account! Hey there {user.AltaInfo.Username}!");
		//	await Context.Message.DeleteAsync();

		//	//await AccountService.UpdateAsync(user, (SocketGuildUser)discordUser);
		//}

		//[Command("listextra")]
		//[RequireUserPermission(GuildPermission.ManageGuild)]
		//public async Task ListExtra()
		//{
		//	foreach (SocketGuildUser user in Context.Guild.GetRole(547202953505800233).Members)
		//	{
		//		TownUser entry = Database.GetUser(user);

		//		if (entry.AltaInfo == null || !entry.AltaInfo.IsSupporter)
		//		{
		//			await ReplyAsync(user.Username + " " + (entry.AltaInfo == null));
		//		}
		//	}

		//	await ReplyAsync("Done!");
		//}

		//[Command("forceall")]
		//[RequireUserPermission(GuildPermission.ManageGuild)]
		//public async Task ForceAll()
		//{
		//	await Context.Guild.DownloadUsersAsync();

		//	await ReplyAsync("Starting...");
		//	await AccountService.UpdateAll(true);
		//	await ReplyAsync("Done!");
		//}

		//[Command("update")]
		//public async Task Update()
		//{
		//	TownUser entry = Database.GetUser(Context.User);

		//	if (entry.AltaInfo != null)
		//	{
		//		await AccountService.UpdateAsync(entry, (SocketGuildUser)Context.User);

		//		await ReplyAsync(Context.User.Mention + ", " + $"Hey {entry.AltaInfo.Username}, your account info has been updated!");
		//	}
		//	else
		//	{
		//		await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
		//	}
		//}

		//[Command("forceupdate"), RequireUserPermission(Discord.GuildPermission.ManageGuild)]
		//public async Task Update(SocketUser user)
		//{
		//	TownUser entry = Database.GetUser(user);

		//	if (entry.AltaInfo != null)
		//	{
		//		await AccountService.UpdateAsync(entry, (SocketGuildUser)user);

		//		await ReplyAsync(Context.User.Mention + ", " + $"{entry.AltaInfo.Username}'s account info has been updated!");
		//	}
		//	else
		//	{
		//		await ReplyAsync(Context.User.Mention + ", " + user.Username + " have not linked to an Alta account!");
		//	}
		//}

		//[Command("unlink")]
		//public async Task Unlink()
		//{
		//	var user = Database.GetUser(Context.User);

		//	if (user.AltaInfo != null && user.AltaInfo.Identifier != 0)
		//	{
		//		user.Unlink();

		//		Database.Users.Update(user);

		//		await ReplyAsync(Context.User.Mention + ", " + "You are no longer linked to an Alta account!");
		//	}
		//	else
		//	{
		//		await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit the 'Account Settings' page in the launcher.");
		//	}
		//}

		[Command("IsLinked"), Alias("Linked")]
		public async Task IsLinked()
		{
			TownUser user = Database.GetUser(Context.User);

			await Database.RefreshAltaUser(user);

			if (user.AltaId <= 0)
			{
				await ReplyAsync(Context.User.Mention + ", " + "You have not linked to an Alta account! To link, visit\n" + LinkUrl);
			}
			else
			{
				var userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(user.AltaId);

				await ReplyAsync(Context.User.Mention + ", " + $"Your account is currently linkedto " + userInfo.Username + "!");
			}
		}

		[Command("Verify")]
		public async Task Verify([Remainder] string encoded)
		{
			await Context.Message.DeleteAsync();

			await IsLinked();
		}
			//	JwtSecurityToken token;
			//	Claim userData;
			//	Claim altaId;

			//	TownUser user = Database.GetUser(Context.User);

			//	try
			//	{
			//		token = new JwtSecurityToken(encoded);

			//		userData = token.Claims.FirstOrDefault(item => item.Type == "user_data");
			//		altaId = token.Claims.FirstOrDefault(item => item.Type == "UserId");
			//	}
			//	catch
			//	{
			//		await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
			//		await Context.Message.DeleteAsync();
			//		return;
			//	}

			//	if (userData == null || altaId == null)
			//	{
			//		await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token.");
			//		await Context.Message.DeleteAsync();
			//	}
			//	else
			//	{
			//		try
			//		{
			//			VerifyData result = JsonConvert.DeserializeObject<VerifyData>(userData.Value);

			//			string test = result.discord.ToLower();
			//			string expected = Context.User.Username.ToLower() + "#" + Context.User.Discriminator;
			//			string alternate = Context.User.Username.ToLower() + " #" + Context.User.Discriminator;


			//			if (test != expected.ToLower() && test != alternate.ToLower())
			//			{
			//				await ReplyAsync(Context.User.Mention + ", " + "Make sure you correctly entered your account info! You entered: " + result.discord + ". Expected: " + expected);
			//				await Context.Message.DeleteAsync();
			//				return;
			//			}

			//			int id = int.Parse(altaId.Value);

			//			bool isValid = await AltaApi.ApiClient.ServicesClient.IsValidShortLivedIdentityTokenAsync(token);

			//			if (isValid)
			//			{
			//				await Link(Context.User, user, id, encoded);
			//			}
			//			else
			//			{
			//				await ReplyAsync(Context.User.Mention + ", " + "Invalid token! Try creating a new one!");
			//				await Context.Message.DeleteAsync();
			//			}
			//		}
			//		catch (Exception e)
			//		{
			//			await ReplyAsync(Context.User.Mention + ", " + "Invalid verification token : " + e.Message);
			//		}
			//	}
			//}
		}
}
