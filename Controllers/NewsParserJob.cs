using Quartz;
using System.Threading.Tasks;

namespace NSZUNews.Controllers
{
    public class NewsParserJob : IJob
    {
        private readonly NewsParser _parser;

        public NewsParserJob(NewsParser parser)
        {
            _parser = parser;
        }

        /// <inheritdoc />
        public Task Execute(IJobExecutionContext context)
        {
            return _parser.ParseAsync(context.CancellationToken);
        }
    }
}