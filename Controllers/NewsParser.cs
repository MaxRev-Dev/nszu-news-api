using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;

namespace NSZUNews.Controllers
{
    public class NewsParser : IHostedService
    {
        private readonly ILogger<NewsParser> _logger;
        private readonly ArticleRepository _repository;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public NewsParser(ILogger<NewsParser> logger,
            ArticleRepository repository,
            IHostEnvironment env,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _repository = repository;
            _env = env;
            _configuration = configuration;
            _appLifetime = appLifetime;
        }

        public async Task ParseAsync(CancellationToken token)
        {
            Ready = false;
            var parserConfig = _configuration.GetSection("NewsParser");
            var url = parserConfig["Url"];
            var countPages = parserConfig.GetValue<int>("CountPages");
            try
            {
                for (int i = 1; i <= countPages; i++)
                {
                    var finalUrl = string.Format(url, i);
                    await GetForPage(i, finalUrl, token);
                    if (token.IsCancellationRequested)
                        return;
                }
            }
            catch (XPathException e)
            {
                _logger.LogInformation($"XPath error: {e.Message}");
                return;
            }

            _repository.Save(CacheFile);
            _logger.LogInformation("News parser finished it's job");
            Ready = true;
        }

        public bool Ready { get; set; }

        private async Task GetForPage(int page, string pageUrl, CancellationToken token)
        {
            var topContainerXpath =
                _configuration.GetSection("NewsParser")["topContainerXpath"];
            var cacheRefresh = TimeSpan.FromHours(
                _configuration.GetSection("NewsParser")
                    .GetValue<int?>("CacheRefreshHours") ?? 12);

            var uri = new Uri(pageUrl);
            using var httpClient = new HttpClient();
            using var request = await httpClient.GetAsync(uri, token);
            if (request.IsSuccessStatusCode)
            {
                var doc = new HtmlDocument();
                doc.Load(await request.Content.ReadAsStreamAsync(token));
                var containerNode = doc.DocumentNode.SelectSingleNode(topContainerXpath);

                foreach (var divArticleContainer in
                    containerNode.Descendants("div")
                        .Where(x => x.HasClass("block-new")))
                {
                    var date = divArticleContainer
                        .SelectSingleNode("a[1]/p").InnerText;
                    var title = divArticleContainer
                        .SelectSingleNode("div[2]").InnerText;
                    var imgSrc = divArticleContainer
                        .SelectSingleNode("div[1]/a/img")
                        .Attributes["src"].Value;
                    var excerpt = divArticleContainer
                        .SelectSingleNode("div[3]/a/p").InnerText;
                    var url = divArticleContainer
                        .SelectSingleNode("div[1]/a").Attributes["href"].Value;

                    var article = new ArticleBase
                    {
                        Id = GetHashString(title + date),
                        Date = DateTime.Parse(date),
                        Url = url,
                        Title = title,
                        ImageUrl = "https://" + uri.Host + imgSrc,
                        Excerpt = excerpt,
                        FetchDate = DateTime.Now
                    };

                    if (_repository.Contains(article.Id)
                        && (DateTime.Now - article.FetchDate) < cacheRefresh)
                    {
                        continue;
                    }

                    // fetch article again
                    article.Content =
                        await GetArticleContent(article.Url, token);

                    _repository.Add(article);
                    _logger.LogInformation($"Fetched page {page} => {article.Title}");
                }

            }
            else
            {
                _logger.LogError($"Request to {pageUrl} failed with code {request.StatusCode}");
            }
        }

        private async Task<string> GetArticleContent(string articleUrl, CancellationToken token)
        {
            using var httpClient = new HttpClient();
            using var request = await httpClient.GetAsync(articleUrl, token);
            if (request.IsSuccessStatusCode)
            {
                var containerXpath =
                    _configuration.GetSection("NewsParser")["articleContainerXpath"];

                var doc = new HtmlDocument();
                doc.Load(await request.Content.ReadAsStreamAsync(token));
                var containerNode = doc.DocumentNode.SelectSingleNode(containerXpath);
                return containerNode.InnerHtml.Trim();
            }

            _logger.LogError($"Request to article => `{articleUrl}` failed with code {request.StatusCode}");
            return default;
        }

        private static byte[] GetHash(string inputString)
        {
            using HashAlgorithm algorithm = SHA256.Create();
            return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static string GetHashString(string inputString)
        {
            var sb = new StringBuilder();
            foreach (byte b in GetHash(inputString))
                sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
        private readonly IHostApplicationLifetime _appLifetime;


        public Task StartAsync(CancellationToken cancellationToken)
        {
            _appLifetime.ApplicationStarted.Register(OnStarted);
            _appLifetime.ApplicationStopping.Register(OnStopping);
            _appLifetime.ApplicationStopped.Register(OnStopped);

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        private async void OnStarted()
        {
            if (File.Exists(CacheFile))
            {
                await _repository.LoadAsync(CacheFile);
            } 
        }

        private string CacheFile => Path.Combine(_env.ContentRootPath, "news-cache.json");

        private void OnStopping()
        {
            _repository.Save(CacheFile);
        }

        private void OnStopped()
        {

        }
    }
}