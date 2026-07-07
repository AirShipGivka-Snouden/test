using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;

namespace VpnHood.Core.Toolkit.Utils;

public static class MimeTypeUtils
{
	private static readonly FrozenDictionary<string, string> MimeTypes = new Dictionary<string, string>
	{
		[".html"] = "text/html",
		[".htm"] = "text/html",
		[".css"] = "text/css",
		[".js"] = "application/javascript",
		[".mjs"] = "application/javascript",
		[".json"] = "application/json",
		[".xml"] = "application/xml",
		[".txt"] = "text/plain",
		[".csv"] = "text/csv",
		[".rtf"] = "application/rtf",
		[".md"] = "text/markdown",
		[".yaml"] = "text/yaml",
		[".yml"] = "text/yaml",
		[".png"] = "image/png",
		[".jpg"] = "image/jpeg",
		[".jpeg"] = "image/jpeg",
		[".gif"] = "image/gif",
		[".bmp"] = "image/bmp",
		[".svg"] = "image/svg+xml",
		[".ico"] = "image/x-icon",
		[".webp"] = "image/webp",
		[".tiff"] = "image/tiff",
		[".tif"] = "image/tiff",
		[".avif"] = "image/avif",
		[".mp3"] = "audio/mpeg",
		[".wav"] = "audio/wav",
		[".ogg"] = "audio/ogg",
		[".m4a"] = "audio/mp4",
		[".aac"] = "audio/aac",
		[".flac"] = "audio/flac",
		[".wma"] = "audio/x-ms-wma",
		[".mp4"] = "video/mp4",
		[".avi"] = "video/x-msvideo",
		[".mov"] = "video/quicktime",
		[".wmv"] = "video/x-ms-wmv",
		[".flv"] = "video/x-flv",
		[".webm"] = "video/webm",
		[".mkv"] = "video/x-matroska",
		[".m4v"] = "video/mp4",
		[".3gp"] = "video/3gpp",
		[".pdf"] = "application/pdf",
		[".doc"] = "application/msword",
		[".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
		[".xls"] = "application/vnd.ms-excel",
		[".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
		[".ppt"] = "application/vnd.ms-powerpoint",
		[".pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
		[".odt"] = "application/vnd.oasis.opendocument.text",
		[".ods"] = "application/vnd.oasis.opendocument.spreadsheet",
		[".odp"] = "application/vnd.oasis.opendocument.presentation",
		[".zip"] = "application/zip",
		[".rar"] = "application/vnd.rar",
		[".7z"] = "application/x-7z-compressed",
		[".tar"] = "application/x-tar",
		[".gz"] = "application/gzip",
		[".bz2"] = "application/x-bzip2",
		[".woff"] = "font/woff",
		[".woff2"] = "font/woff2",
		[".ttf"] = "font/ttf",
		[".otf"] = "font/otf",
		[".eot"] = "application/vnd.ms-fontobject",
		[".exe"] = "application/vnd.microsoft.portable-executable",
		[".msi"] = "application/x-msdownload",
		[".dmg"] = "application/x-apple-diskimage",
		[".deb"] = "application/vnd.debian.binary-package",
		[".rpm"] = "application/x-rpm",
		[".apk"] = "application/vnd.android.package-archive",
		[".wasm"] = "application/wasm",
		[".map"] = "application/json",
		[".manifest"] = "text/cache-manifest",
		[".webmanifest"] = "application/manifest+json",
		[".sqlite"] = "application/vnd.sqlite3",
		[".db"] = "application/x-sqlite3",
		[".sql"] = "application/sql",
		[".cs"] = "text/plain",
		[".java"] = "text/plain",
		[".py"] = "text/plain",
		[".cpp"] = "text/plain",
		[".c"] = "text/plain",
		[".h"] = "text/plain",
		[".php"] = "text/plain",
		[".rb"] = "text/plain",
		[".go"] = "text/plain",
		[".rs"] = "text/plain",
		[".ts"] = "text/plain",
		[".jsx"] = "text/plain",
		[".tsx"] = "text/plain",
		[".vue"] = "text/plain",
		[".swift"] = "text/plain",
		[".kt"] = "text/plain",
		[".dart"] = "text/plain",
		[".conf"] = "text/plain",
		[".ini"] = "text/plain",
		[".cfg"] = "text/plain",
		[".toml"] = "text/plain",
		[".properties"] = "text/plain",
		[".log"] = "text/plain",
		[".out"] = "text/plain",
		[".err"] = "text/plain"
	}.ToFrozenDictionary();

	public static string GetContentType(string path)
	{
		string key = Path.GetExtension(path).ToLowerInvariant();
		return MimeTypes.GetValueOrDefault(key, "application/octet-stream");
	}

	public static bool IsSupported(string extension)
	{
		return MimeTypes.ContainsKey(extension.ToLowerInvariant());
	}

	public static IEnumerable<string> GetSupportedExtensions()
	{
		return MimeTypes.Keys;
	}
}
