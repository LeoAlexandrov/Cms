using System;


namespace HCms.Domain.Types
{
	public interface ITreeNode<T>
	{
		T Id { get; }
		T Parent { get; }
		string Title { get; }
		string Caption { get; }
		string Icon { get; }
		string Data { get; }
		bool Enabled { get; }
	}
}
