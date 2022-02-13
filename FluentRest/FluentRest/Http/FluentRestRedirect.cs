using FluentRest.Urls;

namespace FluentRest.Http
{
    /// <summary>
    /// An object containing information about if/how an automatic redirect request will be created and sent.
    /// </summary>
    public class FluentRestRedirect
    {
        /// <summary>
        /// The URL to redirect to, from the response's Location header.
        /// </summary>
        public Url Url { get; set; }

        /// <summary>
        /// The number of auto-redirects that have already occurred since the original call, plus 1 for this one.
        /// </summary>
        public int Count { get; set; }

        /// <summary>
        /// If true, FluentRest will automatically send a redirect request. Set to false to prevent auto-redirect.
        /// </summary>
        public bool Follow { get; set; }

        /// <summary>
        /// If true, the redirect request will use the GET verb and will not forward the original request body.
        /// Otherwise, the original verb and body will be preserved in the redirect.
        /// </summary>
        public bool ChangeVerbToGet { get; set; }
    }
}