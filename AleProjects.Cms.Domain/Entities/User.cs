using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Claims;



namespace AleProjects.Cms.Domain.Entities
{
	public class User
	{
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int Id { get; set; }

		[Required(AllowEmptyStrings = false), MaxLength(260)]
		public string Login { get; set; }

		[MaxLength(128)]
		public string PasswordHash { get; set; }

		[MaxLength(128)]
		public string Name { get; set; }

		[MaxLength(260)] 
		public string Email { get; set; }

		public string Avatar { get; set; }

		[MaxLength(128)]
		public string Role { get; set; }

		public bool IsEnabled { get; set; }

		public bool IsDemo { get; set; }

		[MaxLength(16)]
		public string Locale { get; set; }

		[MaxLength(128)]
		public string ApiKey { get; set; }

		public DateTimeOffset? LastSignIn { get; set; }
	}
}
