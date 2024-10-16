using System;


namespace AleProjects.Cms.Domain.ValueObjects
{
	public interface ITreeNode<T>
	{
		T Id { get; }
		T Parent { get; }
		string Title { get; }
		string Caption { get; }
		string Icon { get; }
		string Data { get; }
	}
}
