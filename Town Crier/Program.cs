﻿using Discord;
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
using System.Threading.Tasks;
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
			Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "Data"));
			_client = new DiscordSocketClient();
			_config = BuildConfig();

			var services = ConfigureServices();
			services.GetRequiredService<LogService>();
			await services.GetRequiredService<CommandHandlingService>().InitializeAsync(services);

			await _client.LoginAsync(TokenType.Bot, _config["token"]);
			await _client.SetGameAsync(_config["status"]);
			await _client.StartAsync();

			await Task.Delay(-1);
		}

		IServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
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
				.AddSingleton(new InteractiveService(_client))
				// Adds Database
				.AddSingleton(new LiteDatabase(Path.Combine(Directory.GetCurrentDirectory(), "Data", "Database.db")))
				.AddSingleton<TownDatabase>()
				// Initializes Activity Role Service
				.AddSingleton<ActivityRoleService>()
				// Initializes AltaAPIService
				.AddSingleton<AltaAPI>()
				// Initializes other functionality
				.AddSingleton<CrossAlerter>()
				.AddSingleton<NewcomerService>()
				.AddSingleton<WikiSearcher>()
				// Build
				.BuildServiceProvider();
		}

		IConfiguration BuildConfig()
		{
			return new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "config.json"))
				.Build();
		}
	}
}
