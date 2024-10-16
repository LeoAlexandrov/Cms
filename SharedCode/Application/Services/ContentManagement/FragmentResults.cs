using System;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{


	public struct GetFragmentResult
	{
		public DtoFullFragmentResult Result { get; set; }
		public bool NotFound { get; set; }

		public static GetFragmentResult FragmentNotFound() => new() { NotFound = true };

		public static GetFragmentResult Success(DtoFullFragmentResult f) => new() { Result = f };
	}



	public struct CreateFragmentResult
	{
		public DtoFragmentChangeResult Result { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static CreateFragmentResult AccessForbidden() => new() { Forbidden = true };
		public static CreateFragmentResult BadDocumentParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateFragmentResult Success(DtoFragmentResult f, DtoFragmentLinkResult fl, string author, DateTimeOffset modifiedAt) =>
			new()
			{
				Result = new() { Fragment = f, Link = fl, Author = author, ModifiedAt = modifiedAt }
			};
	}



	public struct UpdateFragmentResult
	{
		public DtoFragmentChangeResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static UpdateFragmentResult FragmentNotFound() => new() { NotFound = true };
		public static UpdateFragmentResult AccessForbidden() => new() { Forbidden = true };
		public static UpdateFragmentResult BadFragmentParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UpdateFragmentResult FragmentConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static UpdateFragmentResult Success(DtoFragmentResult f, DtoFragmentLinkResult fl, bool sharedStateChanged, string author, DateTimeOffset modifiedAt) =>
			new()
			{
				Result = new() { Fragment = f, Link = fl, SharedStateChanged = sharedStateChanged, Author = author, ModifiedAt = modifiedAt }
			};
	}



	public struct DeleteFragmentResult
	{
		public DtoDocumentChangeResult Result { get; set; }
		public bool Ok { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static DeleteFragmentResult FragmentNotFound() => new() { NotFound = true };
		public static DeleteFragmentResult AccessForbidden() => new() { Forbidden = true };
		public static DeleteFragmentResult Success(string author, DateTimeOffset modifiedAt) =>
			new()
			{
				Result = new() { Author = author, ModifiedAt = modifiedAt },
				Ok = true
			};
	}



	public struct MoveFragmentResult
	{
		public DtoMoveFragmentResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static MoveFragmentResult FragmentNotFound() => new() { NotFound = true };
		public static MoveFragmentResult AccessForbidden() => new() { Forbidden = true };
		public static MoveFragmentResult Success(int newPosition, int oldPosition, string author, DateTimeOffset modifiedAt) =>
			new()
			{
				Result = new() { NewPosition = newPosition, OldPosition = oldPosition, Author = author, ModifiedAt = modifiedAt }
			};
	}

}