using Microsoft.EntityFrameworkCore;
using NSZUNews.Entities;

namespace NSZUNews.Services
{
    public class ArticlesContext : DbContext
    {
        public ArticlesContext(DbContextOptions<ArticlesContext> options)
            : base(options)
        {
            
        }

        
        public DbSet<ArticleBase> Articles { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        { 
            modelBuilder.Entity<ArticleBase>();
        }
    }
}