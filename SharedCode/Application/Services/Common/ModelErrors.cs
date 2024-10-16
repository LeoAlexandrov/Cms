using System;
using System.Collections.Generic;


namespace AleProjects.Cms.Application.Services
{

	public class ModelErrors : Dictionary<string, ICollection<string>>
	{
		public static ModelErrors Create(string name, params string[] messages)
		{
			return new ModelErrors() { { name, new List<string>(messages) } };
		}

		public ModelErrors Add(string name, params string[] messages)
		{
			Add(name, new List<string>(messages));

			return this;
		}
	}
}