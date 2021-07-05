using Alta.WebApi.Models;
using Amazon.DynamoDBv2.DataModel;
using LiteDB;
using System;

namespace TownCrier.Database
{
	[DynamoDBTable("TC_Users")]
	public class TownUser
	{
		/// <summary>
		/// The user's Discord ID
		/// </summary>
		[BsonId]
		[DynamoDBHashKey("id")]
		public ulong UserId { get; set; }

		[DynamoDBGlobalSecondaryIndexHashKey("alta_id-index", "alta_id")]
		public int AltaId { get; set; }


		[DynamoDBProperty("name")]
		public string Name { get; set; }

		[DynamoDBProperty("description")]
		public string Description { get; set; }

		[DynamoDBProperty("coins")]
		public int Coins { get; set; }
		
		/// <summary>
		/// Date in which the user first joined the server.
		/// </summary>
		[DynamoDBProperty("initial_join")]
		public DateTime InitialJoin { get; set; }

		//[DynamoDBProperty("alta_info")]
		//public UserAltaInfo AltaInfo { get; set; }

		[DynamoDBProperty("scoring")]
		public UserScoring Scoring { get; set; }
		
		//public void Unlink()
		//{
		//	AltaId = 0;
		//	AltaInfo = null;
		//}

		//public void UpdateAltaCredentials(UserInfo info)
		//{
		//	AltaId = info.Identifier;

		//	if (AltaInfo == null)
		//	{
		//		AltaInfo = new UserAltaInfo();
		//	}

		//	AltaInfo.Identifier = info.Identifier;
		//	AltaInfo.Username = info.Username;
		//}
	}

	public class UserScoring
	{
		[DynamoDBProperty("last_message")]
		public DateTime LastMessage { get; set; }

		[DynamoDBProperty("score")]
		public uint Score { get; set; } = 0;

		[DynamoDBProperty("used_hour_points")]
		public uint UsedHourPoints { get; set; } = 0;

		[DynamoDBProperty("used_first_hour_point")]
		public DateTime UsedFirstHourPoint { get; set; }
	}

	public class UserAltaInfo
	{
		/// <summary>
		/// Alta ID
		/// </summary>
		[DynamoDBProperty("identifier")]
		public int Identifier { get; set; }


		/// <summary>
		/// Returns wether or not the user is currently a supporter.
		/// </summary>
		[DynamoDBProperty("is_supporter")]
		public bool IsSupporter { get; set; }

		/// <summary>
		/// Returns the Alta username tied to this account (Updates evert 15 minutes)
		/// </summary>
		[DynamoDBProperty("username")]
		public string Username { get; set; }
	}

	//public class ChattyUser
	//{
	//	public int bet { get; set; } = 0;

	//	public int coins { get; set; } = 0;

	//	/// <summary>
	//	/// The user's current location
	//	/// </summary>
	//	//public Location CurrentLocation { get; set; }

	//	//public List<Location> Locations { get; set; } = new List<Location>();
	//	//public List<Recipe> Recipes { get; set; } = new List<Recipe>();

	//	/// <summary>
	//	/// Amount of spars the user has won.
	//	/// </summary>
	//	public int WonSpars { get; set; } = 0;
	//	/// <summary>
	//	/// Total amount of spars fought.
	//	/// </summary>
	//	public int Spars { get; set; } = 0;

	//	/// <summary>
	//	/// The win-loss ration % of spars.
	//	/// </summary>
	//	[BsonIgnore]
	//	public decimal WinLoseRatio
	//	{
	//		get
	//		{
	//			return Math.Round((decimal)(Spars * WonSpars) / 100, 2);
	//		}
	//		set { }
	//	}
	//}
}
