using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TownCrier.Services
{
	public static class JwtTokenHandler
	{
		static JwtSecurityTokenHandler handler = new JwtSecurityTokenHandler();

		public static string Write(this JwtSecurityToken token)
		{
			if (token.SigningCredentials == null)
			{
				return token.RawData;
			}

			return handler.WriteToken(token);
		}
	}

	public class AcceptInviteService
	{
		public AltaAPI AltaApi { get; }

		public HttpClient Client { get; }

		public AcceptInviteService(AltaAPI altaApi, TimerService timer)
		{
			AltaApi = altaApi;
			
			timer.OnClockInterval += AcceptAll;

			Client = new HttpClient();
			Client.BaseAddress = new Uri("https://967phuchye.execute-api.ap-southeast-2.amazonaws.com/prod/api/");


			Client.DefaultRequestHeaders.Accept.Clear();
			Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			Client.DefaultRequestHeaders.Add("x-api-key", "2l6aQGoNes8EHb94qMhqQ5m2iaiOM9666oDTPORf");

			altaApi.EnsureLoggedIn().Wait();

			Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", AltaApi.ApiClient.UserCredentials.AccessToken.Write());	

			AcceptAll(null, null);

		}

		async void AcceptAll(object sender, IServiceProvider serviceProvider)
		{
			Console.WriteLine("Checking for invites");

			try
			{
				HttpResponseMessage response = await Client.GetAsync($"{Client.BaseAddress.AbsoluteUri}groups/invites");

				string content = await response.Content.ReadAsStringAsync();

				GroupInvite[] invites = JsonConvert.DeserializeObject<GroupInvite[]>(content);

				if (invites != null)
				{
					for (int i = 0; i < invites.Length; i++)
					{
						Console.WriteLine("Accept " + invites[i].id);
						await Client.PostAsync($"{Client.BaseAddress.AbsoluteUri}groups/invites/" + invites[i].id, null);
					}
				}
				else
				{
					Console.WriteLine("No invites? " + content);
				}

				Console.Write("Done!");
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
				Console.WriteLine(e.StackTrace);
			}
		}
	}

	public class GroupInvite
	{
		public int id;
	}
}
