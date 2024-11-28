using System;
using System.Collections.Generic;
using System.Linq;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Sdk.ViewModels
{
	public struct Anchor
	{
		public string Id { get; set; }
		public string Name { get; set; }
		public int Level { get; set; }
	}
}