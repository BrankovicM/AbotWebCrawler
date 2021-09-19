using Abot2.Crawler;
using Abot2.Poco;
using Nito.AsyncEx;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace AbotWebCrawlere
{
    class Program
    {

        private static readonly string _siteToCrawl = "https://www.imdb.com";
        private static readonly string _username = string.Empty;
        private static readonly string _password = string.Empty;

        static void Main(string[] args)
        {
            AsyncContext.Run(() => Crawl());
        }


        static async Task Crawl()
        {
            await SimpleCrawler();

            Console.WriteLine("---> Crawlovanje sajta završeno.");
        }

        private static async Task SimpleCrawler()
        {
            try
            {
                var config = new CrawlConfiguration
                {
                    MaxPagesToCrawl = 300,
                    MaxConcurrentThreads = 3,
                    MaxLinksPerPage = 100,
                    MaxCrawlDepth = 1000,
                    IsAlwaysLogin = true,
                    LoginUser = _username,
                    LoginPassword = _password,
                    IsRespectRobotsDotTextEnabled = true,
                    IsSendingCookiesEnabled = false
                };

                var crawler = new PoliteWebCrawler(config);

                crawler.PageCrawlCompleted += PageCrawlCompleted;
                crawler.PageCrawlStarting += ProcessPageCrawlStarting;
                crawler.PageCrawlCompleted += ProcessPageCrawlCompleted;
                crawler.PageCrawlDisallowed += PageCrawlDisallowed;
                crawler.PageLinksCrawlDisallowed += PageLinksCrawlDisallowed;

                crawler.ShouldCrawlPageDecisionMaker = (pageToCrawl, crawlContext) =>
                {
                    var decision = new CrawlDecision { Allow = true };
                    if (pageToCrawl.RedirectedFrom != null)
                        return new CrawlDecision { Allow = false, Reason = "'Ne crawlovati stranice na koje si redirektovan.'" };
                    if (pageToCrawl.Uri.AbsoluteUri.Contains("gallery") 
                        || pageToCrawl.Uri.AbsoluteUri.Contains("media") 
                        || pageToCrawl.Uri.AbsoluteUri.Contains("video"))
                        return new CrawlDecision { Allow = false, Reason = "'Ne crawlovati nebitan sadrzaj.'" };

                    return decision;
                };

                var crawlResult = await crawler.CrawlAsync(new Uri(_siteToCrawl));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void PageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            PageToCrawl crawledPage = e.CrawledPage;
            Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                $": Crawlovanje je uspešno završeno za {crawledPage.Uri.Host+crawledPage.Uri.AbsolutePath}");
        }

        private static void ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                $": Početak crawlovanja stranicena adresi {pageToCrawl.Uri.Host + pageToCrawl.Uri.AbsolutePath} " +
                $"čiji je link nađen na {pageToCrawl.Uri.Host + pageToCrawl.Uri.AbsolutePath}");
        }

        private static void ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            if (crawledPage.HttpRequestException != null || crawledPage.HttpResponseMessage.StatusCode != HttpStatusCode.OK)
                Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                    $": Crawlovanje nije uspelo za {crawledPage.Uri.Host + crawledPage.Uri.AbsolutePath}");
            else
                Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                    $": Crawlovanje uspešno za {crawledPage.Uri.Host + crawledPage.Uri.AbsolutePath}");

            if (string.IsNullOrEmpty(crawledPage.Content.Text))
                Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                    $": Stranica nema sadržaj: {crawledPage.Uri.Host + crawledPage.Uri.AbsolutePath}");

            var angleSharpHtmlDocument = crawledPage.AngleSharpHtmlDocument; //AngleSharp parser
                                                                             // dalja obrada u zavisnosti od namene
        }

        private static void PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            CrawledPage crawledPage = e.CrawledPage;
            Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                $": Linkovi sa stranice {crawledPage.Uri.Host + crawledPage.Uri.AbsolutePath} " +
                $"nisu crawlovani zbog {e.DisallowedReason}");
        }

        private static void PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            PageToCrawl pageToCrawl = e.PageToCrawl;
            Console.WriteLine($"Thread id" + Thread.CurrentThread.ManagedThreadId + 
                $": Stranica {pageToCrawl.Uri.Host + pageToCrawl.Uri.AbsolutePath} " +
                $"nije crawlovana zbog {e.DisallowedReason}");
        }
    }
}
