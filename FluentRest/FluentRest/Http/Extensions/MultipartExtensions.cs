using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Http.Content;

namespace FluentRest.Http
{
	/// <summary>
	/// Fluent extension methods for sending multipart/form-data requests.
	/// </summary>
	public static class MultipartExtensions
	{
		/// <summary>
		/// Sends an asynchronous multipart/form-data POST request.
		/// </summary>
		/// <param name="buildContent">A delegate for building the content parts.</param>
		/// <param name="request">The IFluentRestRequest.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <returns>A Task whose result is the received IFluentRestResponse.</returns>
		public static Task<IFluentRestResponse> PostMultipartAsync(this IFluentRestRequest request, Action<CapturedMultipartContent> buildContent, CancellationToken cancellationToken = default(CancellationToken)) {
			var cmc = new CapturedMultipartContent(request.Settings);
			buildContent(cmc);
			return request.SendAsync(HttpMethod.Post, cmc, cancellationToken);
		}
	}
}
