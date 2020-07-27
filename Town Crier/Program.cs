using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Discord;
using Discord.Addons.CommandCache;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using LiteDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Town_Crier.Services;
using TownCrier.Services;

namespace TownCrier
{

	class Program
	{
		static void Main(string[] args)
			=> new Program().MainAsync().GetAwaiter().GetResult();

		DiscordSocketClient _client;
		IConfiguration _config;

		public async Task MainAsync()
		{
			Console.WriteLine("STARTING");

			Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data"));
			_client = new DiscordSocketClient();
			_config = BuildConfig();

			var services = ConfigureServices();
			services.GetRequiredService<LogService>();
			await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

			services.GetRequiredService<OutOfOffice>();
			services.GetRequiredService<GettingStartedService>();
			services.GetRequiredService<NewcomerService>();
			services.GetRequiredService<AltaProtocolService>();
			services.GetRequiredService<DoYouCare>();
			services.GetRequiredService<CrossAlerter>();
			services.GetRequiredService<PointCounter>();
			services.GetRequiredService<ContributorsList>();
			services.GetRequiredService<WikiSearcher>();
			services.GetRequiredService<AmIRight>();
			services.GetRequiredService<ChannelFilters>();
			services.GetRequiredService<ActivityRoleService>();
			services.GetRequiredService<Migrator>();
			services.GetRequiredService<AccountService>();
			services.GetRequiredService<AcceptInviteService>();
			services.GetRequiredService<RoutineAnnouncementService>();

			await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN"));
			await _client.SetGameAsync(_config["status"]);
			await _client.StartAsync();

			services.GetRequiredService<UserJoinManagement>();

			await Task.Delay(-1);
		}

		IServiceProvider ConfigureServices()
		{
			bool hasDDB = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("TC_ACCESS_KEY"));
			AWSOptions awsOptions = new AWSOptions()
			{
				Region = RegionEndpoint.APSoutheast2,
				Credentials = new BasicAWSCredentials(Environment.GetEnvironmentVariable("TC_ACCESS_KEY"), Environment.GetEnvironmentVariable("TC_SECRET_KEY")),
			};

			//awsOptions.DefaultClientConfig.HttpClientFactory = new DebugFactory(awsOptions.DefaultClientConfig.HttpClientFactory);

			IServiceCollection result = new ServiceCollection()
				// Base
				.AddSingleton(_client)
				.AddSingleton(new CommandService(new CommandServiceConfig()
				{
					DefaultRunMode = RunMode.Async, // This ensures that a command that isn't "done" (Such as reaction menus) don't block the gateway and cause the bo to go offline.
					CaseSensitiveCommands = false
				})
				)
				.AddSingleton<CommandHandlingService>()
				// Logging
				.AddLogging(x => x.AddConsole())
				.AddSingleton<LogService>()
				// Extra
				.AddSingleton(_config)
				.AddSingleton<TimerService>()
				.AddSingleton(new CommandCacheService(_client))
				.AddSingleton(new InteractiveService(_client));

			// Adds Database
			if (hasDDB)
			{
				result
				.AddDefaultAWSOptions(awsOptions)
				.AddAWSService<IAmazonDynamoDB>();
			}

			if (!hasDDB || !string.IsNullOrEmpty(_config["migrateDdb"]))
			{
				result
				.AddSingleton(new LiteDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database.db")));
			}

			result
				.AddSingleton<TownDatabase>()
				// Initializes AltaAPIService
				.AddSingleton<AltaAPI>()
				.AddSingleton<AltaProtocolService>()
				.AddSingleton<AccountService>()
				.AddSingleton<AcceptInviteService>()
				// Initializes other functionality
				.AddSingleton<ChannelFilters>()
				.AddSingleton<CrossAlerter>()
				.AddSingleton<NewcomerService>()
				.AddSingleton<WikiSearcher>()
				.AddSingleton<PointCounter>()
				.AddSingleton<DoYouCare>()
				.AddSingleton<OutOfOffice>()
				.AddSingleton<ContributorsList>()
				.AddSingleton<AmIRight>()
				.AddSingleton<GettingStartedService>()
				.AddSingleton<ActivityRoleService>()
				.AddSingleton<UserJoinManagement>()
				.AddSingleton<RoutineAnnouncementService>()
				//Migrate
				.AddSingleton<Migrator>();
			
			return result.BuildServiceProvider();
		}

		IConfiguration BuildConfig()
		{
			//if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("env_config")))
			//{
			//	return new ConfigurationBuilder()
			//		.AddEnvironmentVariables()
			//		.Build();
			//}

			return new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"))
				.Build();
		}
	}
}