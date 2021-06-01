using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TownCrier.Database;
using TownCrier.Services;

namespace TownCrier.Services
{
	class ApiResponse
	{
		public Parse parse;

		public class Parse
		{
			public class Text
			{
				[JsonProperty("*")]
				public string value;
			}
			
			public string title;
			public int pageId;
			public int revId;
			public Text text;
			public string displayTitle;
			public string[] images;
			public Text[] categories;
		}
	}

	class ImageResponse
	{
		public string Url { get { return query?.pages.FirstOrDefault().Value?.imageInfo[0].url; } }

		public Query query;

		public class Query
		{
			public Dictionary<int, Page> pages;

			public class Page
			{
				public ImagegInfo[] imageInfo;

				public class ImagegInfo
				{
					public string url;
				}
			}
		}
	}
	
	public class WikiSearcher
	{
		const string WikiStart = "{";
		const string WikiEnd = "}";

		DiscordSocketClient discord;
		TownDatabase database;

		public WikiSearcher(DiscordSocketClient discord, TownDatabase database)
		{
			this.database = database;
			this.discord = discord;

			discord.MessageReceived += Search;
		}

		async Task Search(SocketMessage message)
		{
			ITextChannel channel = message.Channel as ITextChannel;

			if (channel == null)
			{
				return;
			}

			TownGuild guild = database.GetGuild(channel.Guild);

			if (guild == null)
			{
				return;
			}

			await ShowWiki(message, guild);
		}

		public async Task ShowWiki(SocketMessage message, TownGuild guild)
		{
			string url = guild.WikiUrl;

			if (url == null || message.Content.Contains('`'))
			{
				return;
			}

			try
			{
				List<string> items = GetWikiItems(message);

				if (items.Count == 0)
				{
					return;
				}

				var builder = new EmbedBuilder()
				.WithColor(new Color(0xC9881E))
				.WithTimestamp(DateTime.UtcNow)
				.WithThumbnailUrl(guild.WikiIcon)
				.WithAuthor(author =>
				{
					author
					.WithName(guild.WikiName)
					.WithUrl(url);
				});

				if (items.Count > 1)
				{
					//Stop items from looking for image
					builder.ImageUrl = builder.ThumbnailUrl;
				}

				foreach (string item in items)
				{
					await GetWikiDescription(url, item, builder);
				};

				if (items.Count > 1)
				{
					builder.ImageUrl = null;
				}

				Embed embed = builder.Build();

				await message.Channel.SendMessageAsync(
					"Hopefully this will help!",
					embed: embed)
					.ConfigureAwait(false);
			}
			catch (Exception e)
			{
				Console.WriteLine(e.Message);
			}
		}

		List<string> GetWikiItems(SocketMessage message)
		{
			HashSet<string> lowerCase = new HashSet<string>();
			List<string> result = new List<string>();

			int index = 0;

			string modifiedContent = message.Content;

			do
			{
				index = message.Content.IndexOf(WikiStart, index);

				if (index != -1)
				{
					int endIndex = message.Content.IndexOf(WikiEnd, index);

					if (endIndex != -1)
					{
						index += WikiStart.Length;

						int length = endIndex - index;

						if (length == 0 && length > 30)
						{
							continue;
						}

						string content = message.Content.Substring(index, length);

						if (content.Any(character => !char.IsLetterOrDigit(character) && character != '_' && character != ' ' && character != ':' && character != '#' && character != '.'))
						{
							continue;
						}

						modifiedContent.Replace(WikiStart + content + WikiEnd, content);

						if (lowerCase.Add(content.ToLower()))
						{
							result.Add(content);
						}
					}

					index = endIndex;
				}
			}
			while (index != -1);

			return result;
		}

		async Task GetWikiDescription(string baseUrl, string item, EmbedBuilder builder, bool isFixing = true)
		{
			string targetUrl = baseUrl;
			string[] split = item.Split('#');

			item = split[0];

			string categories = null;

			using (HttpClient httpClient = new HttpClient())
			{
				string description = null;

				if (isFixing)
				{
					HttpResponseMessage apiSearch = await httpClient.GetAsync(baseUrl + "/api.php?action=opensearch&profile=fuzzy&redirects=resolve&search=" + item);

					//Format of result is really weird (array of mismatched types).
					//Adding in square brackets to make first item a string array (rather than just string)
					string result = apiSearch.Content.ReadAsStringAsync().Result;
					result = result.Insert(1, "[");
					result = result.Insert(result.IndexOf(','), "]");

					string[][] array = JsonConvert.DeserializeObject<string[][]>(result);

					if (array == null || array[1].Length == 0)
					{
						description = "Page not found";

						targetUrl += "/" + item.Replace(" ", "_");

						description += $"\n[Click here to create it!]({targetUrl})";
					}
					else
					{
						item = array[1][0];
					}
				}

				if (description == null)
				{
					HttpResponseMessage apiResponseText = await httpClient.GetAsync(baseUrl + "/api.php?format=json&action=parse&page=" + item);

					ApiResponse apiResponse = null;

					try
					{
						string text = apiResponseText.Content.ReadAsStringAsync().Result;

						apiResponse = JsonConvert.DeserializeObject<ApiResponse>(text);
					}
					catch (Exception e)
					{
						Console.WriteLine(e.Message);
						apiResponse = new ApiResponse();
					}

					if (apiResponse.parse == null)
					{
						if (!isFixing)
						{
							await GetWikiDescription(baseUrl, item, builder, true);
							return;
						}
					}
					else
					{
						targetUrl += "/" + item.Replace(" ", "_");

						int startSearch = 0;

						if (split.Length > 1)
						{
							startSearch = apiResponse.parse.text.value.IndexOf("mw-headline\" id=\"" + split[1], StringComparison.InvariantCultureIgnoreCase);

							if (startSearch < 0)
							{
								startSearch = apiResponse.parse.text.value.IndexOf("<p><b>" + split[1], StringComparison.InvariantCultureIgnoreCase);

								if (startSearch < 0)
								{
									startSearch = 0;
								}
								else
								{
									//Need to look for best subheading

									int headlineStart = apiResponse.parse.text.value.LastIndexOf("mw-headline\" id=\"", startSearch, startSearch) + 17;

									int headlineEnd = apiResponse.parse.text.value.IndexOf("\"", headlineStart);

									string headline = apiResponse.parse.text.value.Substring(headlineStart, headlineEnd - headlineStart);

									split[1] = headline;
								}
							}

							targetUrl += '#' + split[1];
						}

						int start = apiResponse.parse.text.value.IndexOf("<p>", startSearch);

						if (start < 0 && startSearch > 0)
						{
							start = apiResponse.parse.text.value.IndexOf("<p>");
						}

						if (builder.ImageUrl == null && apiResponse.parse.images.Length > 0)
						{
							for (int i = 0; i < apiResponse.parse.images.Length; i++)
							{
								if (apiResponse.parse.text.value.IndexOf(apiResponse.parse.images[i]) < startSearch)
								{
									continue;
								}

								HttpResponseMessage imageResponse = await httpClient.GetAsync(baseUrl + "/api.php?action=query&prop=imageinfo&format=json&iiprop=url&titles=File:" + apiResponse.parse.images[i]);

								string text = imageResponse.Content.ReadAsStringAsync().Result;

								try
								{
									builder.ImageUrl = JsonConvert.DeserializeObject<ImageResponse>(text).Url;
								}
								catch
								{
									builder.ImageUrl = null;
								}

								break;
							}
						}

						if (start >= 0)
						{
							int end = apiResponse.parse.text.value.IndexOf("</p>", start);

							if (end > 0)
							{
								start += 3;

								description = apiResponse.parse.text.value.Substring(start, end - start)
								.Replace("<b>", "**")
								.Replace("</b>", "**")
								.Trim();

								description = Regex.Replace(description, item + "s?", match => $"[{match.Value}]({targetUrl})");

								RemoveHtml(ref description);
							}
						}

						if (description == null)
						{
							description = "No description found";
						}

						description += $"\n[Click here for more info]({targetUrl})";
					}

					if (apiResponse.parse.categories.Length > 0)
					{
						categories += "";

						for (int i = 0; i < apiResponse.parse.categories.Length; i++)
						{
							string value = apiResponse.parse.categories[i].value;

							categories += $"[{value}]({baseUrl}/Category:{value})";

							if (i + 1 < apiResponse.parse.categories.Length)
							{
								categories += ", ";
							}
						}
					}
				}
				
				builder.AddField(item, description);

				if (categories != null)
				{
					builder.AddField("Categories", categories);
				}
			};
		}

		void RemoveHtml(ref string description)
		{
			int index = 0;

			int linkFrom = -1;
			string link = null;

			do
			{
				index = description.IndexOf('<');

				if (index != -1)
				{
					if (linkFrom >= 0)
					{
						if (index - linkFrom > 1)
						{
							string subString = description.Substring(linkFrom, index - linkFrom);

							description = description.Remove(linkFrom, index - linkFrom);

							description = description.Insert(linkFrom, $"[{subString}]({link})");
						}

						linkFrom = -1;
						link = null;

						continue;
					}

					int endIndex = description.IndexOf('>', index);

					if (endIndex != -1)
					{
						int length = endIndex - index + 1;

						if (description[index + 1] == 'a')
						{
							int linkIndex = description.IndexOf("href", index, length);

							if (linkIndex > 0)
							{
								linkIndex += 6;

								int linkEnd = description.IndexOf("\"", linkIndex);

								if (linkEnd > 0)
								{
									linkFrom = index;

									link = description.Substring(linkIndex, linkEnd - linkIndex);

									if (link.StartsWith("/"))
									{
										link = "https://townshiptale.gamepedia.com" + link;
									}
								}
							}
						}

						description = description.Remove(index, length);
					}
				}
			}
			while (index != -1);

			description = description.Replace("\n\n", "\n");
		}
	}
}
