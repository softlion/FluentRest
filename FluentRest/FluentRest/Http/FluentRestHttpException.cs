using System;
using System.Threading.Tasks;

namespace FluentRest.Http
{
    /// <summary>
    /// An exception that is thrown when an HTTP call made by FluentRest.Http fails, including when the response
    /// indicates an unsuccessful HTTP status code.
    /// </summary>
    public class FluentRestHttpException : Exception
    {
        /// <summary>
        /// An object containing details about the failed HTTP call
        /// </summary>
        public FluentRestDetail? Call { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentRestHttpException"/> class.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public FluentRestHttpException(FluentRestDetail? call, string message, Exception? inner) : base(message, inner) {
            Call = call;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentRestHttpException"/> class.
        /// </summary>
        /// <param name="call">The call.</param>
        /// <param name="inner">The inner.</param>
        public FluentRestHttpException(FluentRestDetail? call, Exception? inner) : this(call, BuildMessage(call, inner), inner) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluentRestHttpException"/> class.
        /// </summary>
        /// <param name="call">The call.</param>
        public FluentRestHttpException(FluentRestDetail? call) : this(call, BuildMessage(call, null), null) { }

        private static string BuildMessage(FluentRestDetail? call, Exception? inner) {
            if (call?.Response != null && !call.Succeeded)
                return $"Call failed with status code {call.Response.StatusCode} ({call.HttpResponseMessage?.ReasonPhrase}): {call}";

            var msg = "Call failed";
            if (inner != null) msg += ". " + inner.Message.TrimEnd('.');
            return msg + ((call == null) ? "." : $": {call}");
        }

        /// <summary>
        /// Gets the HTTP status code of the response if a response was received, otherwise null.
        /// </summary>
        public int? StatusCode => Call?.Response?.StatusCode;

        /// <summary>
        /// Gets the response body of the failed call.
        /// </summary>
        /// <returns>A task whose result is the string contents of the response body.</returns>
        public Task<string?> GetResponseStringAsync() => Call?.Response?.GetStringAsync() ?? Task.FromResult((string?)null);

        /// <summary>
        /// Deserializes the JSON response body to an object of the given type.
        /// </summary>
        /// <typeparam name="T">A type whose structure matches the expected JSON response.</typeparam>
        /// <returns>A task whose result is an object containing data in the response body.</returns>
        public Task<T?> GetResponseJsonAsync<T>() => Call?.Response?.GetJsonAsync<T>() ?? Task.FromResult(default(T));
    }
}