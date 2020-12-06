using System;

namespace NSZUNews
{
    public class ArticleBase
    {
        public string Id;

        public DateTime FetchDate; 

        /// <summary>
        /// Publish date
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Article title
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Article related cover
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Short intro to article
        /// </summary>
        public string Excerpt { get; set; }

        /// <summary>
        /// Html content of the article
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Article url
        /// </summary>
        public string Url { get; set; }
    }
}
