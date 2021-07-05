//using Alta.WebApi.Models;
//using Alta.WebApi.Models.DTOs.Responses;
//using Discord.WebSocket;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using TownCrier.Database;
//using Discord;
//using Amazon.DynamoDBv2.DocumentModel;

//namespace TownCrier.Services
//{
//	public class AccountService
//	{
//		public DiscordSocketClient Client { get; set; }
//		public TownDatabase Database { get; set; }
//		public AltaAPI AltaApi { get; set; }

//		static SocketGuild guild;
//		static SocketRole supporterRole;
//		static SocketTextChannel supporterChannel;
//		static SocketTextChannel supporterSpoilersChannel;
//		static SocketTextChannel generalChannel;

//		public AccountService(DiscordSocketClient client, TownDatabase database, AltaAPI altaApi, TimerService timer)
//		{
//			Client = client;
//			Database = database;
//			AltaApi = altaApi;

//			timer.OnClockInterval += UpdateAll;

//			//Console.WriteLine(database.Users.Count(item => item.AltaInfo != null));

//			//Migrate();

//			//UpdateAll(true);
//		}

//		//void Migrate()
//		//{
//		//	string target = "AltaLink.txt";

//		//	FileInfo fileInfo = new FileInfo($"./{target}");

//		//	if (!fileInfo.Exists)
//		//	{
//		//		Console.WriteLine("Can't find AltaLinks");
//		//		return;
//		//	}

//		//	Console.WriteLine("Migrating with AltaLinks");



//		//	using (StreamReader reader = new StreamReader($"./{target}"))
//		//	{
//		//		while (!reader.EndOfStream)
//		//		{
//		//			string line = reader.ReadLine().Trim();

//		//			string[] split = line.Split(' ');

//		//			if (split.Length == 3)
//		//			{
//		//				ulong discord = ulong.Parse(split[0]);
//		//				string name = split[1];

//		//				if (!int.TryParse(split[2], out int id))
//		//				{
//		//					Console.WriteLine("Failed to parse id for " + line);
//		//					continue;
//		//				}

//		//				TownUser user = Database.Users.FindOne(item => item.UserId == discord);

//		//				if (user == null)
//		//				{
//		//					Console.WriteLine("Couldn't find user " + discord + " for " + name);
//		//					continue;
//		//				}

//		//				if (user.AltaInfo != null)
//		//				{
//		//					//Console.WriteLine("Already processed " + line);
//		//					continue;
//		//				}

//		//				Console.WriteLine("Connecting " + user.Name + " to " + name);

//		//				user.AltaInfo = new UserAltaInfo()
//		//				{
//		//					Identifier = id,
//		//					Username = name
//		//				};

//		//				Database.Users.Update(user);

//		//				UpdateAsync(user, null).Wait();
//		//			}
//		//			else
//		//			{
//		//				Console.WriteLine("Length not 3 " + line);
//		//			}
//		//		}
//		//	}
//		//}

//		async void UpdateAll(object sender, IServiceProvider e)
//		{
//			await UpdateAll();
//		}

//		void InitializeFields()
//		{
//			DateTime time = DateTime.Now;

//			if (guild == null)
//			{
//				var dbEntry = Database.Guilds.FindOne();

//				if (dbEntry == null)
//				{
//					Console.WriteLine("Couldnt get a guild");
//					return;
//				}

//				guild = Client.GetGuild(dbEntry.GuildId);
//			}

//			if (supporterRole == null && guild != null)
//			{
//				supporterRole = guild.GetRole(547202953505800233);

//				supporterChannel = guild.GetTextChannel(547204432144891907);

//				generalChannel = guild.GetChannel(334933825383563266) as SocketTextChannel;
//			}
//		}

//		public async Task UpdateAll(bool isForced = false)
//		{
//			try
//			{
//				DateTime time = DateTime.Now;
//				DateTime day = time.Date;

//				InitializeFields();

//				if (isForced)
//				{
//					//DynamoTableAccess<TownUser> dynamoOnly = Database.Users as DynamoTableAccess<TownUser>;

//					//if (dynamoOnly != null)
//					//{
//					//	foreach (TownUser user in dynamoOnly.FindAllByIndex(0, QueryOperator.GreaterThanOrEqual, "alta_id-index"))
//					//	{
//					//		await UpdateAsync(user, null);
//					//		await Task.Delay(20);
//					//	}
//					//}
//					//else
//					{
//						foreach (TownUser user in Database.Users.FindAll())
//						{
//							await UpdateAsync(user, null);
//							await Task.Delay(20);
//						}
//					}
//				}
//				else
//				{
//					foreach (TownUser user in Database.Users.FindAllByIndex(day, "supporter_expiry_day-index", "SupporterExpiryDay"))
//					{
//						await UpdateAsync(user, null);
//						await Task.Delay(20);
//					}
//				}
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine(e.Message);
//				throw e;
//			}
//		}

//		public async Task UpdateAsync(TownUser townUser, SocketGuildUser user)
//		{

//			if (townUser.AltaInfo == null)
//			{
//				return;
//			}

//			if (guild == null)
//			{
//				guild = Client.GetGuild(Database.Guilds.FindOne().GuildId);
//			}

//			try
//			{
//				UserInfo userInfo = await AltaApi.ApiClient.UserClient.GetUserInfoAsync(townUser.AltaInfo.Identifier);

//				MembershipStatusResponse result = await AltaApi.ApiClient.UserClient.GetMembershipStatus(townUser.AltaInfo.Identifier);

//				if (userInfo == null)
//				{
//					Console.WriteLine("Couldn't find userinfo for " + townUser.Name);
//					return;
//				}

//				//if (townUser.AltaInfo != null && townUser.AltaInfo.IsSupporter)
//				//{
//				//	Console.WriteLine("Couldn't find membership status for " + townUser.Name);
//				//	return;
//				//}

//				bool wasSupporter = townUser.AltaInfo.IsSupporter;

//				townUser.AltaInfo.SupporterExpiry = result.ExpiryTime ?? DateTime.MinValue;

//				townUser.AltaInfo.IsSupporter = result.IsMember;
//				townUser.AltaInfo.Username = userInfo.Username;

//				Console.WriteLine("JUST UPDATED: " + userInfo.Username);

//				if (user == null)
//				{
//					user = guild.GetUser(townUser.UserId);
//				}

//				if (user == null)
//				{
//					Console.WriteLine("Couldn't find Discord user for " + townUser.Name + " " + townUser.UserId);
//					return;
//				}

//				if (supporterRole == null)
//				{
//					supporterRole = guild.GetRole(547202953505800233);
//					supporterChannel = guild.GetTextChannel(547204432144891907);
//					supporterSpoilersChannel = guild.GetTextChannel(560314409549824008);
//					generalChannel = guild.GetChannel(334933825383563266) as SocketTextChannel;
//				}

//				if (townUser.AltaInfo.IsSupporter)
//				{
//					if (user.Roles == null || !user.Roles.Contains(supporterRole))
//					{
//						try
//						{
//							await user.AddRoleAsync(supporterRole);
//						}
//						catch (Exception)
//						{
//							Console.WriteLine("Error adding role");
//							Console.WriteLine(user);
//							Console.WriteLine(supporterRole);
//						}

//						if (!wasSupporter && supporterChannel != null)
//						{
//							await supporterChannel.SendMessageAsync($"{user.Mention} joined. Thanks for the support!");

//							await SendGeneralMessage(user);
//						}
//					}
//				}
//				else if (wasSupporter)
//				{
//					Console.WriteLine("UNSUPPORT : " + user.Username);

//					await user.SendMessageAsync("Oh no! You've lost your supporter role! It's been great having you with us in <#547204432144891907>. If you didn't mean to unsupport, check the Support page in your launcher ( or https://townshiptale.com/supporter ) to be sure your payment didn't fail. If you think this was a mistake, be sure to contact Joel, and he'll help you out!");

//					await user.RemoveRoleAsync(supporterRole);
//				}

//				Database.Users.Update(townUser);
//			}
//			catch (Exception e)
//			{
//				Console.WriteLine("Error updating " + townUser.Name);
//				Console.WriteLine(e.Message);
//			}
//		}

//		public async Task SendGeneralMessage(SocketGuildUser user)
//		{
//			InitializeFields();

//			EmbedBuilder embed = new EmbedBuilder();

//			embed.Description = $"{user.Username} became a supporter! Thanks for the support! Be sure to check out the <#547204432144891907> and <#560314409549824008> channels!";

//			embed.Url = "https://townshiptale.com/supporter";

//			embed.ImageUrl = "https://cdn.discordapp.com/attachments/547204432144891907/612124820863320066/image0.png";

//			embed.Title = "Parttaaayyy!";

//			embed.Footer = new EmbedFooterBuilder()
//				.WithText("Keen to join in? Click 'Parttaaayyy' above!")
//				.WithIconUrl("https://cdn.discordapp.com/attachments/547204432144891907/643959153475190798/Supporter2.png");
			
//			await generalChannel.SendMessageAsync($"{user.Mention}", false, embed.Build());
//		}
//	}
//}
