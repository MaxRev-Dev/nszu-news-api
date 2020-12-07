using System.Collections.Generic;
using System.Linq;
using NSZUNews.Entities;

namespace NSZUNews.Controllers
{
    public class NewsService
    {
        private readonly ArticleRepository _articleRepository;

        public NewsService(ArticleRepository articleRepository)
        {
            _articleRepository = articleRepository;
        }

        public IEnumerable<ArticleBase> GetCollection(int count)
        {
            return _articleRepository.Context.Articles
                .OrderByDescending(c => c.Date).Take(count);
        }
    }
}