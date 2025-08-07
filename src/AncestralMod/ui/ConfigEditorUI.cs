using System.Collections.Generic;
using AncestralMod.Modules;
using BepInEx.Configuration;
using UnityEngine;

namespace AncestralMod.UI;

class ConfigEditorUI : MonoBehaviour
{
	private Vector2 _scrollPosition = Vector2.zero;

	public static ConfigEditorUI Instance { get; private set; } = null!;

	// Window properties
	private Rect _windowRect = new Rect(10, 10, 300, 200);
	private bool _resizing = false;
	private Vector2 _resizeStartMouse;
	private Vector2 _resizeStartSize;

	// UI state
	private bool _isVisible;
	private bool _isWaitingForKey;
	private ConfigEntry<KeyCode>? _currentEditingKey;

	public bool IsVisible => _isVisible;
	public bool IsWaitingForKey => _isWaitingForKey;

	public void SetVisible(bool visible)
	{
		_isVisible = visible;
	}

	protected void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		_isVisible = false;
		_isWaitingForKey = false;
	}


	protected void OnGUI()
	{
		if (!_isVisible)
			return;
		GUI.Window(0, _windowRect, DrawWindow, "Ancestral Mod Config Editor", GUI.skin.window);
	}

	private void DrawWindow(int windowID)
	{
		_scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		CreateInputs();
		GUILayout.EndScrollView();

		if (GUILayout.Button("Close"))
		{
			Close();
		}
		GUI.DragWindow(new Rect(0, 0, _windowRect.width, 20));
		HandleResize();
	}


	private void HandleResize()
	{
		var resizeHandle = new Rect(_windowRect.width - 16, _windowRect.height - 16, 16, 16);
		GUI.DrawTexture(resizeHandle, Texture2D.whiteTexture);

		Vector2 mousePos = Event.current.mousePosition;

		if (!_resizing && Event.current.type == EventType.MouseDown && resizeHandle.Contains(mousePos))
		{
			_resizing = true;
			_resizeStartMouse = GUIUtility.GUIToScreenPoint(mousePos);
			_resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
			Event.current.Use();
		}

		if (_resizing)
		{
			Vector2 mouseScreen = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			Vector2 delta = mouseScreen - _resizeStartMouse;
			float newWidth = Mathf.Max(150, _resizeStartSize.x + delta.x);
			float newHeight = Mathf.Max(100, _resizeStartSize.y + delta.y);

			newWidth = Mathf.Min(newWidth, Screen.width - _windowRect.x);
			newHeight = Mathf.Min(newHeight, Screen.height - _windowRect.y);
			_windowRect.width = newWidth;
			_windowRect.height = newHeight;

			if (Event.current.rawType == EventType.MouseUp || Input.GetMouseButtonUp(0))
			{
				_resizing = false;
				Event.current.Use();
			}
		}
	}


	private void Open()
	{
		_isVisible = true;
	}

	private void Close()
	{
		_isVisible = false;
	}

	public void Toggle()
	{
		if (_isVisible) Close();
		else Open();
	}

	private void CreateInputs()
	{
		Dictionary<string, List<ConfigEntryBase>> perSectionConfig = new();

		foreach (ConfigDefinition configDefinition in ConfigHandler.config.Keys)
		{
			ConfigEntryBase configEntry = ConfigHandler.config[configDefinition];
			if (configEntry == null) continue;

			string section = configDefinition.Section;
			if (!perSectionConfig.ContainsKey(section)) perSectionConfig[section] = new List<ConfigEntryBase>();
			perSectionConfig[section].Add(configEntry);
		}

		foreach (KeyValuePair<string, List<ConfigEntryBase>> kvp in perSectionConfig)
		{
			GUILayout.BeginVertical("box");
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUILayout.Label($"<b>{kvp.Key}</b>", GUILayout.ExpandWidth(true));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			foreach (ConfigEntryBase configEntry in kvp.Value)
			{
				CreateInputField(configEntry);
			}
			GUILayout.EndVertical();
		}
	}

	private void CreateInputField(ConfigEntryBase configEntry)
	{
		if (configEntry == null) return;
		
		GUILayout.Label(configEntry.Description.Description, GUILayout.ExpandWidth(true));
		switch (configEntry.SettingType)
		{
			case var t when t == typeof(string):
				GUILayout.TextField(configEntry.BoxedValue?.ToString() ?? string.Empty);
				break;
			case var t when t == typeof(int):
				if (configEntry.Description.AcceptableValues is AcceptableValueRange<int> rangeObjInt)
				{
					float valueInt = (int)configEntry.BoxedValue;
					GUILayout.BeginHorizontal();
					GUILayout.Label($"Min: {rangeObjInt.MinValue}", GUILayout.ExpandWidth(true));
					GUILayout.Label($"Max: {rangeObjInt.MaxValue}", GUILayout.ExpandWidth(true));
					GUILayout.Label($"Current: {valueInt}", GUILayout.ExpandWidth(true));
					GUILayout.EndHorizontal();
					float newValueInt = GUILayout.HorizontalSlider(valueInt, rangeObjInt.MinValue, rangeObjInt.MaxValue);
					if (newValueInt == valueInt) return;
					configEntry.BoxedValue = (int)newValueInt;
				}
				else
				{
					string valueStr = configEntry.BoxedValue?.ToString() ?? string.Empty;
					string newValueStr = GUILayout.TextField(valueStr);
					if (newValueStr == valueStr) return;
					if (!int.TryParse(newValueStr, out int newValueInt)) return;
					configEntry.BoxedValue = newValueInt;
				}
				break;
			case var t when t == typeof(float):
				if (configEntry.Description.AcceptableValues is AcceptableValueRange<float> rangeObjFloat)
				{
					float valueFloat = (float)configEntry.BoxedValue;
					GUILayout.BeginHorizontal();
					GUILayout.Label($"Min: {rangeObjFloat.MinValue}", GUILayout.ExpandWidth(true));
					GUILayout.Label($"Max: {rangeObjFloat.MaxValue}", GUILayout.ExpandWidth(true));
					GUILayout.Label($"Current: {valueFloat}", GUILayout.ExpandWidth(true));
					GUILayout.EndHorizontal();
					float newValueFloat = GUILayout.HorizontalSlider(valueFloat, rangeObjFloat.MinValue, rangeObjFloat.MaxValue);
					newValueFloat = (float)Mathf.Round(newValueFloat * 100f) / 100f;
					if (Mathf.Approximately(newValueFloat, valueFloat)) return;
					configEntry.BoxedValue = newValueFloat;
				}
				else
				{
					string valueStr = configEntry.BoxedValue?.ToString() ?? string.Empty;
					string newValueStr = GUILayout.TextField(valueStr);
					if (newValueStr == valueStr) return;
					if (!float.TryParse(newValueStr, out float newValueFloat)) return;
					configEntry.BoxedValue = newValueFloat;
				}
				break;
			case var t when t == typeof(bool):
				bool currentValueBool = (bool)configEntry.BoxedValue;
				bool newValueBool = GUILayout.Toggle(currentValueBool, "Enabled");
				if (newValueBool != currentValueBool) configEntry.BoxedValue = newValueBool;
				break;
			case var t when t == typeof(KeyCode):
				KeyCode currentKey = (KeyCode)configEntry.BoxedValue;
				if (_isWaitingForKey && _currentEditingKey == configEntry)
				{
					GUILayout.Label("Press a key...");
					Event e = Event.current;
					if (e.type == EventType.KeyDown && _isWaitingForKey)
					{
						configEntry.BoxedValue = e.keyCode;
						_isWaitingForKey = false;
					}
				} else
				{
					if (GUILayout.Button(currentKey.ToString()))
					{
						_isWaitingForKey = true;
						_currentEditingKey = (ConfigEntry<KeyCode>)configEntry;
					}
				}
				break;
			default:
				GUILayout.Label($"Unsupported type for {configEntry.Definition.Key}: {configEntry.SettingType.Name}");
				break;
		}
	}
}