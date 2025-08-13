using Photon.Pun;
using UnityEngine;

namespace AncestralMod.UI;

public class BetterRoomUI : MonoBehaviour
{

	public static BetterRoomUI? Instance { get; private set; }

	private GUIStyle? customStyle;

	private bool fontLoaded = false;

	private int offsetX = 0;

	private int offsetY = 70;

	private int fontSize = 42;

	private bool IsVisible => PhotonNetwork.InRoom;

	private void Awake()
	{
		if (Instance != null)
		{
			Debug.LogError("Multiple instances of BetterRoomUI detected!");
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void OnGUI()
	{
		if (!IsVisible)
		{
			return;
		}
		if (!fontLoaded)
		{
			Font[] array = Resources.FindObjectsOfTypeAll<Font>();
			foreach (Font val in array)
			{
				if (val.name == "DarumaDropOne-Regular")
				{
					customStyle = new GUIStyle(GUI.skin.label)
					{
						font = val,
						fontSize = fontSize,
						alignment = TextAnchor.LowerCenter
					};
					customStyle.normal.textColor = Color.white;
					fontLoaded = true;
					break;
				}
			}
		}
		if (customStyle == null)
			return;
		
		string roomName = PhotonNetwork.CurrentRoom.Name;
		string playerCount = PhotonNetwork.CurrentRoom.PlayerCount + "/" + PhotonNetwork.CurrentRoom.MaxPlayers;
		string displayText = $"Players: {playerCount}\nCode: {roomName}";

		float maxWidth = Screen.width - (offsetX * 2);
		float textHeight = customStyle.CalcHeight(new GUIContent(displayText), maxWidth);

		// Align the bottom of the text block to (Screen.height - offsetY)
		float y = Screen.height - offsetY - textHeight;

		GUI.Label(new Rect(offsetX, y, maxWidth, textHeight), displayText, customStyle);
	}



}