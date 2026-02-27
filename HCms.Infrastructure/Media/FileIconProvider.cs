using System;

namespace HCms.Infrastructure.Media
{ 

	public interface IFileIconProvider
	{
		bool TryGet(string filetype, int size, out byte[] bytes);
		byte[] Default(int size);
	}
}