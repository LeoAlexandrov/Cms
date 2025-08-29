using System;

using HCms.ViewModels;
using DemoSite.Services;


namespace DemoSite.ViewModels
{
	public class CmsPageModel
	{
		readonly CmsContentService _content;

		public Document Document { get; set; }
		public string Language { get => string.IsNullOrEmpty(Document?.Language) ? "en-US" : Document.Language; }
		public bool IsAuthenticated { get; protected set; }
		public MainMenu MainMenu { get; set; }
		public NavigationMenu NavigationMenu { get; set; }
		public Footer Footer { get; set; }


		public CmsPageModel(CmsContentService content, bool isAuthenticated)
		{
			_content = content;
			Document = _content.RequestedDocument;
			IsAuthenticated = isAuthenticated;

			if (Document != null)
			{
				InitializeMenus();
				InitializeFooter();
			}
			else
			{
				MainMenu = new MainMenu() { Languages = [], Commands = [] };
				NavigationMenu = new NavigationMenu() { Commands = [] };
				Footer = new Footer() { Links = [] };
			}

			IsAuthenticated = isAuthenticated;
		}

		void InitializeMenus()
		{
			if (Document.Attributes.TryGetValue("main-menu", out string menu))
			{
				MainMenu = System.Text.Json.JsonSerializer.Deserialize<MainMenu>(menu);

				string root = Document.Root?.Slug ?? Document.Slug;
				string currentLink = _content.Repo.PathTransformer.Forward(root, Document.Path, false);

				for (int i = 0; i < MainMenu.Commands.Length; i++)
				{
					if (MainMenu.Commands[i].Link == currentLink)
						MainMenu.Commands[i].Inactive = true;

					if (MainMenu.Commands[i].Submenu != null)
						for (int j = 0; j < MainMenu.Commands[i].Submenu.Length; j++)
							if (MainMenu.Commands[i].Submenu[j].Link == currentLink)
								MainMenu.Commands[i].Submenu[j].Inactive = true;

				}
			}
			else
			{
				MainMenu = new MainMenu() { Languages = [], Commands = [] };
			}


			var navMenu = new NavigationMenu() { Title = Document.Parent?.Title };

			if (Document.Siblings.Length != 0)
			{
				string root = Document.Root?.Slug ?? Document.Slug;
				int n = Document.Siblings.Length;
				int m;

				var commands = new NavigationMenuItem[n];

				for (int i = 0; i < n; i++)
				{
					commands[i] = new NavigationMenuItem()
					{
						Label = Document.Siblings[i].Title,
						Link = _content.Repo.PathTransformer.Forward(root, Document.Siblings[i].Path, false)
					};

					if (Document.Id == Document.Siblings[i].Id)
					{
						commands[i].Inactive = true;

						if (Document.Children != null && (m = Document.Children.Length) != 0)
						{
							commands[i].Submenu = new NavigationMenuItem[m];

							for (int j = 0; j < m; j++)
								commands[i].Submenu[j] = new NavigationMenuItem()
								{
									Label = Document.Children[j].Title,
									Link = _content.Repo.PathTransformer.Forward(root, Document.Children[j].Path, false)
								};
						}
					}
				}

				navMenu.Title = Document.Parent?.Title;
				navMenu.Commands = commands;
			}
			else
			{
				navMenu.Commands = [];
			}

			NavigationMenu = navMenu;
		}

		void InitializeFooter()
		{
			if (Document.Attributes.TryGetValue("footer", out string footer))
				Footer = System.Text.Json.JsonSerializer.Deserialize<Footer>(footer);
			else
				Footer = new Footer() { Links = [] };
		}

	}
}
