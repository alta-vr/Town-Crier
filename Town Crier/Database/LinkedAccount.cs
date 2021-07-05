using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Town_Crier.Database
{
	public class AltaUserCommon
	{
		[DynamoDBHashKey("id")]
		public int Identifier { get; set; }

		[DynamoDBProperty("username")]
		[DynamoDBGlobalSecondaryIndexHashKey("username-index")]
		public string Username { get; set; }

	}
	
	public enum Service
	{
		Discord
	}

	[DynamoDBTable("LinkedAccounts")]
	public class LinkedAccountTable
	{
		[DynamoDBIgnore]
		public ulong UlongLinkedId => ulong.Parse(LinkedId);

		[DynamoDBHashKey("user_id")]
		public int UserId { get; set; }

		[DynamoDBProperty("linked_id")]
		[DynamoDBGlobalSecondaryIndexHashKey("linked_id-index")]
		public string LinkedId { get; set; }

		[DynamoDBProperty("service", typeof(EnumToStringConverter<Service>))]
		public Service Service { get; set; }

		[DynamoDBProperty("username")]
		public string Username { get; set; }
	}

	public class DiscordAccount : LinkedAccountTable
	{
		[DynamoDBProperty("access_token")]
		public string AccessToken { get; set; }

		[DynamoDBProperty("refresh_token")]
		public string RefreshToken { get; set; }

		[DynamoDBProperty("expires")]
		public DateTime Expires { get; set; }

		[DynamoDBProperty("scopes")]
		public List<string> Scopes { get; set; }

		[DynamoDBProperty("token_type")]
		public string TokenType { get; set; }

		[DynamoDBProperty("avatar")]
		public string Avatar { get; set; }

		[DynamoDBProperty("discriminator")]
		public string Discriminator { get; set; }

		[DynamoDBProperty("public_flags")]
		public int PublicFlags { get; set; }
	}
}
