using System;

namespace FluentRest.Http
{
    /// <summary>
    /// An exception that is thrown when an HTTP call made by FluentRest.Http times out.
    /// </summary>
    public class FluentRestHttpTimeoutException : FluentRestHttpException
    {
        public FluentRestHttpTimeoutException(FluentRestDetail call, Exception inner) : base(call, BuildMessage(call), inner) { }
        private static string BuildMessage(FluentRestDetail? call) => call == null ? "Call timed out." :  $"Call timed out: {call}";
    }
}