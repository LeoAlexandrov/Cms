using System;
using System.ComponentModel.DataAnnotations;

using MessagePack;

using AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Application.Dto
{

	[MessagePackObject]
	public class DtoUserLiteResult
	{
		[MessagePack.Key("id")]
		public int Id { get; set; }

		[MessagePack.Key("login")]
		public string Login { get; set; }

		[MessagePack.Key("name")]
		public string Name { get; set; }

		[MessagePack.Key("email")]
		public string Email { get; set; }

		[MessagePack.Key("avatar")]
		public string Avatar { get; set; }

		[MessagePack.Key("role")]
		public string Role { get; set; }

		[MessagePack.Key("isEnabled")]
		public bool IsEnabled { get; set; }

		[MessagePack.Key("isDemo")]
		public bool IsDemo { get; set; }

		[MessagePack.Key("locale")]
		public string Locale { get; set; }

		[MessagePack.Key("lastSignIn")]
		public DateTimeOffset? LastSignIn { get; set; }

		public DtoUserLiteResult() { }

		internal DtoUserLiteResult(User user)
		{
			if (user != null)
			{
				Id = user.Id;
				Login = user.Login;
				Name = user.Name;
				Email = user.Email;
				Avatar = user.Avatar;
				Role = user.Role;
				IsEnabled = user.IsEnabled;
				IsDemo = user.IsDemo;
				Locale = user.Locale;
				LastSignIn = user.LastSignIn;
			}
		}
	}



	[MessagePackObject]
	public class DtoUserResult : DtoUserLiteResult
	{
		[MessagePack.Key("apikey")]
		public string ApiKey { get; set; }

		public DtoUserResult() { }

		internal DtoUserResult(User user, bool includeApiKey) : base(user)
		{
			if (user != null)
			{
				ApiKey = includeApiKey ? user.ApiKey : null;
			}
		}
	}



	[MessagePackObject]
	public class DtoCreateUser
	{
		[MessagePack.Key("login")]
		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Login { get; set; }

		[MessagePack.Key("role")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string Role { get; set; }
	}



	[MessagePackObject]
	public class DtoUpdateUser
	{
		[MessagePack.Key("role")]
		[Required(AllowEmptyStrings = false), MaxLength(128)]
		public string Role { get; set; }

		[MessagePack.Key("name")]
		[MaxLength(128)]
		public string Name { get; set; }

		[MessagePack.Key("email")]
		[MaxLength(260)]
		public string Email { get; set; }

		[MessagePack.Key("locale")]
		[RegularExpression(@"^\w\w(\-\w\w)?$")]
		public string Locale { get; set; }

		[MessagePack.Key("resetApiKey")]
		[Required]
		public bool? ResetApiKey { get; set; }

		[MessagePack.Key("isEnabled")]
		[Required]
		public bool? IsEnabled { get; set; }
	}



	[MessagePackObject]
	public class DtoDeleteUserResult
	{
		[MessagePack.Key("signout")]
		public bool Signout { get; set; }
	}

}