using System;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public struct CreateAttributeResult
	{
		public DtoDocumentAttributeResult Result { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static CreateAttributeResult AccessForbidden() => new() { Forbidden = true };
		public static CreateAttributeResult BadAttributeParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateAttributeResult AttributeConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static CreateAttributeResult Success(DtoDocumentAttributeResult attr) => new() { Result = attr };
	}



	public struct UpdateAttributeResult
	{
		public DtoDocumentAttributeResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public bool Conflict { get; set; }
		public object Errors { get; set; }

		public static UpdateAttributeResult AttributeNotFound() => new() { NotFound = true };
		public static UpdateAttributeResult AccessForbidden() => new() { Forbidden = true };
		public static UpdateAttributeResult BadAttributeParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UpdateAttributeResult AttributeConflict(object errors) => new() { Conflict = true, Errors = new { errors } };
		public static UpdateAttributeResult Success(DtoDocumentAttributeResult doc) => new() { Result = doc };
	}



	public struct DeleteAttributeResult
	{
		public bool Ok { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }

		public static DeleteAttributeResult AttributeNotFound() => new() { NotFound = true };
		public static DeleteAttributeResult AccessForbidden() => new() { Forbidden = true };
		public static DeleteAttributeResult Success() => new() { Ok = true };
	}


}