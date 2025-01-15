using System;
using System.ComponentModel.DataAnnotations;

namespace AleProjects.Cms.Application.Dto
{

	public class RequiredPositiveAttribute : ValidationAttribute
	{
		const string ERROR_NESSAGE_REQUIRED = "The {0} field is required.";
		const string ERROR_MESSAGE_NOT_POSITIVE = "The {0} field must be a positive number.";

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
				return new ValidationResult(string.Format(ERROR_NESSAGE_REQUIRED, validationContext.DisplayName));

			else if (!IsPositive(value))
				return new ValidationResult(string.Format(ERROR_MESSAGE_NOT_POSITIVE, validationContext.DisplayName));

			return ValidationResult.Success;
		}

		private static bool IsPositive(object value)
		{
			return Type.GetTypeCode(value.GetType()) switch
			{
				TypeCode.Int16 => (short)value > 0,
				TypeCode.Int32 => (int)value > 0,
				TypeCode.Int64 => (long)value > 0,
				TypeCode.UInt16 => (ushort)value > 0,
				TypeCode.UInt32 => (uint)value > 0,
				TypeCode.UInt64 => (ulong)value > 0,
				TypeCode.Byte => (byte)value > 0,
				TypeCode.SByte => (sbyte)value > 0,
				TypeCode.Decimal => (decimal)value > 0,
				TypeCode.Double => (double)value > 0,
				TypeCode.Single => (float)value > 0,
				_ => false,
			};
		}
	}



	public class RequiredNonNegativeAttribute : ValidationAttribute
	{
		const string ERROR_NESSAGE_REQUIRED = "The {0} field is required.";
		const string ERROR_MESSAGE_IS_NEGATIVE = "The {0} field must be a non-negative number.";

		protected override ValidationResult IsValid(object value, ValidationContext validationContext)
		{
			if (value == null)
				return new ValidationResult(string.Format(ERROR_NESSAGE_REQUIRED, validationContext.DisplayName));

			else if (!IsNonNegative(value))
				return new ValidationResult(string.Format(ERROR_MESSAGE_IS_NEGATIVE, validationContext.DisplayName));

			return ValidationResult.Success;
		}

		private static bool IsNonNegative(object value)
		{
			return Type.GetTypeCode(value.GetType()) switch
			{
				TypeCode.Int16 => (short)value >= 0,
				TypeCode.Int32 => (int)value >= 0,
				TypeCode.Int64 => (long)value >= 0,
				TypeCode.UInt16 => (ushort)value >= 0,
				TypeCode.UInt32 => (uint)value >= 0,
				TypeCode.UInt64 => (ulong)value >= 0,
				TypeCode.Byte => (byte)value >= 0,
				TypeCode.SByte => (sbyte)value >= 0,
				TypeCode.Decimal => (decimal)value >= 0,
				TypeCode.Double => (double)value >= 0,
				TypeCode.Single => (float)value >= 0,
				_ => false,
			};
		}

	}

}