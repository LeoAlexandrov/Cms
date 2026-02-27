using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using HCms.Content.ViewModels;


namespace HCms.Content.Repo
{

	/// <summary>
	/// Represents CMS content repository.
	/// </summary>
	public interface IContentRepo
	{
		/// <summary>
		/// Resets or forces to reset inner repository data.
		/// </summary>
		void Reset();

		/// <summary>
		/// Asynchronously returns a view model of the document with the specified logical path.
		/// </summary>
		/// <param name="root">Slug of the root document.</param>
		/// <param name="path">Logical path of the document.</param>
		/// <param name="childrenFromPos">The starting position of the document child to start selection from. Used for paginated children output. When negative no children are selected.</param>
		/// <param name="siblings">Determine whether to include or not sibling documents.</param>
		/// <param name="allowedStatus">Array of allowed publication statuses. If null only published documents are retrived.</param>
		/// <param name="exactPathMatch">False value instructs the method to search a closest matching document if nothing is found by exact path.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the view model or null if no document found..</returns>
		Task<Document> GetDocument(string root, string path, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus, bool exactPathMatch);

		/// <summary>
		/// Asynchronously returns a view model of the document with the specified id.
		/// </summary>
		/// <param name="id">Document id</param>
		/// <param name="childrenFromPos">Position of the document child to start selection from. Used for paginated children output. When negative no children are selected.</param>
		/// <param name="siblings">Determine whether to include or not sibling documents.</param>
		/// <param name="allowedStatus">Array of allowed publication statuses. If null only published documents are retrived.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the view model or null if no document found..</returns>
		Task<Document> GetDocument(int id, int childrenFromPos, int takeChildren, bool siblings, int[] allowedStatus);

		/// <summary>
		/// Asynchronously returns a role of CMS user with specified login or null if no user found.
		/// </summary>
		/// <param name="login">User login</param>
		/// <returns>User role or null.</returns>
		ValueTask<string> UserRole(string login);
	}

}