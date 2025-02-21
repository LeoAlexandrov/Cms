using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Entities = AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;

using HCms.Routing;
using HCms.ViewModels;


namespace HCms.ContentRepo
{

	/// <summary>
	/// Represents CMS content repository.
	/// </summary>
	public interface IContentRepo
	{
		/// <summary>
		/// Gets the path transformer. Assumed to be injected by DI container.
		/// </summary>
		IPathTransformer PathTransformer { get; }

		/// <summary>
		/// Reloads or forces to reload schemata depending on repository implementation.
		/// </summary>
		void ReloadSchemata();

		/// <summary>
		/// Asynchronously returns a view model of the document with the specified logical path.
		/// </summary>
		/// <param name="root">Slug of the root document.</param>
		/// <param name="path">Logical path of the document.</param>
		/// <param name="childrenFromPos">The starting position of the document child to start selection from. Used for paginated children output. When negative no children are selected.</param>
		/// <param name="siblings">Determine whether to include or not sibling documents.</param>
		/// <param name="exactPathMatch">False value instructs the method to search a closest matching document if nothing is found by exact path.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the view model or null if no document found..</returns>
		Task<Document> GetDocument(string root, string path, int childrenFromPos, bool siblings, bool exactPathMatch);

		/// <summary>
		/// Asynchronously returns a view model of the document with the specified id.
		/// </summary>
		/// <param name="id">Document id</param>
		/// <param name="childrenFromPos">Position of the document child to start selection from. Used for paginated children output. When negative no children are selected.</param>
		/// <param name="siblings">Determine whether to include or not sibling documents.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the view model or null if no document found..</returns>
		Task<Document> GetDocument(int id, int childrenFromPos, bool siblings);

		/// <summary>
		/// Asynchronously returns a view models of the document children.
		/// </summary>
		/// <param name="docId">Document id</param>
		/// <param name="childrenFromPos">The starting position of the document child to start selection from. Used for paginated children output. When negative no children are selected.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an array that contains view models of the document children.</returns>
		Task<Document[]> Children(int docId, int childrenFromPos);

		/// <summary>
		/// Returns a tuple with the logical path and root slug of the document with the specified id.
		/// </summary>
		/// <param name="docId">Document id</param>
		/// <returns>A tuple with the logical path and root document slug.</returns>
		Task<(string, string)> IdToPath(int docId);
	}



	/// <summary>
	/// Base class for CMS content repository implementations.
	/// </summary>
	public abstract partial class ContentRepo
	{
		protected IPathTransformer pathTransformer;
		protected int PageSize = 25;


		[GeneratedRegex("\\^\\(\\d+\\)")]
		protected static partial Regex RefRegex();

		[GeneratedRegex("\\^\\('[a-zA-Z0-9+/%]+'\\)")]
		protected static partial Regex MediaLinkRegex();


		internal struct Reference(string pattern, string docPath, string mediaLink, string root, IPathTransformer pathTransformer)
		{
			public string Pattern { get; set; } = pattern;
			public string Replacement { get; set; } = string.IsNullOrEmpty(mediaLink) ?
					pathTransformer.Forward(docPath, false, root) :
					pathTransformer.Forward(mediaLink, true, root);
		}


		/// <summary>
		/// Gets the path transformer. Assumed to be injected by DI container in inherited classes.
		/// </summary>
		public IPathTransformer PathTransformer { get => pathTransformer; }


		#region internal-functions

		class MatchComparer : IComparer<Match>
		{
			public int Compare(Match x, Match y)
			{
				return x.Index.CompareTo(y.Index);
			}
		}

		static void SetChildren(Fragment fr, Dictionary<int, Memory<Entities.FragmentLink>> tree, Func<Entities.FragmentLink, Fragment> fragmentFactory)
		{
			if (tree.TryGetValue(fr.LinkId, out Memory<Entities.FragmentLink> children))
			{
				Span<Entities.FragmentLink> span = children.Span;
				Fragment[] frChildren = new Fragment[span.Length];

				for (int i = 0; i < span.Length; i++)
					SetChildren(frChildren[i] = fragmentFactory(span[i]), tree, fragmentFactory);

				fr.Children = frChildren;
			}
		}

		protected static Fragment[] CreateFragmentsTree(Entities.FragmentLink[] links, Document doc, ILookup<int, Entities.FragmentAttribute> fragmentAttrs, IList<XSElement> xse)
		{
			Fragment fragmentFactory(Entities.FragmentLink link) => Fragment.Create(link, doc, fragmentAttrs[link.FragmentRef], xse);

			Dictionary<int, Memory<Entities.FragmentLink>> tree = [];

			if (links.Length > 0)
			{
				int key = links[0].Parent;
				int from = 0;

				for (int i = 0; i < links.Length; i++)
					if (!key.Equals(links[i].Parent))
					{
						tree.Add(key, links.AsMemory(from, i - from));
						from = i;
						key = links[i].Parent;
					}

				tree.Add(key, links.AsMemory(from, links.Length - from));
			}

			Fragment[] result;

			if (tree.TryGetValue(default, out Memory<Entities.FragmentLink> roots))
			{
				result = roots
					.ToArray()
					.Select(fl => Fragment.Create(fl, doc, fragmentAttrs[fl.FragmentRef], xse))
					.ToArray();

				foreach (var d in result)
					SetChildren(d, tree, fragmentFactory);
			}
			else
			{
				result = [];
			}

			return result;
		}

		protected static void FillAnchors(Entities.FragmentLink[] links, int linkIdx, List<Anchor> anchors, int level)
		{
			int cref = links[linkIdx].ContainerRef;

			do
			{
				if (links[linkIdx].Anchor)
				{
					anchors.Add(new Anchor() { Id = links[linkIdx].Fragment.XmlName + links[linkIdx].Id, Name = links[linkIdx].Fragment?.Name, Level = level });

					int i = Array.FindIndex(links, linkIdx, l => l.ContainerRef == links[linkIdx].Id && l.Anchor);

					if (i >= 0)
						FillAnchors(links, i, anchors, level + 1);
				}

				linkIdx++;
			} while (linkIdx < links.Length && links[linkIdx].ContainerRef == cref);
		}

		protected static string ReplaceRefs(string content, Dictionary<string, string> refs)
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

		#endregion


	}
}