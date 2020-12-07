using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSZUNews.Entities;
using System;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.XPath;
using Microsoft.Extensions.DependencyInjection;
using NSZUNews.Services;

namespace NSZUNews.Controllers
{
    public class NewsParser : IHostedService
    {
        private readonly ILogger<NewsParser> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHostEnvironment _env;
        private readonly IConfiguration _configuration;

        public NewsParser(ILogger<NewsParser> logger,
            IServiceScopeFactory scopeFactory,
            IHostEnvironment env,
            IConfiguration configuration,
            IHostApplicationLifetime appLifetime)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _env = env;
            _configuration = configuration;
            _appLifetime = appLifetime;
        }

        public async Task ParseAsync(CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<ArticleRepository>();
            Ready = false;
            var parserConfig = _configuration.GetSection("NewsParser");
            var url = parserConfig["Url"];
            var countPages = parserConfig.GetValue<int>("CountPages");
            try
            {
                for (int i = 1; i <= countPages; i++)
                {
                    var finalUrl = string.Format(url, i);
                    await GetForPage(repository, i, finalUrl, token);
                    await repository.SaveAsync();
                    if (token.IsCancellationRequested)
                        return;
                }
            }
            catch (XPathException e)
            {
                _logger.LogInformation($"XPath error: {e.Message}");
                return;
            }
            await repository.SaveAsync();
            _logger.LogInformation("News parser finished it's job");
            Ready = true;
        }

        public bool Ready { get; set; }

        private async Task GetForPage(ArticleRepository articleRepository,
            int page, string pageUrl, CancellationToken token)
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
                        Date = DateTime.Parse(date.Trim(), new CultureInfo("uk-UA"), DateTimeStyles.None),
                        Url = url,
                        Title = title,
                        ImageUrl = "https://" + uri.Host + imgSrc,
                        Excerpt = excerpt,
                        FetchDate = DateTime.Now
                    };

                    if (articleRepository.Contains(article.Id)
                        && (DateTime.Now - article.FetchDate) < cacheRefresh)
                    {
                        continue;
                    }

                    // fetch article again
                    article.Content =
                        await GetArticleContent(article.Url, token);

                    articleRepository.Add(article);
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
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider
                .GetRequiredService<ArticleRepository>();
            await repository.LoadAsync();
        }

        private void OnStopping()
        {
        }

        private void OnStopped()
        {

        }
    }
}