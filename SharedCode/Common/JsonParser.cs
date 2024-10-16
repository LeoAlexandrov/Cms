/* (c) AleProjects.com, 2021
 * v.1.0
 * 
 * JSON parser
 * 
 * MIT License
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;


namespace AleProjects.Json
{
	// https://www.ietf.org/rfc/rfc4627.txt


	public class JsonParseException : Exception
	{
		public JsonParseException(int code, int line, int position, string message) : base(message)
		{
			Data.Add("Code", code);
			Data.Add("Line", line);
			Data.Add("Position", position);
		}
	}



	public class JsonDoc
	{

		protected enum NumberType
		{
			Integer = 1,
			Decimal = 2,
			Double = 3,
		}

		protected enum LastProcessedElement
		{
			TextConstant		= 0x00000001,
			Number				= 0x00000002,
			BoolConstant		= 0x00000004,
			Null				= 0x00000008,
			PrimitiveValue		= TextConstant + Number + BoolConstant + Null,

			ObjectStart			= 0x00000010,
			ObjectKey			= 0x00000020,
			KeyValueSep			= 0x00000040,
			ObjectEnd			= 0x00000080,

			ArrayStart			= 0x00000100,
			ArrayEnd			= 0x00000200,

			ListSeparator		= 0x00001000,

			Value				= PrimitiveValue + ObjectEnd + ArrayEnd
		}


		public class ParsingSettings
		{
			public bool StrictPropertyNames { get; set; }
			public bool AllowComments { get; set; }
			public bool RecognizeDateTime { get; set; }
			public bool ForceDoubleInArrays { get; set; }

			public Func<IEnumerable<string>, object> ObjectFactory { get; set; }
			public Func<string, object, object, int> PropertySetter { get; set; }

		}


		public class ParseError
		{
			public const int ARGUMENT_NULL = 1;
			public const int UNEXPECTED_END = 2;
			public const int KEY_NOT_UNIQUE = 3;
			public const int INVALID_KEY = 4;
			public const int UNEXPECTED_TOKEN = 5;
			public const int INCOMPATIBLE_TYPE = 6;
			public const int UNKNOWN_PROPERTY = 7;

			public const string MESSAGE_ARGUMENT_NULL = "Argument is null";
			public const string MESSAGE_UNEXPECTED_END = "Unexpected end";
			public const string MESSAGE_KEY_NOT_UNIQUE = "Key not unique";
			public const string MESSAGE_INVALID_KEY = "Invalid key";
			public const string MESSAGE_UNEXPECTED_TOKEN = "Unexpected token";
			public const string MESSAGE_INCOMPATIBLE_TYPE = "Incompatible type";
			public const string MESSAGE_UNKNOWN_PROPERTY = "Unknown property";
			public const string MESSAGE_UNKNOWN_ERROR = "Unknown error";

			public int Code { get; private set; }
			public int Position { get; private set; }
			public int Line { get; private set; }
			public string Message { get; private set; }

			public ParseError(int code, string text, int index)
			{
				if (text == null)
				{
					Code = code;
					Position = 0;
					Line = 0;
					Message = MESSAGE_ARGUMENT_NULL;

					return;
				}

				if (index >= text.Length)
					index = text.Length - 1;

				int line = 0;
				int position = 0;

				while (index >= 0)
				{
					if (text[index] == '\n')
						line++;

					if (line == 0)
						position++;

					index--;
				}

				string message;

				switch (code)
				{
					case ARGUMENT_NULL:
						message = MESSAGE_ARGUMENT_NULL;
						break;

					case UNEXPECTED_END:
						message = MESSAGE_UNEXPECTED_END;
						break;

					case KEY_NOT_UNIQUE:
						message = MESSAGE_KEY_NOT_UNIQUE;
						break;

					case INVALID_KEY:
						message = MESSAGE_INVALID_KEY;
						break;

					case UNEXPECTED_TOKEN:
						message = MESSAGE_UNEXPECTED_TOKEN;
						break;

					case INCOMPATIBLE_TYPE:
						message = MESSAGE_INCOMPATIBLE_TYPE;
						break;

					case UNKNOWN_PROPERTY:
						message = MESSAGE_UNKNOWN_PROPERTY;
						break;

					default:
						message = MESSAGE_UNKNOWN_ERROR;
						break;
				}

				Code = code;
				Position = position;
				Line = line;
				Message = message;
			}

			public void ThrowException()
			{
				throw new JsonParseException(Code, Line, Position, Message);
			}
		}


		public class JsonObject : Dictionary<string, object>
		{
			public T GetValue<T>(params string[] pathNodes)
			{
				object obj = this;

				for (int i = 0; i < pathNodes.Length; i++)
					if (obj is IReadOnlyList<object> jsonArr)
					{
						if (!int.TryParse(pathNodes[i], out int index) || index >= jsonArr.Count || index < 0)
							throw new IndexOutOfRangeException();

						obj = jsonArr[index];
					}
					else if (!(obj is JsonObject jsonObj && jsonObj.TryGetValue(pathNodes[i], out obj)))
					{
						throw new KeyNotFoundException();
					}


				if (!(obj is T result))
					result = (T)Convert.ChangeType(obj, typeof(T));

				return result;
			}

			public object GetValueOrDefault(params string[] pathNodes)
			{
				object obj = this;

				for (int i = 0; i < pathNodes.Length; i++)
					if (!string.IsNullOrEmpty(pathNodes[i]))
						if (obj is IReadOnlyList<object> jsonArr)
						{
							if (!int.TryParse(pathNodes[i], out int index) || index >= jsonArr.Count || index < 0)
								return default;

							obj = jsonArr[index];
						}
						else if (!(obj is JsonObject jsonObj && jsonObj.TryGetValue(pathNodes[i], out obj)))
						{
							return default;
						}

				return obj;
			}

			public T GetValueOrDefault<T>(params string[] pathNodes)
			{
				object obj = this;

				for (int i = 0; i < pathNodes.Length; i++)
					if (!string.IsNullOrEmpty(pathNodes[i]))
						if (obj is IReadOnlyList<object> jsonArr)
						{
							if (!int.TryParse(pathNodes[i], out int index) || index >= jsonArr.Count || index < 0)
								return default;

							obj = jsonArr[index];
						}
						else if (!(obj is JsonObject jsonObj && jsonObj.TryGetValue(pathNodes[i], out obj)))
						{
							return default;
						}


				if (!(obj is T result))
					try
					{
						result = (T)Convert.ChangeType(obj, typeof(T));
					}
					catch
					{
						result = default;
					}

				return result;
			}

			public bool TryGetValue<T>(out T val, params string[] pathNodes)
			{
				object obj = this;

				for (int i = 0; i < pathNodes.Length; i++)
					if (!string.IsNullOrEmpty(pathNodes[i]))
						if (obj is IReadOnlyList<object> jsonArr)
						{
							if (!int.TryParse(pathNodes[i], out int index) || index >= jsonArr.Count || index < 0)
							{
								val = default;
								return false;
							}

							obj = jsonArr[index];
						}
						else if (!(obj is JsonObject jsonObj && jsonObj.TryGetValue(pathNodes[i], out obj)))
						{
							val = default;
							return false;
						}


				bool result = true;

				if (!(obj is T res))
					try
					{
						val = (T)Convert.ChangeType(obj, typeof(T));
					}
					catch
					{
						val = default;
						result = false;
					}
				else
					val = res;

				return result;
			}

			public T Map<T>(T instance = default) where T : new()
			{
				if (EqualityComparer<T>.Default.Equals(instance, default))
					instance = new T();

				foreach (var prop in instance.GetType().GetProperties())
					if (prop.CanWrite && this.TryGetValue(prop.Name, out object val))
						try
						{
							prop.SetValue(instance, val);
						}
						catch
						{
							try
							{
								prop.SetValue(instance, Convert.ChangeType(val, prop.PropertyType));
							}
							catch
							{
								continue;
							}
						}

				return instance;
			}

		}


		protected class ParsingContext
		{
			protected List<object> JsonArray { get; set; }
			protected object TypedArray { get; set; }
			protected IDictionary<string, object> JsonObject { get; set; }
			protected object TypedObject { get; set; }
			public string PendingObjectKey { get; set; }
			public LastProcessedElement LastProcessed { get; set; }
			public bool IsObject { get => JsonObject != null || TypedObject != null; }
			public bool IsArray { get => JsonObject == null && TypedObject == null; }
			public bool IsTypedObject { get => TypedObject != null; }

			public string PathNode
			{
				get
				{
					if (JsonArray != null) 
						return JsonArray.Count.ToString();

					if (TypedArray != null && TypedArray is IList list) 
						return list.Count.ToString();

					if (LastProcessed == LastProcessedElement.ArrayStart)
						return "0";

					return PendingObjectKey;
				}
			}

			public ParsingContext(object jsonObject)
			{
				if (jsonObject != null)
					if (jsonObject is IDictionary<string, object> dict) 
						JsonObject = dict;
					else 
						TypedObject = jsonObject;
			}


			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public object GetObject()
			{
				return JsonObject ?? TypedObject;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public object GetArray()
			{
				return JsonArray ?? TypedArray;
			}

			public int SetObjectProperty(string name, object value, Func<string, object, object, int> PropertySetter)
			{
				int result;

				if (JsonObject != null)
				{
					JsonObject.Add(name, value);
					result = 0;
				}
				else if (TypedObject != null)
				{
					if (PropertySetter != null)
						try
						{
							result = PropertySetter(name, value, TypedObject);
						}
						catch
						{
							result = -1;
						}
					else
						result = 1;

					if (result > 0)
					{

						var property = TypedObject
							.GetType()
							.GetProperties()
							.FirstOrDefault(p => p.CanWrite && string.Compare(p.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0);

						if (property != null)
							try
							{
								property.SetValue(TypedObject, value);
							}
							catch
							{
								try
								{
									property.SetValue(TypedObject, Convert.ChangeType(value, property.PropertyType));
								}
								catch
								{
									result = -1;
								}
							}
					}
				}
				else
				{
					result = 1;
				}

				return result;
			}

			public void AddObjectToArray(object item)
			{
				if (TypedArray != null)
					ConvertToJsonArrayAndAdd(item);
				else if (JsonArray == null) 
					JsonArray = new List<object>() { item };
				else 
					JsonArray.Add(item);
			}

			public void AddStringToArray(string item)
			{
				if (JsonArray != null) 
					JsonArray.Add(item);
				else if (TypedArray == null) 
					TypedArray = new List<string>() { item };
				else if (TypedArray is List<string> stringList) 
					stringList.Add(item);
				else 
					ConvertToJsonArrayAndAdd(item);
			}

			public void AddNumberToArray(object item)
			{
				if (JsonArray != null) 
					JsonArray.Add(item);
				else if (TypedArray == null)
				{
					switch (item)
					{
						case double doubleVal:
							TypedArray = new List<double>() { doubleVal };
							break;

						case int intVal:
							TypedArray = new List<int>() { intVal };
							break;

						case long longVal:
							TypedArray = new List<long>() { longVal };
							break;

						default:
							ConvertToJsonArrayAndAdd(item);
							break;
					}
				}
				else if (TypedArray is List<double> doubleList) 
					doubleList.Add(Convert.ToDouble(item));
				else if (item.GetType() == typeof(double))
				{
					IList list = TypedArray as IList;
					doubleList = new List<double>(list.Count * 3 / 2);
					for (int i = 0; i < list.Count; i++) doubleList.Add(Convert.ToDouble(list[i]));
					doubleList.Add((double)item);
					TypedArray = doubleList;
				}
				else if (TypedArray is List<long> longList) 
					longList.Add(Convert.ToInt64(item));
				else if (item.GetType() == typeof(long))
				{
					IList list = TypedArray as IList;
					longList = new List<long>(list.Count * 3 / 2);
					for (int i = 0; i < list.Count; i++) longList.Add(Convert.ToInt64(list[i]));
					longList.Add((long)item);
					TypedArray = longList;
				}
				else if (TypedArray is List<int> intList && item.GetType() == typeof(int)) 
					intList.Add((int)item);
				else
					ConvertToJsonArrayAndAdd(item);

			}

			public void AddDateTimeToArray(DateTime item)
			{
				if (JsonArray != null) 
					JsonArray.Add(item);
				else if (TypedArray == null) 
					TypedArray = new List<DateTime>() { item };
				else if (TypedArray is List<DateTime> datetimeList) 
					datetimeList.Add(item);
				else 
					ConvertToJsonArrayAndAdd(item);
			}

			public void AddBoolToArray(bool item)
			{
				if (JsonArray != null) 
					JsonArray.Add(item);
				else if (TypedArray == null) 
					TypedArray = new List<bool>() { item };
				else if (TypedArray is List<bool> boolList) 
					boolList.Add(item);
				else 
					ConvertToJsonArrayAndAdd(item);
			}

			private void ConvertToJsonArrayAndAdd(object item)
			{
				int n;

				switch (TypedArray)
				{
					case List<string> stringList:
						n = stringList.Count;
						JsonArray = new List<object>(n * 3 / 2);
						for (int i = 0; i < n; i++)	JsonArray.Add(stringList[i]);
						break;

					case List<int> intList:
						n = intList.Count;
						JsonArray = new List<object>(n * 3 / 2);
						for (int i = 0; i < n; i++) JsonArray.Add(intList[i]);
						break;

					case List<double> doubleList:
						n = doubleList.Count;
						JsonArray = new List<object>(n * 3 / 2);
						for (int i = 0; i < n; i++) JsonArray.Add(doubleList[i]);
						break;

					case List<long> longList:
						n = longList.Count;
						JsonArray = new List<object>(n * 3 / 2);
						for (int i = 0; i < n; i++) JsonArray.Add(longList[i]);
						break;

					case List<DateTime> datetimeList:
						n = datetimeList.Count;
						JsonArray = new List<object>(n * 3 / 2);
						for (int i = 0; i < n; i++) JsonArray.Add(datetimeList[i]);
						break;

					default:
						JsonArray = new List<object>();
						break;
				}

				TypedArray = null;
				JsonArray.Add(item);
			}
		}


		public object Root { get; protected set; }

		protected StringBuilder reusableBuilder;
		protected Stack<ParsingContext> reusableContext;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool IsJsonWhitespace(char c)
		{
			return c == ' ' || c == '\t' || c == '\r' || c == '\n';
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool IsHexDigit(char c)
		{
			return (c >= '0' && c <= '9') || ((c = char.ToUpper(c)) >= 'A' && c <= 'F');
		}

		protected static bool IsValidObjectKey(string s, bool strict)
		{
			if (string.IsNullOrWhiteSpace(s))
				return false;

			if (strict)
			{
				if (!char.IsLetter(s[0]) && s[0] != '_' && s[0] != '$')
					return false;

				for (int i = 1; i < s.Length; i++)
					if (!char.IsLetterOrDigit(s[i]) && s[0] != '_' && s[0] != '$')
						return false;
			}

			return true;
		}

		protected static object ObjectForNumber(string text, int start, int count, NumberType numberType)
		{
			double doubleVal;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1 

			ReadOnlySpan<char> number = text.AsSpan(start, count);

#else

			string number = text.Substring(start, count);

#endif

			switch (numberType)
			{
				case NumberType.Integer:
					if (int.TryParse(number, out int intVal))
						return intVal;

					if (long.TryParse(number, out long longVal))
						return longVal;

					if (double.TryParse(number, NumberStyles.Float, new NumberFormatInfo(), out doubleVal))
						return doubleVal;

					return null;

				case NumberType.Decimal:
				case NumberType.Double:
					if (double.TryParse(number, NumberStyles.Float, new NumberFormatInfo(), out doubleVal))
						return doubleVal;

					return null;

				default:
					return null;
			}
		}

		protected static int SkipTextConstant(string text, int position, StringBuilder resultBuffer, out string result)
		{
			result = null;
			resultBuffer.Clear();


			int Hi = text.Length - 1;
			int i = position + 1;

			while (i <= Hi)
			{
				char c = text[i];

				switch (c)
				{
					case '\r':
					case '\n':
						//end of line and not closed constant => error
						return -1;

					case '"':
						// end of text constant
						result = resultBuffer.ToString();
						return i + 1;

					case '\\':
						// escaped character

						if (++i > Hi)
							return -1;

						switch (c = text[i])
						{
							case 'r':
								i++;
								resultBuffer.Append('\r');
								break;

							case 'n':
								i++;
								resultBuffer.Append('\n');
								break;

							case 't':
								i++;
								resultBuffer.Append('\t');
								break;

							case 'b':
								i++;
								resultBuffer.Append('\b');
								break;

							case 'f':
								i++;
								resultBuffer.Append('\f');
								break;

							case '\\':
							case '"':
							case '/':
								i++;
								resultBuffer.Append(c);
								break;

							case 'u':
								if (Hi - i + 1 < 4 ||
									!IsHexDigit(text[++i]) ||
									!IsHexDigit(text[++i]) ||
									!IsHexDigit(text[++i]) ||
									!IsHexDigit(text[++i])) return -1;

								i++;

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1
								resultBuffer.Append((char)ushort.Parse(text.AsSpan(i - 4, 4), NumberStyles.HexNumber));
#else
								resultBuffer.Append((char)ushort.Parse(text.Substring(i - 4, 4), NumberStyles.HexNumber));
#endif

								break;

							default:
								return -1;
						}

						break;

					default:
						i++;
						resultBuffer.Append(c);
						break;
				}
			}

			return -1;
		}

		protected static int SkipNumber(string text, int position, out NumberType numberType)
		{
			numberType = NumberType.Integer;

			int Hi = text.Length - 1;
			int i = position;

			if (i > Hi || i < 0)
				return -1;

			/* 
				number = [ minus ] int [ frac ] [ exp ]

				decimal-point = %x2E       ; .
				digit1-9 = %x31-39         ; 1-9
				e = %x65 / %x45            ; e E
				exp = e [ minus / plus ] 1*DIGIT
				frac = decimal-point 1*DIGIT
				int = zero / ( digit1-9 *DIGIT )
				minus = %x2D               ; -
				plus = %x2B                ; +
				zero = %x30                ; 0
			*/


			// minus part

			if (text[i] == '-')
			{
				if (++i > Hi)
					return -1;
			}

			char c;


			// int part

			if (text[i] != '0')
			{
				int j = i;

				while (i <= Hi && (c = text[i]) >= '0' && c <= '9') i++;

				if (i == j)
					return -1;
			}
			else i++;

			if (i > Hi)
				return i;

			c = text[i];


			// frac part

			if (c == '.')
			{
				int j = ++i;

				while (i <= Hi && (c = text[i]) >= '0' && c <= '9') i++;

				if (i == j)
					return -1;

				numberType = NumberType.Decimal;

				if (i > Hi)
					return i;

				c = text[i];
			}


			// exp part

			if (char.ToUpper(c) == 'E')
			{
				int k = i;

				if (++i > Hi)
					return k;

				c = text[i];

				bool hasSign = c == '+' || c == '-';

				if (hasSign && ++i > Hi)
					return -1;

				int j = i;

				while (i <= Hi && (c = text[i]) >= '0' && c <= '9') i++;

				if (i == j)
					if (hasSign)
						return -1;
					else
					{
						numberType = NumberType.Decimal;
						return k;
					}
				else numberType = NumberType.Double;
			}

			return i;
		}

		protected static bool TryGetDateTime(string text, out DateTime dt)
		{
			if (text.Length == "0000-00-00T00:00:00.000Z".Length &&
				char.IsNumber(text[0]) && text[4] == '-' && text[10] == 'T' && text[23] == 'Z' &&
				DateTime.TryParse(text, out dt))
				return true;

			if (text.Length >= "0000-00-00T00:00:00".Length &&
				char.IsNumber(text[0]) && text[4] == '-' && text[10] == 'T' && text[13] == ':' &&
				DateTime.TryParse(text, out dt))
				return true;

			if (text.Length > 8 &&
				text[0] == '/' &&
				text.StartsWith("/Date(") && 
				text.EndsWith(")/"))
			{

#if NETCOREAPP2_1_OR_GREATER || NETSTANDARD2_1

				if (long.TryParse(text.AsSpan("/Date(".Length, text.Length - "/Date()/".Length), out long msec))
				{
					double ms = msec;

					if (msec >= 0)
						if (DateTime.MaxValue.Subtract(DateTime.UnixEpoch).TotalMilliseconds < ms)
							dt = DateTime.UnixEpoch.AddMilliseconds(ms);
						else
							dt = DateTime.MaxValue;
					else if (DateTime.UnixEpoch.Subtract(DateTime.MinValue).TotalMilliseconds < -ms)
						dt = DateTime.UnixEpoch.AddMilliseconds(ms);
					else
						dt = DateTime.MinValue;

					return true;
				}

#else

				if (long.TryParse(text.Substring("/Date(".Length, text.Length - "/Date()/".Length), out long msec))
				{
					double ms = msec;
					DateTime unixEpoch = new DateTime(1970, 1, 1);

					if (msec >= 0)
						if (DateTime.MaxValue.Subtract(unixEpoch).TotalMilliseconds < ms)
							dt = unixEpoch.AddMilliseconds(ms);
						else
							dt = DateTime.MaxValue;
					else if (unixEpoch.Subtract(DateTime.MinValue).TotalMilliseconds < -ms)
						dt = unixEpoch.AddMilliseconds(ms);
					else
						dt = DateTime.MinValue;

					return true;
				}

#endif
			}

			dt = DateTime.MinValue;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool TryPeek(Stack<ParsingContext> parsingContext, out ParsingContext context)
		{
#if NETCOREAPP2_1 || NETCOREAPP2_2 || NETCOREAPP3_0 || NETCOREAPP3_1 || NET5_0 || NET6_0 || NETSTANDARD2_1
			return parsingContext.TryPeek(out context);
#else
			if (parsingContext.Any())
			{
				context = parsingContext.Peek();
				return true;
			}

			context = null;
			return false;
#endif
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static object WithError(int code, string text, int index, out ParseError error)
		{
			error = new ParseError(code, text, index);
			return null;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static bool NeedEscape(string src, int i)
		{
			char c = src[i];

			return c < 32 || c == '"' || c == '\\'
				// Broken lead surrogate
				|| (c >= '\uD800' && c <= '\uDBFF' &&
					(i == src.Length - 1 || src[i + 1] < '\uDC00' || src[i + 1] > '\uDFFF'))
				// Broken tail surrogate
				|| (c >= '\uDC00' && c <= '\uDFFF' &&
					(i == 0 || src[i - 1] < '\uD800' || src[i - 1] > '\uDBFF'))
				// To produce valid JavaScript
				|| c == '\u2028' || c == '\u2029'
				// Escape "</" for <script> tags
				|| (c == '/' && i > 0 && src[i - 1] == '<');
		}

		public static string EscapeString(string src)
		{
			if (string.IsNullOrEmpty(src))
				return string.Empty;

			StringBuilder sb = new StringBuilder(src.Length);
			int start = 0;

			for (int i = 0; i < src.Length; i++)
				if (NeedEscape(src, i))
				{
					sb.Append(src, start, i - start);

					switch (src[i])
					{
						case '\b': 
							sb.Append("\\b"); 
							break;
						case '\f':
							sb.Append("\\f"); 
							break;
						case '\n': 
							sb.Append("\\n"); 
							break;
						case '\r': 
							sb.Append("\\r"); 
							break;
						case '\t': 
							sb.Append("\\t"); 
							break;
						case '\"': 
							sb.Append("\\\""); 
							break;
						case '\\': 
							sb.Append("\\\\"); 
							break;
						case '/': 
							sb.Append("\\/"); 
							break;
						default:
							sb.Append("\\u");
							sb.Append(((int)src[i]).ToString("x04"));
							break;
					}

					start = i + 1;
				}

			sb.Append(src, start, src.Length - start);
			return sb.ToString();
		}

		public static string Stringify(string src)
		{
			if (src == null)
				return "null";

			return string.Format("\"{0}\"", EscapeString(src));
		}

		protected JsonDoc(object root) 
		{
			Root = root;
		}

		public JsonDoc()
		{
			reusableBuilder = new StringBuilder(256);
			reusableContext = new Stack<ParsingContext>();
		}


		public object Parse(string text, ParsingSettings settings, out ParseError error)
		{
			if (reusableBuilder == null)
				reusableBuilder = new StringBuilder(256);

			if (reusableContext == null)
				reusableContext = new Stack<ParsingContext>();

			return Parse(text, settings, reusableContext, reusableBuilder, out error);
		}

		protected static object Parse(string text, ParsingSettings settings, Stack<ParsingContext> parsingContext, StringBuilder resultBuffer, out ParseError error)
		{
			if (text == null)
				return WithError(ParseError.ARGUMENT_NULL, null, 0, out error);

			bool strictPropertyNames = true;
			bool allowComments = true;
			bool recognizeDateTime = false;
			bool forceDoubleInArrays = false;
			Func<IEnumerable<string>, object> objectFactory = null;
			Func<string, object, object, int> propertySetter = null;

			if (settings != null)
			{
				strictPropertyNames = settings.StrictPropertyNames;
				allowComments = settings.AllowComments;
				recognizeDateTime = settings.RecognizeDateTime;
				forceDoubleInArrays = settings.ForceDoubleInArrays;
				objectFactory = settings.ObjectFactory;
				propertySetter = settings.PropertySetter;
			}

			parsingContext.Clear();
			resultBuffer.Clear();

			object root = null;
			int Hi = text.Length - 1;
			int i = 0;
			char c;


			while (i <= Hi)
			{
				c = text[i];

				if (IsJsonWhitespace(c))
				{
					while (++i <= Hi && IsJsonWhitespace(text[i])) ;
				}
				else if (c == '"')
				{
					int j = SkipTextConstant(text, i, resultBuffer, out string val);

					if (j < 0)
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					if (root == null)
					{
						if (recognizeDateTime && TryGetDateTime(val, out DateTime dt)) root = dt;
						else root = val;
					}
					else if (!TryPeek(parsingContext, out ParsingContext ctx))
					{
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}
					else if (ctx.IsArray)
					{
						if (ctx.LastProcessed == LastProcessedElement.ArrayStart ||
							ctx.LastProcessed == LastProcessedElement.ListSeparator)
						{
							if (recognizeDateTime && TryGetDateTime(val, out DateTime dt)) ctx.AddDateTimeToArray(dt);
							else ctx.AddStringToArray(val);

							ctx.LastProcessed = LastProcessedElement.TextConstant;
						}
						else
						{
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
						}
					}
					else
					{
						if (ctx.LastProcessed == LastProcessedElement.ObjectKey ||
							(ctx.LastProcessed & LastProcessedElement.Value) != 0)
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);


						if (ctx.LastProcessed == LastProcessedElement.ObjectStart ||
							ctx.LastProcessed == LastProcessedElement.ListSeparator)
						{
							if (!IsValidObjectKey(val, strictPropertyNames))
								return WithError(ParseError.INVALID_KEY, text, i, out error);

							if (ctx.GetObject() is IDictionary<string, object> dict &&
								dict.ContainsKey(val))
								return WithError(ParseError.KEY_NOT_UNIQUE, text, i, out error);

							ctx.PendingObjectKey = val;
							ctx.LastProcessed = LastProcessedElement.ObjectKey;
						}
						else
						{
							if (recognizeDateTime && TryGetDateTime(val, out DateTime dt))
							{
								if (ctx.SetObjectProperty(ctx.PendingObjectKey, dt, propertySetter) < 0)
									return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);
							}
							else if (ctx.SetObjectProperty(ctx.PendingObjectKey, val, propertySetter) < 0)
								return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);

							ctx.LastProcessed = LastProcessedElement.TextConstant;
						}
					}

					i = j;
				}
				else if (c == ',')
				{
					if (!TryPeek(parsingContext, out ParsingContext ctx) ||
						(ctx.LastProcessed & LastProcessedElement.Value) == 0)
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					ctx.LastProcessed = LastProcessedElement.ListSeparator;
					i++;
				}
				else if (c == ':')
				{
					if (!TryPeek(parsingContext, out ParsingContext ctx) ||
						ctx.LastProcessed != LastProcessedElement.ObjectKey)
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					ctx.LastProcessed = LastProcessedElement.KeyValueSep;
					i++;
				}
				else if (c == '{')
				{
					if (TryPeek(parsingContext, out ParsingContext ctx) &&
						ctx.LastProcessed != LastProcessedElement.KeyValueSep &&
						ctx.LastProcessed != LastProcessedElement.ArrayStart &&
						(ctx.LastProcessed != LastProcessedElement.ListSeparator || ctx.IsObject))
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					parsingContext.Push(ctx = new ParsingContext(objectFactory?.Invoke(parsingContext.Select(ct => ct.PathNode)) ?? new JsonObject()) { LastProcessed = LastProcessedElement.ObjectStart });

					if (root == null)
						root = ctx.GetObject();

					i++;
				}
				else if (c == '}')
				{
					if (!TryPeek(parsingContext, out ParsingContext ctx) ||
						!ctx.IsObject ||
						(ctx.LastProcessed != LastProcessedElement.ObjectStart && (ctx.LastProcessed & LastProcessedElement.Value) == 0))
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					object obj = ctx.GetObject();

					parsingContext.Pop();

					if (TryPeek(parsingContext, out ctx))
					{
						if (ctx.IsArray)
							ctx.AddObjectToArray(obj);
						else if (ctx.SetObjectProperty(ctx.PendingObjectKey, obj, propertySetter) < 0)
							return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);

						ctx.LastProcessed = LastProcessedElement.ObjectEnd;
					}

					i++;
				}
				else if (c == '[')
				{
					if (TryPeek(parsingContext, out ParsingContext ctx) &&
						ctx.LastProcessed != LastProcessedElement.KeyValueSep &&
						ctx.LastProcessed != LastProcessedElement.ArrayStart &&
						(ctx.LastProcessed != LastProcessedElement.ListSeparator || ctx.IsObject))
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					/*
					Type forceArrayItemsType = null;

					if (ctx != null &&
						ctx.IsTypedObject)
					{
						var property = ctx.GetObject()
							.GetType()
							.GetProperty(ctx.PendingObjectKey, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

						Type listInterface, genericType;

						if (property != null &&
							(listInterface = property.PropertyType.GetInterface("IReadOnlyList`1")) != null &&
							((genericType = listInterface.GenericTypeArguments.FirstOrDefault()) == typeof(double) || genericType == typeof(long) || genericType == typeof(int)))
						{
							forceArrayItemsType = genericType;
						}
					}
					*/

					parsingContext.Push(ctx = new ParsingContext(null) { LastProcessed = LastProcessedElement.ArrayStart });

					i++;
				}
				else if (c == ']')
				{
					if (!TryPeek(parsingContext, out ParsingContext ctx) ||
						!ctx.IsArray ||
						(ctx.LastProcessed != LastProcessedElement.ArrayStart && (ctx.LastProcessed & LastProcessedElement.Value) == 0))
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					object array = ctx.GetArray();

					parsingContext.Pop();

					if (TryPeek(parsingContext, out ctx))
					{
						if (ctx.IsArray)
							ctx.AddObjectToArray(array);
						else if (ctx.SetObjectProperty(ctx.PendingObjectKey, array, propertySetter) < 0)
							return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);

						ctx.LastProcessed = LastProcessedElement.ArrayEnd;
					}
					else
					{
						root = array;
					}

					i++;
				}
				else if (c == '-' || (c >= '0' && c <= '9'))
				{
					int j = SkipNumber(text, i, out NumberType numberType);

					if (j < 0)
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);


					if (root == null)
					{
						root = ObjectForNumber(text, i, j - i, numberType);

						if (root == null)
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}
					else if (!TryPeek(parsingContext, out ParsingContext ctx))
					{
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}
					else if (ctx.IsArray)
					{
						object number;

						if ((ctx.LastProcessed == LastProcessedElement.ArrayStart || ctx.LastProcessed == LastProcessedElement.ListSeparator) &&
							(number = ObjectForNumber(text, i, j - i, forceDoubleInArrays ? NumberType.Double : numberType)) != null)
						{
							ctx.AddNumberToArray(number);
							ctx.LastProcessed = LastProcessedElement.Number;
						}
						else
						{
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
						}
					}
					else
					{
						object number;

						if (ctx.LastProcessed != LastProcessedElement.KeyValueSep ||
							(number = ObjectForNumber(text, i, j - i, numberType)) == null)
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

						if (ctx.SetObjectProperty(ctx.PendingObjectKey, number, propertySetter) < 0)
							return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);
						
						ctx.LastProcessed = LastProcessedElement.Number;
					}

					i = j;
				}
				else if (c == 'n')
				{
					if (string.Compare(text, i, "null", 0, 4, StringComparison.InvariantCulture) != 0)
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					if (root == null)
					{
						root = null;
					}
					else if (!TryPeek(parsingContext, out ParsingContext ctx))
					{
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}
					else if (ctx.IsArray)
					{
						if (ctx.LastProcessed == LastProcessedElement.ArrayStart ||
							ctx.LastProcessed == LastProcessedElement.ListSeparator)
						{
							ctx.AddObjectToArray(null);
							ctx.LastProcessed = LastProcessedElement.Null;
						}
						else
						{
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
						}
					}
					else
					{
						if (ctx.LastProcessed != LastProcessedElement.KeyValueSep)
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

						if (ctx.SetObjectProperty(ctx.PendingObjectKey, null, propertySetter) < 0)
							return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);

						ctx.LastProcessed = LastProcessedElement.Null;
					}

					i += 4;
				}
				else if (c == 'f' || c == 't')
				{
					bool val;

					if (string.Compare(text, i, "false", 0, 5, StringComparison.InvariantCulture) == 0)
					{
						val = false;
						i += 5;
					}
					else if (string.Compare(text, i, "true", 0, 4, StringComparison.InvariantCulture) == 0)
					{
						val = true;
						i += 4;
					}
					else
					{
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}

					if (root == null)
					{
						root = val;
					}
					else if (!TryPeek(parsingContext, out ParsingContext ctx))
					{
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
					}
					else if (ctx.IsArray)
					{
						if (ctx.LastProcessed == LastProcessedElement.ArrayStart ||
							ctx.LastProcessed == LastProcessedElement.ListSeparator)
						{
							ctx.AddBoolToArray(val);
							ctx.LastProcessed = LastProcessedElement.BoolConstant;
						}
						else
						{
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
						}
					}
					else
					{
						if (ctx.LastProcessed != LastProcessedElement.KeyValueSep)
							return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

						if (ctx.SetObjectProperty(ctx.PendingObjectKey, val, propertySetter) < 0)
							return WithError(ParseError.INCOMPATIBLE_TYPE, text, i, out error);

						ctx.LastProcessed = LastProcessedElement.BoolConstant;
					}
				}
				else if (c == '/')
				{
					if (!allowComments || i == Hi || ((c = text[++i]) != '/' && c != '*'))
						return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);

					if (c == '/')
					{
						do
						{
							i++;
						} while (i <= Hi && text[i] != '\n');
					}
					else
					{
						do
						{
							i++;
						} while (i < Hi && (text[i] != '*' || text[i + 1] != '/'));

						if (i < Hi)
							i += 2;
						else
							i++;
					}
				}
				else
				{
					return WithError(ParseError.UNEXPECTED_TOKEN, text, i, out error);
				}
			}


			if (parsingContext.Count != 0)
				return WithError(ParseError.UNEXPECTED_END, text, i, out error);

			error = null;

			return root;
		}
		
		public static JsonDoc Parse(string text, ParsingSettings settings = null)
		{
			if (text == null)
				throw new ArgumentNullException(nameof(text));

			object root = Parse(text, settings, new Stack<ParsingContext>(), new StringBuilder(256), out ParseError error);

			if (error != null)
				error.ThrowException();

			return new JsonDoc(root);
		}

	}
}
