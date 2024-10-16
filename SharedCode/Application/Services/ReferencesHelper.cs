using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using AleProjects.Cms.Domain.Entities;



namespace AleProjects.Cms.Application.Services
{

	public struct ExtractedRef(int id, string link) : IComparable<ExtractedRef>, IComparable<Reference>, IEquatable<ExtractedRef>
	{
		public int DocumentId { get; set; } = id;
		public string MediaLink { get; set; } = link;

		public readonly int CompareTo(ExtractedRef other)
		{
			int result = DocumentId.CompareTo(other.DocumentId);

			if (result != 0) 
				return result;

			if (ReferenceEquals(MediaLink, other.MediaLink))
				return 0;

			if (MediaLink is null)
				return -1;

			return MediaLink.CompareTo(other.MediaLink);
		}

		public readonly int CompareTo(Reference other)
		{
			int result = DocumentId.CompareTo(other.ReferenceTo);

			if (result != 0)
				return result;

			if (ReferenceEquals(MediaLink, other.MediaLink))
				return 0;

			if (MediaLink is null)
				return -1;

			return MediaLink.CompareTo(other.MediaLink);
		}

		public readonly bool Equals(ExtractedRef other)
		{
			if (!DocumentId.Equals(other.DocumentId))
				return false;

			if (ReferenceEquals(MediaLink, other.MediaLink))
				return true;

			if (MediaLink is null)
				return false;

			return MediaLink.Equals(other.MediaLink);
		}

		public override readonly bool Equals(object other)
		{
			return other is ExtractedRef @ref && this.Equals(@ref);
		}

		public override readonly int GetHashCode()
		{
			if (string.IsNullOrEmpty(MediaLink)) 
				return DocumentId.GetHashCode();

			return MediaLink.GetHashCode();
		}
	}

	class MatchComparer : IComparer<Match>
	{
		public int Compare(Match x, Match y)
		{
			return x.Index.CompareTo(y.Index);
		}
	}

	public static partial class ReferencesHelper
	{
		[GeneratedRegex("#\\(\\d+\\)")]
		private static partial Regex RefRegex();

		[GeneratedRegex("#\\('[a-zA-Z0-9+/%]+'\\)")]
		private static partial Regex MediaLinkRegex();


		public static string ToBase64(string text)
		{
			string result = Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

			return System.Web.HttpUtility.UrlEncode(result);
		}

		public static bool TryFromBase64(string base64, out string value)
		{
			bool result;

			if (string.IsNullOrEmpty(base64))
			{
				value = string.Empty;
				result = true;
			}
			else
			{
				try
				{
					byte[] data = Convert.FromBase64String(base64);
					value = Encoding.UTF8.GetString(data);
					result = true;
				}
				catch
				{
					value = null;
					result = false;
				}
			}

			return result;
		}

		public static ExtractedRef[] Extract(string content)
		{
			if (string.IsNullOrEmpty(content))
				return [];

			HashSet<ExtractedRef> refs = [];

			var re = RefRegex();
			var matches = re.Matches(content);

			for (int i = 0; i < matches.Count; i++)
				refs.Add(new(int.Parse(matches[i].Value[2..^1]), null));

			re = MediaLinkRegex();
			matches = re.Matches(content);

			for (int i = 0; i < matches.Count; i++)
				if (TryFromBase64(System.Web.HttpUtility.UrlDecode(matches[i].Value[3..^2]), out string path))
					refs.Add(new(0, path));

			if (refs.Count == 0)
				return [];

			var res = refs.ToArray();

			Array.Sort(res);

			return res;
		}

		public static ExtractedRef[] Extract(IReadOnlyList<string> content)
		{
			if (content == null) 
				return [];

			HashSet<ExtractedRef> refs = [];
			MatchCollection matches;

			var re = RefRegex();

			for (int i = 0; i < content.Count; i++)
				if (!string.IsNullOrEmpty(content[i]))
				{
					matches = re.Matches(content[i]);

					for (int j = 0; j < matches.Count; j++)
						refs.Add(new(int.Parse(matches[j].Value[2..^1]), null));
				}

			re = MediaLinkRegex();

			for (int i = 0; i < content.Count; i++)
				if (!string.IsNullOrEmpty(content[i]))
				{
					matches = re.Matches(content[i]);

					for (int j = 0; j < matches.Count; j++)
						if (TryFromBase64(System.Web.HttpUtility.UrlDecode(matches[j].Value[3..^2]), out string path))
							refs.Add(new(0, path));
				}

			if (refs.Count == 0)
				return [];

			var res = refs.ToArray();

			Array.Sort(res);

			return res;
		}

		public static void GetReferencesChanges(int id, List<Reference> existingRefs, ExtractedRef[] newRefs, out List<Reference> toAdd, out List<Reference> toRemove)
		{
			int n = newRefs.Length;

			if (n == 0)
			{
				toAdd = null;
				toRemove = existingRefs;
				return;
			}

			int m = existingRefs.Count;

			if (m == 0)
			{
				toAdd = newRefs.Select(r => new Reference { DocumentRef = id, ReferenceTo = r.DocumentId, MediaLink = r.MediaLink }).ToList();
				toRemove = null;
				return;
			}

			toAdd = [];
			toRemove = [];
			int i = 0;
			int j = 0;
			int cmpRes;

			while (i < n && j < m)
			{
				cmpRes = newRefs[i].CompareTo(existingRefs[j]);

				if (cmpRes < 0)
				{
					toAdd.Add(new Reference { DocumentRef = id, ReferenceTo = newRefs[i].DocumentId, MediaLink = newRefs[i].MediaLink });
					i++;
				}
				else if (cmpRes > 0)
				{
					toRemove.Add(existingRefs[j]);
					j++;
				}
				else
				{
					i++;
					j++;
				}
			}

			if (i < n)
				toAdd.AddRange(newRefs[i ..].Select(r => new Reference { DocumentRef = id, ReferenceTo = r.DocumentId, MediaLink = r.MediaLink }));

			if (j < m)
				toRemove.AddRange(existingRefs[j ..]);

		}

		public static string Replace(string content, IDictionary<string, string> refs)
		{
			if (string.IsNullOrEmpty(content))
				return string.Empty;

			var re1 = RefRegex();
			MatchCollection matchesD = re1.Matches(content);

			var re2 = MediaLinkRegex();
			MatchCollection matchesM = re2.Matches(content);

			if (matchesD.Count + matchesM.Count == 0)
				return content;

			var matches = new Match[matchesD.Count + matchesM.Count];

			matchesD.CopyTo(matches, 0);
			matchesM.CopyTo(matches, matchesD.Count);

			Array.Sort(matches, new MatchComparer());

			StringBuilder result = new();
			var span = content.AsSpan();
			int from = 0;

			foreach (Match m in matches)
			{
				result.Append(span[from..m.Index]);

				if (refs.TryGetValue(m.Value, out string link))
					result.Append(link);
				else 
					result.Append(m.Value);

				from = m.Index + m.Length;
			}

			if (from < content.Length)
				result.Append(span[from..content.Length]);

			return result.ToString();
		}
	}
}