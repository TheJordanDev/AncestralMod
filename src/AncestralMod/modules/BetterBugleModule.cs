using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;
using UnityEngine.Audio;
using Zorro.Settings;
using AncestralMod.UI;
using AncestralMod.Utils;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace AncestralMod.Modules;

class BetterBugleModule : Module
{

	public static BetterBugleModule? Instance { get; private set; }

	public static readonly string SoundsDirectory = Path.Combine(BepInEx.Paths.BepInExRootPath, "bugleSounds");

	public static readonly Dictionary<string, AudioType> AudioTypes = new()
	{
		{ "wav", AudioType.WAV },
		{ "mp3", AudioType.MPEG },
		{ "ogg", AudioType.OGGVORBIS },
		{ "aiff", AudioType.AIFF },
	};

	public static bool IsLoading { get; private set; } = false;
	public static bool IsSyncing { get; private set; } = false;
	public static int CurrentSongIndex { get; set; } = 0;
	public static string CurrentSongName { get; set; } = "None";
	public static bool HadConfirmation { get; set; } = false;

	public static bool IsPlaying = false;
	public static AudioSource? CurrentAudioSource { get; set; } = null;

	public override string ModuleName => "BetterBugle";

	public static readonly string bugleItemName = "Bugle";

	public override Type[] GetPatches()
	{
		return [typeof(Patches.BetterBuglePatch)];
	}

	public override void Initialize()
	{
		if (Instance != null) return;
		Instance = this;
		SceneManager.sceneLoaded += OnSceneLoaded;
		ManageLocalizedText();
		GetAudioClips();
		base.Initialize();
	}

	public override void Update()
	{
		if (Input.GetKeyDown(ConfigHandler.SyncAudioRepository.Value))
		{
			Instance?.TrySyncAndLoadAudioClips();
		}
		base.Update();
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!BetterBugleUI.Instance)
		{
			GameObject uiObject = new("BetterBugleUI");
			UnityEngine.Object.DontDestroyOnLoad(uiObject);
			uiObject.AddComponent<BetterBugleUI>();
		}
	}

	public override void Destroy()
	{
		ClearAudioClips();
		base.Destroy();
	}

	private void ManageLocalizedText()
	{
		List<string> secondaryActionLocalizations = new(LocalizedText.LANGUAGE_COUNT);
		for (int i = 0; i < LocalizedText.LANGUAGE_COUNT; i++) secondaryActionLocalizations.Add("Refresh Songs");

		List<string> scrollActionLocalizations = new(LocalizedText.LANGUAGE_COUNT);
		for (int i = 0; i < LocalizedText.LANGUAGE_COUNT; i++) scrollActionLocalizations.Add("Change Song");

		LocalizedText.mainTable.Add("SONG_LIST", secondaryActionLocalizations);
		LocalizedText.mainTable.Add("CHANGE_SONG", scrollActionLocalizations);
	}

	public void GetAudioClips()
	{
		if (IsLoading || IsSyncing) return;
		if (!Directory.Exists(SoundsDirectory)) return;
		IsLoading = true;
		Plugin.Instance.StartCoroutine(LoadAllAudioClipsCoroutine(SoundsDirectory));
	}
	private void ClearAudioClips()
	{
		foreach (Song song in Song.Songs.Values.ToList())
		{
			song.Dispose();
		}
		Song.Songs.Clear();
		Song.SongsByHash.Clear();
		GC.Collect();
	}
	private IEnumerator LoadAllAudioClipsCoroutine(string directoryPath, string[]? forceReload = null)
	{
		List<(string filePath, string ext, string name)> filesToLoad = new();

		foreach (var ext in AudioTypes.Keys)
		{
			var files = Directory.GetFiles(directoryPath, $"*.{ext}");
			foreach (var file in files)
			{
				string name = Path.GetFileNameWithoutExtension(file);
    			bool shouldForceReload = forceReload != null && forceReload.Contains($"{name}.{ext}");
				if (!Song.Songs.ContainsKey(name) || shouldForceReload)
				{
					filesToLoad.Add((file, ext, name));
				}
			}
		}

		const int BATCH_SIZE = 2;
		int loadedCount = 0;

		for (int i = 0; i < filesToLoad.Count; i += BATCH_SIZE)
		{
			List<Coroutine> loadCoroutines = new();

			for (int j = i; j < i + Math.Min(BATCH_SIZE, filesToLoad.Count - i) && j < filesToLoad.Count; j++)
			{
				var (filePath, ext, name) = filesToLoad[j];
				bool forceReloadClip = forceReload != null && forceReload.Contains($"{name}.{ext}");
				Coroutine loadCoroutine = Plugin.Instance.StartCoroutine(LoadAudioClipCoroutine(filePath, ext, name, forceReloadClip));
				loadCoroutines.Add(loadCoroutine);
			}

			foreach (var coroutine in loadCoroutines) yield return coroutine;
			loadedCount += loadCoroutines.Count;
			BetterBugleUI.Instance?.ShowActionbar($"Loading audio clips... {loadedCount}/{filesToLoad.Count}");
		}
		OnAllAudioClipsLoaded();
	}
	private IEnumerator LoadAudioClipCoroutine(string filePath, string ext, string name, bool forceReload = false)
	{

		Debug.Log($"Loading audio clip: {name}.{ext} from {filePath}" + (forceReload ? " (forced reload)" : ""));

		using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip($"file://{filePath}", AudioTypes[ext]);
		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
			yield break;
		}

		bool songExists = Song.Songs.ContainsKey(name);

		Debug.Log($"Audio clip '{name}' exists: {songExists}. Force reload: {forceReload}");

		if (songExists && !forceReload)
		{
			Debug.LogWarning($"Audio clip with name '{name}' already exists. Skipping duplicate.");
			yield break;
		}

		if (songExists && forceReload)
		{
			Song? previousSong = Song.Songs.TryGetValue(name, out var existingSong) ? existingSong : null;
			previousSong?.Dispose();
		}

		AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);
		if (audioClip == null)
		{
			Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
			yield break;
		}

		Song song = new(name, ext, filePath, audioClip);
		song.register();
		Debug.Log($"Loaded audio clip: {name} from {filePath}");
	}
	private void OnAllAudioClipsLoaded()
	{
		if (Song.Songs.Count == 0) Debug.LogWarning("No songs loaded. Please ensure audio files are in the Sounds directory.");
		else Debug.Log($"🎵 {Song.Songs.Count} songs loaded !");
		BetterBugleUI.Instance?.ShowActionbar($"{Song.Songs.Count} songs loaded !");
		if (!Song.Songs.ContainsKey(CurrentSongName))
			CurrentSongName = Song.GetSongNames_Alphabetically()[CurrentSongIndex];
		IsLoading = false;
	}

	public void TrySyncAndLoadAudioClips()
	{
		if (IsLoading || IsSyncing) return;
		Task.Run(() =>
		{
			SyncAndLoadAudioClipsCoroutine().GetAwaiter().GetResult();
		});
	}
	private async Task SyncAndLoadAudioClipsCoroutine()
	{
		if (IsLoading || IsSyncing) return;
		IsSyncing = true;
		AudioSyncService audioSyncService = AudioSyncService.GetInstance();
		Dictionary<AudioSyncService.APIAudioFormat, Song?> toDownload = new();

		string[] existingSongNames = Song.Songs.Keys.ToArray();
		AudioSyncService.APIAudioFormat[] existingAPIFormats = [.. audioSyncService.GetAudioClips()];
		string[] apiExistingNames = [.. existingAPIFormats.Select(apiAudio => apiAudio.Filename)];

		var songsToRemove = existingSongNames.Except(apiExistingNames).ToArray();
		foreach (var songName in songsToRemove)
		{
			if (Song.Songs.TryGetValue(songName, out var songToDispose))
			{
				songToDispose.Dispose();
				songToDispose.DeleteFile();
			}
		}


		foreach (AudioSyncService.APIAudioFormat apiAudio in existingAPIFormats)
		{
			Song? existingSong = Song.SongsByHash.GetValueOrDefault(apiAudio.Hash);
			if (existingSong == null || existingSong.Hash != apiAudio.Hash)
			{
				toDownload.Add(apiAudio, existingSong);
			}
		}

		BetterBugleUI.Instance?.ShowActionbar($"Syncing audio bank... {toDownload.Count} changed/new files found.");

		string[] filesToOverload = [];

		foreach (AudioSyncService.APIAudioFormat apiAudio in toDownload.Keys)
		{
			bool success = await DownloadAPIAudio(apiAudio, toDownload[apiAudio]);
			if (success)
			{
				Debug.Log($"Successfully downloaded audio: {apiAudio.Filename}.{apiAudio.Extension}, adding to forceload");
				filesToOverload = [.. filesToOverload, $"{apiAudio.Filename}.{apiAudio.Extension}"];
			}
		}
		IsSyncing = false;
		IsLoading = true;
		Plugin.Instance.StartCoroutine(LoadAllAudioClipsCoroutine(SoundsDirectory, filesToOverload));
	}
	private async Task<bool> DownloadAPIAudio(AudioSyncService.APIAudioFormat apiAudio, Song? existingSong = null)
	{
		bool success = true;
		try
		{
			if (existingSong != null && apiAudio.Filename != existingSong.Name)
			{
				File.Delete(Path.Combine(SoundsDirectory, $"{existingSong.Name}.{existingSong.Extension}"));
			}
			await apiAudio.DownloadToFolder(SoundsDirectory);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Failed to download API audio: {ex.Message}");
			success = false;
		}
		return success;
	}
}

public class Song : IDisposable
{
	public static readonly Dictionary<string, Song> Songs = new();
	public static readonly Dictionary<string, Song> SongsByHash = new();

	public static List<string> GetSongNames_Alphabetically()
	{
		return [.. new List<string>(Songs.Keys).OrderBy(name => name)];
	}

	public string Name { get; set; }
	public string Extension { get; set; }
	public string FilePath { get; set; }
	public AudioClip AudioClip { get; }
	public string Hash { get; }

	public Song(string name, string extension, string filePath, AudioClip audioClip)
	{
		Name = name;
		Extension = extension;
		FilePath = filePath;
		AudioClip = audioClip;
		Hash = GenerateHash(filePath);
	}

	public void register()
	{
		Songs[Name] = this;
		SongsByHash[Hash] = this;
	}

	public void Dispose()
	{
		if (AudioClip == null) return;
		var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
		foreach (var audioSource in audioSources)
		{
			if (audioSource.clip == AudioClip)
			{
				audioSource.Stop();
				audioSource.clip = null;
			}
		}
		Songs.Remove(Name);
		SongsByHash.Remove(Hash);
		UnityEngine.Object.Destroy(AudioClip);
	}

	public void DeleteFile()
	{
		if (AudioClip == null) return;
		var filePath = Path.Combine(BetterBugleModule.SoundsDirectory, $"{Name}.{Extension}");
		if (File.Exists(filePath))
		{
			File.Delete(filePath);
			Debug.Log($"Deleted local file: {filePath}");
		}
	}

	public string GenerateHash(string filePath)
	{
		using var hasher = SHA256.Create();
		var fileBytes = File.ReadAllBytes(filePath);
		var hashBytes = hasher.ComputeHash(fileBytes);
		return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
	}
}

public class BetterBugleSFX : MonoBehaviourPun
{
	public Item? item;
	public MagicBugle? magicBugle;
	public bool isMegaphone = false;
	public Song? song;
	public AudioSource? audioSource;
	public float GetVolume => (isMegaphone && item?._holderCharacter != Character.localCharacter) ? ConfigHandler.BugleVolume.Value * 2 : ConfigHandler.BugleVolume.Value;

	public float maxBugleDistance = 500;
	public float maxMegaphoneDistance = 1000;

	public bool hold = false;
	public bool isTooting = false;

	private void Start()
	{
		item = GetComponent<Item>();
		TryGetComponent<MagicBugle>(out magicBugle);
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
		audioSource.maxDistance = isMegaphone ? maxMegaphoneDistance : maxBugleDistance;
		audioSource.spatialBlend = 1f;
		audioSource.volume = 0f;
		audioSource.loop = true;
		if (IsLocal()) BetterBugleModule.CurrentAudioSource = audioSource;
		song = Song.Songs.GetValueOrDefault(BetterBugleModule.CurrentSongName);
	}

	private bool IsLocal()
	{
		return item?._holderCharacter == Character.localCharacter;
	}

	private void Update()
	{
		if (item == null || audioSource == null) return;
		UpdateTooting();
		if (hold && !isTooting)
		{
			audioSource.clip = song?.AudioClip;
			if (audioSource.clip == null) return;
			audioSource.Play();
			audioSource.volume = GetVolume;
			isTooting = true;
			if (IsLocal()) BetterBugleModule.IsPlaying = true;
		}

		if (!hold && isTooting)
		{
			isTooting = false;
			if (IsLocal()) BetterBugleModule.IsPlaying = false;
		}

		if (hold) audioSource.volume = Mathf.Lerp(audioSource.volume, GetVolume, 10f * Time.deltaTime);
		if (!hold) audioSource.volume = Mathf.Lerp(audioSource.volume, 0f, 10f * Time.deltaTime);

		if (!isTooting && audioSource.volume <= 0.01f)
		{
			audioSource.Stop();
		}
	}

	private void UpdateTooting()
	{
		if (item == null || audioSource == null) return;
		if (!photonView.IsMine) return;
		bool flag = item.isUsingPrimary;
		if (magicBugle && magicBugle.currentFuel <= 0f) flag = false;

		if (flag != hold)
		{
			if (flag) photonView.RPC("RPC_StartBetterToot", RpcTarget.All, BetterBugleModule.CurrentSongName);
			else photonView.RPC("RPC_StopBetterToot", RpcTarget.All);
			hold = flag;
		}
	}

	[PunRPC]
	private void RPC_StartBetterToot(string filename)
	{
		song = Song.Songs.GetValueOrDefault(filename);
		if (song == null) return;
		if (audioSource == null) return;
		hold = true;
	}

	[PunRPC]
	private void RPC_StopBetterToot()
	{
		if (audioSource == null) return;
		hold = false;
	}
}

public class BugleVolumeSettings : VolumeSetting
{
	public BugleVolumeSettings(AudioMixerGroup mixerGroup) : base(mixerGroup)
	{
	}

	public override string GetParameterName()
	{
		return "BugleVolume";
	}
	
	public string GetDisplayName()
	{
		return "Bugle Volume";
	}

	public string GetCategory()
	{
		return "Bugle";
	}
}