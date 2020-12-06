using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace NSZUNews.Controllers
{
    public class ArticleRepository
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public ArticleRepository()
        {
            _serializerOptions = new JsonSerializerOptions
            {
                IncludeFields = true
            };
        }
        public List<ArticleBase> ArticleList { get; private set; } = new List<ArticleBase>();


        public async Task LoadAsync(string cacheFile)
        {
            var jsonString = File.OpenRead(cacheFile);
            ArticleList = await JsonSerializer
                .DeserializeAsync<List<ArticleBase>>(jsonString, _serializerOptions);
        }

        public void Save(string cacheFile)
        {
            using var fileStream = File.Open(cacheFile,
                File.Exists(cacheFile) ? FileMode.Truncate : FileMode.OpenOrCreate);
            using var writer = new Utf8JsonWriter(fileStream);

            JsonSerializer.Serialize(writer, ArticleList, _serializerOptions);
        }

        public bool Contains(string articleId)
        {
            return ArticleList.Any(x => x.Id == articleId);
        }

        public void Add(ArticleBase article)
        {
            ArticleList.Add(article);
        }
    }
}