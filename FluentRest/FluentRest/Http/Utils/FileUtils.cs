﻿using System.IO;
using System.Threading.Tasks;

namespace FluentRest.Http
{
	internal static class FileUtils
	{
		internal static string GetFileName(string path) {
			return Path.GetFileName(path);
		}

		internal static string CombinePath(params string[] paths) {
			return Path.Combine(paths);
		}

		internal static Task<Stream> OpenReadAsync(string path, int bufferSize) {
			return Task.FromResult<Stream>(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true));
		}

		internal static Task<Stream> OpenWriteAsync(string folderPath, string fileName, int bufferSize) {
			Directory.CreateDirectory(folderPath); // checks existence
			var filePath = Path.Combine(folderPath, fileName);
			return Task.FromResult<Stream>(new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true));
		}

		/// <summary>
		/// Replaces invalid path characters with underscores.
		/// </summary>
		internal static string MakeValidName(string s) {
			return string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
		}
	}
}