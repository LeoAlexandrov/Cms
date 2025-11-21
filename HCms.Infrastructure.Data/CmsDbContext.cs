using System;

using Microsoft.EntityFrameworkCore;

using HCms.Domain.Entities;


namespace HCms.Infrastructure.Data
{
	public class CmsDbContext : DbContext
	{
		static bool maybeNotCreated = true;

		readonly IDbIndexConflictDetector _indexConflictDetector;

		public DbSet<Document> Documents { get; set; }
		public DbSet<DocumentPathNode> DocumentPathNodes { get; set; }
		public DbSet<DocumentAttribute> DocumentAttributes { get; set; }
		public DbSet<Reference> References { get; set; }
		public DbSet<FragmentLink> FragmentLinks { get; set; }
		public DbSet<Fragment> Fragments { get; set; }
		public DbSet<FragmentAttribute> FragmentAttributes { get; set; }
		public DbSet<Schema> Schemata { get; set; }
		public DbSet<EventDestination> EventDestinations { get; set; }
		public DbSet<User> Users { get; set; }


		public CmsDbContext(DbContextOptions<CmsDbContext> options) : base(options)
		{
			_indexConflictDetector = null;

			if (maybeNotCreated)
			{
				Database.EnsureCreated();
				maybeNotCreated = false;
			}
		}

		public CmsDbContext(DbContextOptions<CmsDbContext> options, IDbIndexConflictDetector indexConflictDetector) : base(options)
		{
			_indexConflictDetector = indexConflictDetector;

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
				.HasIndex(d => d.PathHash);
			

			modelBuilder.Entity<Document>()
				.HasMany(d => d.DocumentPathNodes)
				.WithOne(n => n.Document)
				.HasForeignKey(n => n.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<Document>()
				.HasMany(d => d.References)
				.WithOne(r => r.Document)
				.HasForeignKey(r => r.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<Document>()
				.HasMany(d => d.DocumentAttributes)
				.WithOne(a => a.Document)
				.HasForeignKey(a => a.DocumentRef)
				.IsRequired();

			modelBuilder.Entity<Fragment>()
				.HasMany(d => d.FragmentAttributes)
				.WithOne(a => a.Fragment)
				.HasForeignKey(a => a.FragmentRef)
				.IsRequired();

			modelBuilder.Entity<FragmentLink>()
				.HasOne(d => d.Document)
				.WithMany(d => d.FragmentLinks)
				.HasForeignKey(l => l.DocumentRef);

			modelBuilder.Entity<FragmentLink>()
				.HasOne(l => l.Fragment)
				.WithMany(f => f.DocumentLinks)
				.HasForeignKey(l => l.FragmentRef);

			modelBuilder.Entity<DocumentAttribute>()
				.HasIndex(a => new { a.DocumentRef, a.AttributeKey })
				.IsUnique();

			modelBuilder.Entity<FragmentAttribute>()
				.HasIndex(a => new { a.FragmentRef, a.AttributeKey })
				.IsUnique();

			modelBuilder.Entity<User>()
				.HasIndex(u => u.Login)
				.IsUnique();

		}

		public bool IsConflict(Exception ex)
		{
			return _indexConflictDetector != null && _indexConflictDetector.ConflictDetected(ex);
		}
	}
}
