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
		if (!IsVisible)
		{
			return;
		}
		if (Time.time - lastChangeTime > 3f)
		{
			IsVisible = false;
			return;
		}
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
		if (customStyle == null)
			return;

		float maxWidth = Screen.width - (offsetX * 2);
		float textHeight = customStyle.CalcHeight(new GUIContent(soundDisplay), maxWidth);

		// Align the bottom of the text block to (Screen.height - offsetY)
		float y = Screen.height - offsetY - textHeight;

		GUI.Label(new Rect(offsetX, y, maxWidth, textHeight), soundDisplay, customStyle);
	}



}