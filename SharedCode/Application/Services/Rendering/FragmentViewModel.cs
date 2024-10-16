using System;

using AleProjects.Cms.Application.Dto;
using AleProjects.Cms.Domain.Entities;
using AleProjects.Cms.Domain.ValueObjects;


namespace AleProjects.Cms.Application.Services
{

	public class FragmentModel
	{
		public dynamic F { get; set; }
		public Document Document { get; set; }
		public Fragment Fragment { get; set; }
		public FragmentModel[] Children { get; set; }

	}


}