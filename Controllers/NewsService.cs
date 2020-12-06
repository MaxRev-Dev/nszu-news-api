using System.Collections.Generic;
using System.Linq;

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
            return _articleRepository.ArticleList.Take(count);
        }
    }
}