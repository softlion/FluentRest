﻿using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace FluentRest.Http.Content;

/// <summary>
/// Represents HTTP content based on a local file. Typically used with PostMultipartAsync for uploading files.
/// </summary>
public class FileContent : HttpContent
{
	/// <summary>
	/// The local file path.
	/// </summary>
	public string Path { get; }

	private readonly int bufferSize;

	/// <summary>
	/// Initializes a new instance of the <see cref="FileContent"/> class.
	/// </summary>
	/// <param name="path">The local file path.</param>
	/// <param name="bufferSize">The buffer size of the stream upload in bytes. Defaults to 4096.</param>
	public FileContent(string path, int bufferSize = 4096) {
		Path = path;
		this.bufferSize = bufferSize;
	}

	/// <summary>
	/// Serializes to stream asynchronous.
	/// </summary>
	/// <param name="stream">The stream.</param>
	/// <param name="context">The context.</param>
	/// <returns></returns>
	protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
	{
		await using var source = await FileUtils.OpenReadAsync(Path, bufferSize);
		await source.CopyToAsync(stream, bufferSize);
	}

	/// <summary>
	/// Tries the length of the compute.
	/// </summary>
	/// <param name="length">The length.</param>
	/// <returns></returns>
	protected override bool TryComputeLength(out long length) 
	{
		length = -1;
		return false;
	}
}