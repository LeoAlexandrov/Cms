using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AleProjects.Cms.Domain.ValueObjects
{

	public class DynamicXml : DynamicObject, IReadOnlyDictionary<string, object>
	{
		private readonly string _ns;
		private readonly XSElement _xse;
		private readonly XElement _root;
		private readonly IFormatProvider _fmt;

		private DynamicXml(XElement root, IList<XSElement> xs)
		{
			_root = root;
			_fmt = new NumberFormatInfo();

			XAttribute attr;

			if (root != null && (attr = _root.Attribute("xmlns")) != null)
			{
				_ns = attr.Value;

				string name = _root.Name.LocalName;
				_xse = xs.FirstOrDefault(e => e.Name == name && e.Namespace == _ns);
			}
		}

		private DynamicXml(XElement root, string ns, IFormatProvider fmt, XSElement xse)
		{
			_root = root;
			_ns = ns;
			_fmt = fmt;
			_xse = xse;
		}

		public static DynamicXml Parse(string xmlString, IList<XSElement> xs)
		{
			return new DynamicXml(XDocument.Parse(xmlString).Root, xs);
		}

		private object Convert(string val, int type)
		{
			object result;

			switch (type)
			{
				case 1:
					if (int.TryParse(val, out int iRes))
						result = iRes;
					else
						result = val;
					break;

				case 2:
					if (double.TryParse(val, _fmt, out double xRes))
						result = xRes;
					else
						result = val;
					break;

				case 3:
					result = string.Compare(val, "true", true) == 0;
					break;

				default:
					result = val;
					break;
			}

			return result;
		}


		private string[] GetKeys()
		{
			if (_root == null)
				return [];

			var all = _root.Elements();

			string[] result;

			if (string.IsNullOrEmpty(_ns))
			{
				result = all.Select(e => e.Name.ToString())
					.Distinct()
					.ToArray();
			}
			else
			{
				string prefix = $"{{{_ns}}}";

				result = all.Select(e => e.Name.ToString())
					.Where(name => name.StartsWith(prefix))
					.Select(name => name[prefix.Length..])
					.Distinct()
					.ToArray();
			}

			return result;
		}

		public bool TryGetMember(string memberName, out object result)
		{
			string name = string.IsNullOrEmpty(_ns) ? memberName : $"{{{_ns}}}{memberName}";
			var nodes = _root.Elements(name);
			var any = nodes.Any();

			if (!any && memberName.Contains('_'))
			{
				memberName = memberName.Replace('_', '-');
				name = string.IsNullOrEmpty(_ns) ? memberName : $"{{{_ns}}}{memberName}";
				nodes = _root.Elements(name);
				any = nodes.Any();
			}

			if (!any)
			{
				result = null;
				return false;
			}

			XSElement newXse = null;
			int mtype = -1;
			bool isArray = false;

			if (_xse != null)
			{
				for (int i = 0; i < _xse.Elements.Count; i++)
					if (_xse.Elements[i].Name == memberName)
					{
						newXse = _xse.Elements[i];
						isArray = newXse.MaxOccurs > 1;

						if (newXse.IsSimple)
							mtype = newXse.XmlType switch
							{
								"int" or "integer" or "short" or "byte" => 1,
								"double" or "decimal" or "float" => 2,
								"boolean" or "bool" => 3,
								_ => 0, // string
							};

						break;
					}
			}

			if (isArray)
			{
				result = nodes.Select(n => n.HasElements ? (object)new DynamicXml(n, _ns, _fmt, newXse) : Convert(n.Value, mtype)).ToArray();
				return true;
			}

			var node = nodes.FirstOrDefault();

			if (node.HasElements ||
				node.HasAttributes ||
				(node.FirstNode != null && node.FirstNode.NodeType == System.Xml.XmlNodeType.Comment))
			{
				result = new DynamicXml(node, _ns, _fmt, newXse);
			}
			else
			{
				result = Convert(node.Value, mtype);
			}

			return true;
		}

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			this.TryGetMember(binder.Name, out result);

			return true;
		}

		public override string ToString()
		{
			return _root?.Value;
		}


		// IReadOnlyDictionary<string, object> implementation

		IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => this.GetKeys();

		IEnumerable<object> IReadOnlyDictionary<string, object>.Values
		{
			get
			{
				var keys = this.GetKeys();
				var result = new List<object>(keys.Length);

				foreach (var key in keys)
					if (this.TryGetMember(key, out object value))
						result.Add(value);
					else
						result.Add(null);

				return result;
			}
		}

		int IReadOnlyCollection<KeyValuePair<string, object>>.Count => _root?.Elements()?.Count() ?? 0;

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

		bool IReadOnlyDictionary<string, object>.ContainsKey(string key)
		{
			string name = string.IsNullOrEmpty(_ns) ? key : $"{{{_ns}}}{key}";
			var nodes = _root.Elements(name);
			var result = nodes.Any();

			if (!result && key.Contains('_'))
			{
				key = key.Replace('_', '-');
				name = string.IsNullOrEmpty(_ns) ? key : $"{{{_ns}}}{key}";
				nodes = _root.Elements(name);
				result = nodes.Any();
			}

			return result;
		}

		bool IReadOnlyDictionary<string, object>.TryGetValue(string key, out object value) => this.TryGetMember(key, out value);

		IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
		{
			var keys = this.GetKeys();

			foreach (var key in keys)
				if (this.TryGetMember(key, out object value))
					yield return new KeyValuePair<string, object>(key, value);
				else
					yield return new KeyValuePair<string, object>(key, null);
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<KeyValuePair<string, object>>)this).GetEnumerator();
		}
	}

}