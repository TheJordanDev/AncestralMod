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

	public static Song lastPlayedSong = null!;
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
		ClearAudioClips();
		IsLoading = true;
		Plugin.Instance.StartCoroutine(LoadAllAudioClipsCoroutine(SoundsDirectory));
	}

	public void TrySyncAndLoadAudioClips()
	{
		if (IsSyncing) return;
		Task.Run(() => SyncAndLoadAudioClipsCoroutine());
	}

	private async Task SyncAndLoadAudioClipsCoroutine()
	{
		if (IsSyncing || IsLoading) return;
		
		GitService git = new(ConfigHandler.BugleSoundGitRepository.Value, SoundsDirectory);		
		BetterBugleUI.Instance?.ShowActionbar("Syncing audio repository...");
		IsSyncing = true;
		bool isRepo = git.IsRepository();
		bool folderChanged = false;
		if (!isRepo)
		{
			bool success = await git.CloneAsync();			
			if (!success) 
			{
				BetterBugleUI.Instance?.ShowActionbar("Failed to clone audio repository."); 
				IsSyncing = false; 
				return; 
			}
			
			folderChanged = true;

			var clonedFiles = Directory.GetFiles(SoundsDirectory, "*", SearchOption.AllDirectories)
				.Where(f => !f.Contains(".git")).ToArray();
			
			if (clonedFiles.Length == 0)
			{
				BetterBugleUI.Instance?.ShowActionbar("Checking out files...");
				bool checkoutSuccess = await git.CheckoutFilesAsync();
				
				if (!checkoutSuccess) 
				{ 
					BetterBugleUI.Instance?.ShowActionbar("Failed to checkout files from repository."); 
					IsSyncing = false; 
					return; 
				}
				
				// Check again after checkout
				var checkoutFiles = Directory.GetFiles(SoundsDirectory, "*", SearchOption.AllDirectories)
					.Where(f => !f.Contains(".git")).ToArray();
			}
		}
		else
		{
			bool needsPull = await git.NeedsPullAsync();
			
			if (needsPull)
			{
				bool success = await git.PullAsync();				
				
				if (!success)
				{
					folderChanged = true;
					BetterBugleUI.Instance?.ShowActionbar("Failed to pull audio repository updates.");
					IsSyncing = false;
					return;
				}
			}
		}

		
		var files = Directory.GetFiles(SoundsDirectory, "*", SearchOption.AllDirectories)
			.Where(f =>
				!f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}") &&
				Path.GetFileName(f) != ".git" &&
				!AudioTypes.Keys.Contains(Path.GetExtension(f).TrimStart('.').ToLowerInvariant())
			).ToList();

		foreach (var file in files)
		{
			try
			{
				File.Delete(file);
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"Failed to delete file {file}: {ex.Message}");
			}
		}

		IsSyncing = false;
		if (folderChanged)
		{
			BetterBugleUI.Instance?.ShowActionbar("Loading audio clips...");
			ClearAudioClips();
			IsLoading = true;
			Plugin.Instance.StartCoroutine(LoadAllAudioClipsCoroutine(SoundsDirectory));
		}
		else
		{
			BetterBugleUI.Instance?.ShowActionbar("Audio repository is up to date.");
		}
	}

	private void ClearAudioClips()
	{
		foreach (var song in Song.Songs.Values)
		{
			if (song.AudioClip != null)
			{
				var audioSources = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
				foreach (var audioSource in audioSources)
				{
					if (audioSource.clip == song.AudioClip)
					{
						audioSource.Stop();
						audioSource.clip = null;
					}
				}
				UnityEngine.Object.Destroy(song.AudioClip);
			}
		}
		Song.Songs.Clear();
		GC.Collect();
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

	private IEnumerator LoadAllAudioClipsCoroutine(string directoryPath)
	{
		List<(string filePath, string ext, string name)> filesToLoad = new();

		foreach (var ext in AudioTypes.Keys)
		{
			var files = Directory.GetFiles(directoryPath, $"*.{ext}", SearchOption.TopDirectoryOnly);
			foreach (var file in files)
			{
				string name = Path.GetFileNameWithoutExtension(file);
				name = name.Replace("_", " ");
				if (!Song.Songs.ContainsKey(name))
				{
					filesToLoad.Add((file, ext, name));
				}
			}
		}

		const int BATCH_SIZE = 2;
		int loadedCount = 0;
		for (int i = 0; i < filesToLoad.Count; i += BATCH_SIZE)
		{
			List<Coroutine> batchCoroutines = new();

			for (int j = i; j < Mathf.Min(i + BATCH_SIZE, filesToLoad.Count); j++)
			{
				var (filePath, ext, name) = filesToLoad[j];
				Coroutine loadCoroutine = Plugin.Instance.StartCoroutine(LoadAudioClipCoroutine(filePath, ext, name, Song.Songs));
				batchCoroutines.Add(loadCoroutine);
			}

			// Wait for this batch to complete
			foreach (var coroutine in batchCoroutines) yield return coroutine;
			loadedCount += batchCoroutines.Count;

			BetterBugleUI.Instance?.ShowActionbar($"Loading songs... {loadedCount}/{filesToLoad.Count}");
			yield return null;
		}

		OnAllAudioClipsLoaded();
	}

	private IEnumerator LoadAudioClipCoroutine(string filePath, string ext, string name, Dictionary<string, Song> songs)
	{
		using UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioTypes[ext]);
		yield return www.SendWebRequest();

		if (www.result != UnityWebRequest.Result.Success)
		{
			Debug.LogError($"Failed to load audio clip from {filePath}: {www.error}");
			yield break;
		}

		if (songs.ContainsKey(name))
		{
			Debug.LogWarning($"Audio clip '{name}' already loaded, skipping duplicate.");
			yield break;
		}

		AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
		if (clip == null) yield break;

		songs[name] = new Song(name, filePath, clip);
		Debug.Log($"Loaded audio clip: {name} from {filePath}");
	}

}

public class Song : IDisposable
{
	public static readonly Dictionary<string, Song> Songs = new();

	public static List<string> GetSongNames_Alphabetically()
	{
		return [.. new List<string>(Songs.Keys).OrderBy(name => name)];
	}

	public string Name { get; }
	public string FilePath { get; }
	public AudioClip AudioClip { get; }

	public Song(string name, string filePath, AudioClip audioClip)
	{
		Name = name;
		FilePath = filePath;
		AudioClip = audioClip;
	}

	public void Dispose()
    {
        if (AudioClip != null) UnityEngine.Object.Destroy(AudioClip);
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
		song = Song.Songs.GetValueOrDefault(BetterBugleModule.CurrentSongName);
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
		}

		if (!hold && isTooting)
		{
			isTooting = false;
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