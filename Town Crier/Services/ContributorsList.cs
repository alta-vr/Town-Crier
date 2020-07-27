using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Town_Crier.Services
{
    class ContributorsList
    {
		DiscordSocketClient discord;
		HttpClient Client;
		JsonSerializerSettings serializerSettings;

		public ContributorsList(DiscordSocketClient discord)
		{
			
			this.discord = discord;

			discord.MessageReceived += Contributors;

			Client = new HttpClient();
			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Add("User-Agent", "Town Crier (Discord Bot)");

			serializerSettings = new JsonSerializerSettings();
			serializerSettings.MissingMemberHandling = MissingMemberHandling.Ignore;
			
		}

		async Task Contributors(SocketMessage message)
		{
			if (message.Content.ToLower().Contains("!contributors"))
			{
				//line break before user list
				string contributerlist = "\n";
				HttpResponseMessage ContributersResponseMessage = await Client.GetAsync("https://api.github.com/repos/alta-vr/Town-Crier/stats/contributors");

				
				GithubResponseFormat[] Contributers = JsonConvert.DeserializeObject<GithubResponseFormat[]>(await ContributersResponseMessage.Content.ReadAsStringAsync(), serializerSettings);

				foreach (GithubResponseFormat contributer in Contributers)
				{
					contributerlist += contributer.author.login;
					contributerlist += "\n";
				}


				await message.Channel.SendMessageAsync("These are the people who helped make Town Crier! " + contributerlist);
			}
		}
		// there is a lot more in this format but its unused currently so i cant be bothered implementing it
		private struct GithubAuthorFormat
		{
			/// <summary>
			/// practically just the username
			/// </summary>
			public string login;
			public string type;
		}
		private struct GithubResponseFormat
		{
			public GithubAuthorFormat author;
		}
	}
	
}
