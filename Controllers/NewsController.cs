using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using NSZUNews.Entities;

namespace NSZUNews.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class NewsController : ControllerBase
    {
        private readonly NewsService _newsService;

        public NewsController(NewsService newsService)
        {
            _newsService = newsService;
        }

        [HttpGet]
        public IEnumerable<ArticleBase> Get(int count = 10)
        {
            return _newsService.GetCollection(count);
        }
    }
}
