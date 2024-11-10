using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace AleProjects.Cms.Domain.ValueObjects
{

	public class DynamicXml : DynamicObject
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

		public override bool TryGetMember(GetMemberBinder binder, out object result)
		{
			string bName = binder.Name;
			string name = string.IsNullOrEmpty(_ns) ? bName : string.Format("{{{0}}}{1}", _ns, bName);

			var attr = _root.Attribute(name);

			if (attr != null)
			{
				result = attr.Value;
				return true;
			}

			var nodes = _root.Elements(name);
			var n = nodes.Count();

			if (n == 0 && bName.Contains('_'))
			{
				bName = bName.Replace('_', '-');
				name = string.IsNullOrEmpty(_ns) ? bName : string.Format("{{{0}}}{1}", _ns, bName);
				nodes = _root.Elements(name);
				n = nodes.Count();
			}

			if (n == 0)
			{
				result = null;
				return true;
			}

			XSElement newXse = null;
			int mtype = -1;
			bool isArray = false;

			if (_xse != null)
			{
				for (int i = 0; i < _xse.Elements.Count; i++)
					if (_xse.Elements[i].Name == bName)
					{
						newXse = _xse.Elements[i];
						isArray = newXse.MaxOccurs > 1;

						if (newXse.IsSimple)
							mtype = newXse.XmlType switch
							{
								"int" or "integer" or "short" or "byte" => 1,
								"double" or "decimal" or "float" => 2,
								"boolean" or "bool" => 3,
								_ => 0,
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
				return true;
			}

			object res = Convert(node.Value, mtype);

			result = isArray ? new object[] { res } : res;

			return true;
		}

		public override string ToString()
		{
			return _root.Value;
		}
	}


}