using NSZUNews.Entities;
using NSZUNews.Services;
using System.Linq;
using System.Threading.Tasks;

namespace NSZUNews.Controllers
{
    public class ArticleRepository
    {
        public ArticlesContext Context { get; } 

        public ArticleRepository(ArticlesContext articlesContext)
        {
            Context = articlesContext; 
        } 
        public async Task LoadAsync()
        {
            await Context.Database.EnsureCreatedAsync(); 
        }

        public async Task SaveAsync()
        {
            await Context.SaveChangesAsync(); 
        }

        public bool Contains(string articleId)
        {
            return Context.Articles.Any(x => x.Id == articleId);
        }

        public void Add(ArticleBase article)
        {
            Context.Articles.Add(article);
        }
    }
}