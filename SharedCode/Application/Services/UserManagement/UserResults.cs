using System;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public struct GetUserResult
	{
		public DtoUserResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static GetUserResult UserNotFound() => new() { NotFound = true };
		public static GetUserResult AccessForbidden() => new() { Forbidden = true };
		public static GetUserResult Success(DtoUserResult user) => new() { Result = user };
	}



	public struct CreateUserResult
	{
		public DtoUserResult Result { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static CreateUserResult AccessForbidden() => new() { Forbidden = true };
		public static CreateUserResult BadUserParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateUserResult UserConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static CreateUserResult Success(DtoUserResult user) => new() { Result = user };
	}



	public struct UpdateUserResult
	{
		public DtoUserResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static UpdateUserResult UserNotFound() => new() { NotFound = true };
		public static UpdateUserResult AccessForbidden() => new() { Forbidden = true };
		public static UpdateUserResult BadUserParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UpdateUserResult Success(DtoUserResult user) => new() { Result = user };
	}



	public struct DeleteUserResult
	{
		public DtoDeleteUserResult Result { get; set; } 
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static DeleteUserResult UserNotFound() => new() { NotFound = true };
		public static DeleteUserResult AccessForbidden() => new() { Forbidden = true };
		public static DeleteUserResult BadUserParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static DeleteUserResult Success(bool signout) => new() { Result = new() { Signout = signout } };

	}
}