using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FluentRest.Http
{
	/// <summary>
	/// ReceiveXXX extension methods off Task&lt;IFluentRestResponse&gt; that allow chaining off methods like SendAsync
	/// without the need for nested awaits.
	/// </summary>
	public static class ResponseExtensions
	{
		/// <summary>
		/// Deserializes JSON-formatted HTTP response body to object of type T. Intended to chain off an async HTTP.
		/// </summary>
		/// <typeparam name="T">A type whose structure matches the expected JSON response.</typeparam>
		/// <returns>A Task whose result is an object containing data in the response body.</returns>
		/// <example>x = await url.PostAsync(data).ReceiveJson&lt;T&gt;()</example>
		/// <exception cref="FluentRestHttpException">Condition.</exception>
		public static async Task<T> ReceiveJson<T>(this Task<IFluentRestResponse> response) {
			using (var resp = await response) {
				if (resp == null) return default(T);
				return await resp.GetJsonAsync<T>();
			}
		}

		/// <summary>
		/// Returns HTTP response body as a string. Intended to chain off an async call.
		/// </summary>
		/// <returns>A Task whose result is the response body as a string.</returns>
		/// <example>s = await url.PostAsync(data).ReceiveString()</example>
		public static async Task<string> ReceiveString(this Task<IFluentRestResponse> response) {
			using (var resp = await response) {
				if (resp == null) return null;
				return await resp.GetStringAsync();
			}
		}

		/// <summary>
		/// Returns HTTP response body as a stream. Intended to chain off an async call.
		/// </summary>
		/// <returns>A Task whose result is the response body as a stream.</returns>
		/// <example>stream = await url.PostAsync(data).ReceiveStream()</example>
		public static async Task<Stream> ReceiveStream(this Task<IFluentRestResponse> response) {
			// don't wrap in a using, otherwise we'll dispose the stream too early.
			// we can dispose it if there's an error, otherwise the user is on the hook for it.
			var resp = await response;
			if (resp == null) return null;
			try {
				return await resp.GetStreamAsync();
			}
			catch (Exception) {
				resp.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Returns HTTP response body as a byte array. Intended to chain off an async call.
		/// </summary>
		/// <returns>A Task whose result is the response body as a byte array.</returns>
		/// <example>bytes = await url.PostAsync(data).ReceiveBytes()</example>
		public static async Task<byte[]> ReceiveBytes(this Task<IFluentRestResponse> response) {
			using (var resp = await response) {
				if (resp == null) return null;
				return await resp.GetBytesAsync();
			}
		}
	}
}