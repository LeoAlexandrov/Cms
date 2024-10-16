using System;

using AleProjects.Cms.Application.Dto;


namespace AleProjects.Cms.Application.Services
{

	public struct CreateSchemaResult
	{
		public DtoSchemaResult Result { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static CreateSchemaResult AccessForbidden() => new() { Forbidden = true };
		public static CreateSchemaResult BadSchemaParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static CreateSchemaResult Success(DtoSchemaResult schema) => new() { Result = schema };
	}



	public struct UpdateSchemaResult
	{
		public DtoSchemaResult Result { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static UpdateSchemaResult SchemaNotFound() => new() { NotFound = true };
		public static UpdateSchemaResult AccessForbidden() => new() { Forbidden = true };
		public static UpdateSchemaResult BadSchemaParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static UpdateSchemaResult Success(DtoSchemaResult schema) => new() { Result = schema };
	}



	public struct DeleteSchemaResult
	{
		public bool Ok { get; set; }
		public bool NotFound { get; set; }
		public bool Forbidden { get; set; }
		public bool BadParameters { get; set; }
		public object Errors { get; set; }

		public static DeleteSchemaResult SchemaNotFound() => new() { NotFound = true };
		public static DeleteSchemaResult AccessForbidden() => new() { Forbidden = true };
		public static DeleteSchemaResult BadSchemaParameters(object errors) => new() { BadParameters = true, Errors = new { errors } };
		public static DeleteSchemaResult Success() => new() { Ok = true };
	}


}