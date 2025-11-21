using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text.Json;


namespace HCms.Content.ViewModels
{

	public class DynamicProperties : DynamicObject, IReadOnlyDictionary<string, object>
	{
		private readonly IReadOnlyDictionary<string, object> _dictionary;

		public DynamicProperties()
		{
			_dictionary = new Dictionary<string, object>();
		}

		public DynamicProperties(IReadOnlyDictionary<string, object> dictionary)
		{
			_dictionary = dictionary ?? new Dictionary<string, object>();
		}

		private bool TryGetMember(string name, out object result)
		{
			if (!_dictionary.TryGetValue(name, out object value))
			{
				if (!name.Contains('_'))
				{
					result = null;
					return false;
				}

				name = name.Replace('_', '-');

				if (!_dictionary.TryGetValue(name, out value))
				{
					result = null;
					return false;
				}
			}

			if (value is IList<object> list)
			{
				// ! Limitation - "array of arrays" is not supported.
				// if any member of the 'list' is also the array having 'DynamicProperties' item, then this method eventually returns wrong result.

				object[] res = new object[list.Count];

				for (int i = 0; i < list.Count; i++)
				{
					if (list[i] is Dictionary<string, object> dict)
						res[i] = new DynamicProperties(dict);
					else if (list[i] is Dictionary<object, object> odict)
						res[i] = new DynamicProperties(odict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value));
					else
						res[i] = list[i];
				}

				result = res;
			}
			else if (value is Dictionary<string, object> dict)
				result = new DynamicProperties(dict);
			else if (value is Dictionary<object, object> odict)
				result = new DynamicProperties(odict.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value));
			else if (value is JsonElement jelem)
				result= JsonElementToObject(jelem);
			else
				result = value;

			return true;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			this.TryGetMember(binder.Name, out result);

			return true;
		}

		private static object JsonElementToObject(JsonElement jsonElement)
		{
			object result;

			switch (jsonElement.ValueKind)
			{
				case JsonValueKind.Object:
					var jdict = new Dictionary<string, object>();

					foreach (var prop in jsonElement.EnumerateObject())
						jdict[prop.Name] = prop.Value;

					result = new DynamicProperties(jdict);
					break;

				case JsonValueKind.Array:
					var res = new object[jsonElement.GetArrayLength()];
					int i = 0;

					foreach (var prop in jsonElement.EnumerateArray())
						res[i++] = JsonElementToObject(prop);

					result = res;
					break;

				case JsonValueKind.String:
					result = jsonElement.GetString();
					break;

				case JsonValueKind.Number:
					if (jsonElement.TryGetInt32(out int k))
						result = k;
					else if (jsonElement.TryGetInt64(out long l))
						result = l;
					else if (jsonElement.TryGetDouble(out double x))
						result = x;
					else
						result = 0;

					break;

				case JsonValueKind.True:
					result = true;
					break;

				case JsonValueKind.False:
					result = false;
					break;

				default:
					result = null;
					break;
			}

			return result;
		}

		public IReadOnlyDictionary<string, object> ToDictionary()
		{
			return _dictionary;
		}


		// IReadOnlyDictionary<string, object> implementation

		IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _dictionary.Keys;

		IEnumerable<object> IReadOnlyDictionary<string, object>.Values
		{
			get
			{
				var keys = _dictionary.Keys;
				var result = new List<object>();

				foreach (var key in keys)
					if (this.TryGetMember(key, out object value))
						result.Add(value);
					else
						result.Add(null);

				return result;
			}
		}

		int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _dictionary?.Count ?? 0;

		object IReadOnlyDictionary<string, object>.this[string key]
		{
			get
			{
				bool result = this.TryGetMember(key, out object value);

				if (result)
					return value;

				throw new KeyNotFoundException($"Key '{key}' not found.");
			}
		}

		bool IReadOnlyDictionary<string, object>.ContainsKey(string key) => _dictionary.ContainsKey(key);

		bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => this.TryGetMember(key, out value);

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator() => _dictionary.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
		}
	}

}