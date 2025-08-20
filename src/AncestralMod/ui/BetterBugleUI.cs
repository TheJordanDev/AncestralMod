using System.Linq;
using AncestralMod.Modules;
using UnityEngine;
using UnityEngine.UI;

namespace AncestralMod.UI;

public class BetterBugleUI : MonoBehaviour
{

	public static BetterBugleUI? Instance { get; private set; }
	public bool IsVisible { get; private set; }

	private float lastChangeTime = -10f;

	private string soundDisplay = "";

	private GUIStyle? customStyle;

	private bool fontLoaded = false;

	private int offsetX = 0;

	private int offsetY = 70;

	private int fontSize = 42;

	private void Awake()
	{
		if (Instance != null)
		{
			Debug.LogError("Multiple instances of BetterBugleUI detected!");
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
		IsVisible = false;
	}


	public void ShowActionbar(string message)
	{
		soundDisplay = message;
		IsVisible = true;
		lastChangeTime = Time.time;
	}

	private void OnGUI()
	{
		if (!fontLoaded)
		{
			Font[] array = Resources.FindObjectsOfTypeAll<Font>();
			foreach (Font val in array)
			{
				if (val.name == "DarumaDropOne-Regular")
				{
					customStyle = new GUIStyle(GUI.skin.label);
					customStyle.font = val;
					customStyle.fontSize = fontSize;
					customStyle.alignment = TextAnchor.LowerCenter;
					customStyle.normal.textColor = Color.white;
					fontLoaded = true;
					break;
				}
			}
		}
		if (customStyle == null) return;
		RenderSoundDisplay();
		RenderPlayingDisplay();
	}

	private void RenderSoundDisplay()
	{
		if (customStyle == null) return;
		if (!IsVisible) return;
		if (Time.time - lastChangeTime > 3f)
		{
			IsVisible = false;
			return;
		}

		float maxWidth = Screen.width - (offsetX * 2);
		float textHeight = customStyle.CalcHeight(new GUIContent(soundDisplay), maxWidth);

		// Align the bottom of the text block to (Screen.height - offsetY)
		float y = Screen.height - offsetY - textHeight;

		GUI.Label(new Rect(offsetX, y, maxWidth, textHeight), soundDisplay, customStyle);
	}

	private void RenderPlayingDisplay()
	{
		if (customStyle == null) return;
		if (BetterBugleModule.CurrentAudioSource == null || !BetterBugleModule.IsPlaying) return;
		Song? currentAudio = Song.Songs.FirstOrDefault(s => s.Value.Name == BetterBugleModule.CurrentSongName).Value;
		if (currentAudio == null || BetterBugleModule.CurrentAudioSource.clip == null) return;

		float MAX_WIDTH = Screen.width - (offsetX * 2);

		float audioLength = currentAudio.AudioClip.length;
		float progress = BetterBugleModule.IsPlaying ? BetterBugleModule.CurrentAudioSource.time / audioLength : 0f;

		// Progress bar: left to right, top right corner, with margin
		float margin = 32f;
		float barHeight = 18f;
		float barWidth = Mathf.Min(400f, MAX_WIDTH * 0.5f); // reasonable max width
		float barX = Screen.width - barWidth - margin;
		float barY = margin;

		// Draw background bar
		Rect barRect = new Rect(barX, barY, barWidth, barHeight);
		GUI.color = new Color(0f, 0f, 0f, 0.5f);
		GUI.DrawTexture(barRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false);

		// Draw progress circle (left to right)
		float circleRadius = barHeight * 0.8f * 0.5f;
		float circleCenterX = barX + barWidth * progress;
		float circleCenterY = barY + barHeight / 2f;
		GUI.color = Color.white;
		GUI.DrawTexture(new Rect(circleCenterX - circleRadius, circleCenterY - circleRadius, circleRadius * 2, circleRadius * 2), Texture2D.whiteTexture, ScaleMode.StretchToFill, true);

		// Draw progress text under the bar, centered relative to the bar
		float textY = barY + barHeight + 4f;
		string FormatTime(float t)
		{
			int minutes = (int)t / 60;
			float seconds = t % 60f;
			return $"{minutes:00}:{seconds,5:00.00}";
		}
		string progressText = $"{FormatTime(BetterBugleModule.CurrentAudioSource.time)} - {FormatTime(audioLength)}";
		GUIStyle textStyle = new GUIStyle(GUI.skin.label)
		{
			alignment = TextAnchor.UpperCenter,
			fontSize = 16,
			normal = { textColor = Color.white }
		};
		// Center the text horizontally relative to the bar
		GUI.Label(new Rect(barX, textY, barWidth, 22f), progressText, textStyle);

		// Reset color
		GUI.color = Color.white;
	}
}