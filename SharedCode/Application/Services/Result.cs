using System;
using System.Collections.Generic;
using System.Linq;



namespace AleProjects.Cms.Application.Services
{

	public enum ResultType
	{
		Success,
		BadParameters,
		NotFound,
		Forbidden,
		Conflict,
		Other
	}


	public struct Result<T>
	{
		public T Value;
		public ResultType Type;
		public object Errors;

		public readonly bool Ok { get => Type == ResultType.Success; }
		public readonly bool IsBadParameters { get => Type == ResultType.BadParameters; }
		public readonly bool IsConflict { get => Type == ResultType.Conflict; }
		public readonly bool IsNotFound { get => Type == ResultType.NotFound; }
		public readonly bool IsForbidden { get => Type == ResultType.Forbidden; }
		public readonly bool IsOther { get => Type == ResultType.Other; }


		public static Result<T> Success(T value) => new() { Value = value, Type = ResultType.Success };

		public static Result<T> BadParameters(object errors) => new() { Type = ResultType.BadParameters, Errors = new { errors } };

		public static Result<T> BadParameters(string name, params string[] messages) =>
			new()
			{
				Type = ResultType.BadParameters,
				Errors = new { errors = new Dictionary<string, string[]> { { name, messages.ToArray() } } }
			};

		public static Result<T> Conflict(object errors) => new() { Type = ResultType.Conflict, Errors = new { errors } };

		public static Result<T> Conflict(string name, params string[] messages) =>
			new()
			{
				Type = ResultType.Conflict,
				Errors = new { errors = new Dictionary<string, string[]> { { name, messages.ToArray() } } }
			};

		public static Result<T> NotFound() => new() { Type = ResultType.NotFound };
		
		public static Result<T> Forbidden() => new() { Type = ResultType.Forbidden };
		
		public static Result<T> Other(object errors) => new() { Type = ResultType.Other, Errors = errors };

		public static implicit operator Result<T>(T value) => Success(value);
	}
}