using System;

namespace HCms.Infrastructure.Data
{
	public interface IDbIndexConflictDetector
	{
		bool ConflictDetected(Exception ex);
	}
}