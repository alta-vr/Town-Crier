using Alta.WebApi.Client;
using LiteDB;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using TownCrier.Database;

namespace TownCrier.Services
{
    public class AltaAPI
    {
        const int Timeout = 40;

        public IHighLevelApiClient ApiClient { get; set; }

        
        SHA512 sha512 = new SHA512Managed();

        public AltaAPI()
        {
            StartWithEndpoint(HighLevelApiClientFactory.ProductionEndpoint);

            EnsureLoggedIn().Wait();
            //No need to update username/identifier on interval. Needs to be changed to update supporter status, only for those who it's passed expiry for
            //this.timer.OnClockInterval += _timer_OnClockInterval;
        }

        //async void _timer_OnClockInterval(object sender, IServiceProvider e)
        //{
        //	var col = database.Users;
        //	var users = col.FindAll();

        //	foreach (var x in users.Where(x => x.AltaInfo != null && x.AltaInfo.Identifier != 0))
        //	{
        //		var altauser = await ApiClient.UserClient.GetUserInfoAsync(x.AltaInfo.Identifier);

        //		x.AltaInfo.UpdateAltaCredentials(altauser);

        //		col.Update(x);
        //	}
        //}

        public void StartWithEndpoint(string endpoint)
        {
            if (ApiClient != null)
            {
                Console.WriteLine("Already have an Api Client");
                return;
            }

            SetApiClientLogging();

            ApiClient = HighLevelApiClientFactory.CreateHighLevelClient(endpoint, "TownCrier/1.0", Timeout);
        }

        void SetApiClientLogging()
        {
            //HighLevelApiClientFactory.SetLogging(new AltaLoggerFactory());
        }

        public void StartOffline(LoginCredentials credentials)
        {
            if (ApiClient != null)
            {
                Console.WriteLine("Already have an Api Client");
                return;
            }

            SetApiClientLogging();

            ApiClient = HighLevelApiClientFactory.CreateOfflineHighLevelClient(credentials);
        }

        public async Task EnsureLoggedIn()
        {
            if (!ApiClient.IsLoggedIn)
            {
                string username, password;

				string useEnvVars = Environment.GetEnvironmentVariable("USE_ENV_ALTA_LOGIN");

				if (useEnvVars == "true")
                {
                    GetCredentialsFromEnvironment(out username, out password);
                }
                else
                {
                    GetCredentialsFromFile(out username, out password);
                }

                try
                {
                    await ApiClient.LoginAsync(username, password);
                    Console.WriteLine($"Logged in as {username} \n");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

		void GetCredentialsFromEnvironment(out string username, out string password)
		{
			username = Environment.GetEnvironmentVariable("ALTA_USERNAME");
			password = Environment.GetEnvironmentVariable("ALTA_PASSWORD");

			if (password.Length < 64)
			{
				password = HashString(password);
			}
		}

        void GetCredentialsFromFile(out string username, out string password)
        {
            if (!File.Exists("account.txt"))
            {
                Console.WriteLine("`account.txt` expected next to be next to the .exe " +
                    "with the contents `username|password` (for your Alta account)");
                Console.ReadLine();
                throw new Exception("No credentials provided");
            }

            try
            {
                string[] account = System.IO.File.ReadAllText("account.txt").Trim().Split('|');

                username = account[0].Trim();
                password = account[1].Trim();

                if (password.Length < 64)
                {
                    password = HashString(password);

                    Console.WriteLine("Detected a password in the account file." +
                        " Replaced it with a hash for security reasons.");

                    File.WriteAllText("account.txt", username + "|" + password);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("`account.txt` found, but failed reading the contents." +
                    " Expected format: `username|password` (for your Alta account)");
                Console.ReadLine();
                throw new Exception("Invalid credential format");
            }
        }

        string HashString(string text)
        {
            //return Convert.ToBase64String(sha512.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password)));
            return BitConverter.ToString(sha512.ComputeHash(System.Text.Encoding.UTF8.GetBytes(text))).Replace("-", String.Empty).ToLowerInvariant();
        }
    }
}