using Microsoft.EntityFrameworkCore;

using AleProjects.Cms.Domain.Entities;


namespace AleProjects.Cms.Infrastructure.Data
{
	public class CmsDbContext : DbContext
	{
		private static bool maybeNotCreated = true;

		public DbSet<Document> Documents { get; set; }
		public DbSet<DocumentPathNode> DocumentPathNodes { get; set; }
		public DbSet<DocumentAttribute> DocumentAttributes { get; set; }
		public DbSet<Reference> References { get; set; }
		public DbSet<FragmentLink> FragmentLinks { get; set; }
		public DbSet<Fragment> Fragments { get; set; }
		internal DbSet<Schema> Schemata { get; set; }
		internal DbSet<User> Users { get; set; }


		public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
		{
			if (maybeNotCreated)
			{
				Database.EnsureCreated();
				maybeNotCreated = false;
			}
		}

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<Document>()
				.HasIndex(d => new { d.Parent, d.Slug })
				.IsUnique();

			modelBuilder.Entity<Document>()
				.HasMany(d => d.DocumentPathNodes)
				.WithOne(t => t.Document)
				.HasForeignKey(t => t.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<Document>()
				.HasMany(d => d.References)
				.WithOne(t => t.Document)
				.HasForeignKey(t => t.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<Document>()
				.HasMany(d => d.DocumentAttributes)
				.WithOne(t => t.Document)
				.HasForeignKey(t => t.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<FragmentLink>()
				.HasOne(b => b.Document)
				.WithMany(d => d.FragmentLinks)
				.HasForeignKey(d => d.DocumentRef);

			modelBuilder.Entity<FragmentLink>()
				.HasOne(b => b.Fragment)
				.WithMany(d => d.DocumentLinks)
				.HasForeignKey(p => p.FragmentRef);

			modelBuilder.Entity<DocumentAttribute>()
				.HasIndex(d => new { d.DocumentRef, d.AttributeKey })
				.IsUnique();

			modelBuilder.Entity<User>()
				.HasIndex(s => s.Login)
				.IsUnique();

		}
	}
}
