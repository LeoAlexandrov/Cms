using System;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public struct CreateDocumentResult
	{
		public DtoDocumentResult Result { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static CreateDocumentResult AccessForbidden() => new() { Forbidden = true };
		public static CreateDocumentResult BadDocumentParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateDocumentResult DocumentConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static CreateDocumentResult Success(DtoDocumentResult doc) => new() { Result = doc };
	}



	public struct UpdateDocumentResult
	{
		public DtoDocumentResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static UpdateDocumentResult DocumentNotFound() => new() { NotFound = true };
		public static UpdateDocumentResult AccessForbidden() => new() { Forbidden = true };
		public static UpdateDocumentResult BadDocumentParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UpdateDocumentResult DocumentConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static UpdateDocumentResult Success(DtoDocumentResult doc) => new() { Result = doc };
	}



	public struct DeleteDocumentResult
	{
		public bool Ok { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static DeleteDocumentResult DocumentNotFound() => new() { NotFound = true };
		public static DeleteDocumentResult AccessForbidden() => new() { Forbidden = true };
		public static DeleteDocumentResult Success() => new() { Ok = true };
	}



	public struct MoveDocumentResult
	{
		public DtoMoveDocumentResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static MoveDocumentResult DocumentNotFound() => new() { NotFound = true };
		public static MoveDocumentResult AccessForbidden() => new() { Forbidden = true };
		public static MoveDocumentResult Success(int newPosition, int oldPosition, string author, DateTimeOffset modifiedAt) =>
			new()
			{
				Result = new() { NewPosition = newPosition, OldPosition = oldPosition, Author = author, ModifiedAt = modifiedAt }
			};
	}


}