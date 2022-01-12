using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentRest.Urls;

namespace FluentRest.Http
{
	/// <summary>
	/// Fluent extension methods for downloading a file.
	/// </summary>
	public static class DownloadExtensions
	{
		/// <summary>
		/// Asynchronously downloads a file at the specified URL.
		/// </summary>
		/// <param name="request">The FluentRest request.</param>
		/// <param name="localFolderPath">Path of local folder where file is to be downloaded.</param>
		/// <param name="localFileName">Name of local file. If not specified, the source filename (from Content-Dispostion header, or last segment of the URL) is used.</param>
		/// <param name="bufferSize">Buffer size in bytes. Default is 4096.</param>
		/// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
		/// <returns>A Task whose result is the local path of the downloaded file.</returns>
		public static async Task<string> DownloadFileAsync(this IFluentRestRequest request, string localFolderPath, string? localFileName = null, int bufferSize = 4096, CancellationToken cancellationToken = default) 
		{
			using var resp = await request.SendAsync(HttpMethod.Get, cancellationToken: cancellationToken, completionOption: HttpCompletionOption.ResponseHeadersRead);
			localFileName ??= GetFileNameFromHeaders(resp.ResponseMessage) ?? GetFileNameFromPath(request);

			// http://codereview.stackexchange.com/a/18679
			await using (var httpStream = await resp.GetStreamAsync())
			await using (var fileStream = await FileUtils.OpenWriteAsync(localFolderPath, localFileName, bufferSize)) 
			{
				await httpStream.CopyToAsync(fileStream, bufferSize, cancellationToken);
			}

			return FileUtils.CombinePath(localFolderPath, localFileName);
		}

		private static string? GetFileNameFromHeaders(HttpResponseMessage resp) 
		{
			var header = resp.Content?.Headers.ContentDisposition;
			if (header == null) 
				return null;
			// prefer filename* per https://tools.ietf.org/html/rfc6266#section-4.3
			var val = (header.FileNameStar ?? header.FileName)?.StripQuotes();
			return val == null ? null : FileUtils.MakeValidName(val);
		}

		private static string GetFileNameFromPath(IFluentRestRequest req) 
			=> FileUtils.MakeValidName(Url.Decode(req.Url.Path.Split('/').Last(), false));
	}
}