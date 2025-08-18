using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace AncestralMod.Utils;

class AudioSyncService
{
	public static string API_BASE_URL => ConfigHandler.BugleSoundAPIURL.Value;

	private static AudioSyncService? Instance { get; set; }
	public static AudioSyncService GetInstance()
	{
		Instance ??= new AudioSyncService();
		return Instance;
	}

	public List<APIAudioFormat> GetAudioClips()
	{
		List<APIAudioFormat> audioClips = [];

		Uri? uri = Uri.TryCreate($"{API_BASE_URL}/audio/list", UriKind.Absolute, out var result) ? result : null;
		if (uri == null)
		{
			Debug.LogError("Invalid BugleSoundAPIURL in config.");
			return audioClips;
		}

		using var client = new System.Net.WebClient();
		try
		{
			string json = client.DownloadString(uri);
			var data = JsonConvert.DeserializeObject<List<APIAudioFormat>>(json);
			if (data == null) return audioClips;
			audioClips.AddRange(data);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to fetch or parse audio clip hashes: {ex.Message}");
		}
		return audioClips;
	}

	public class APIAudioFormat
	{
		[JsonProperty("_id")]
		public string Id { get; set; } = string.Empty;

		[JsonProperty("filename")]
		public string Filename { get; set; } = string.Empty;

		[JsonProperty("extension")]
		public string Extension { get; set; } = string.Empty;

		[JsonProperty("size")]
		public long Size { get; set; }

		[JsonProperty("hash")]
		public string Hash { get; set; } = string.Empty;

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonProperty("modified_at")]
		public DateTime ModifiedAt { get; set; }

		[JsonProperty("owner")]
		public string Owner { get; set; } = string.Empty;

		public async Task DownloadToFolder(string folderPath)
		{
			if (string.IsNullOrEmpty(Filename) || string.IsNullOrEmpty(Extension))
			{
				Debug.LogError("Invalid audio file information.");
				return;
			}
			string filePath = Path.Combine(folderPath, $"{Filename}.{Extension}");

			// Ensure the directory exists
			Directory.CreateDirectory(folderPath);

			if (File.Exists(filePath)) File.Delete(filePath);

			string url = $"{API_BASE_URL}/audio/{Id}/download?hash={Hash}";
			Debug.LogError($"Downloading audio from URL: {url}");

			using UnityWebRequest www = UnityWebRequest.Get(url);
			var operation = www.SendWebRequest();
			while (!operation.isDone) await Task.Yield();
			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError($"Failed to download API audio: {www.error}");
				return;
			}
			File.WriteAllBytes(filePath, www.downloadHandler.data);
		}
	}

}