using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Reflection;
using BepInEx.Configuration;
using System.Security.Cryptography;

namespace AncestralMod.UI;

public class ModSettingsUI
{
    // UI Constants
    private const int CANVAS_SORT_ORDER = 1001;
    private const float ENTRY_HEIGHT = 60f;
    private const float LAYOUT_SPACING = 10f;
    private const int PADDING = 20;
    private const int TITLE_FONT_SIZE = 24;
    private const int LABEL_FONT_SIZE = 16;
    private const int INPUT_FONT_SIZE = 14;
    private const int BUTTON_FONT_SIZE = 16;

    // UI Colors
    private static readonly Color PANEL_BACKGROUND = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    private static readonly Color ENTRY_BACKGROUND = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    private static readonly Color INPUT_BACKGROUND = new Color(0.2f, 0.2f, 0.2f, 1f);
    private static readonly Color BUTTON_BLUE = new Color(0.3f, 0.5f, 0.8f, 1f);
    private static readonly Color BUTTON_RED = new Color(0.8f, 0.3f, 0.3f, 1f);
    private static readonly Color BUTTON_GREEN = new Color(0.3f, 0.8f, 0.3f, 1f);
    private static readonly Color TOGGLE_BACKGROUND = new Color(0.3f, 0.3f, 0.3f, 1f);

    // Events
    public event Action? OnCloseRequested;
    public event Action? OnSaveRequested;

    // UI Components
    private GameObject? _configUI;
    private Canvas? _canvas;
    private bool _isVisible = false;
    private bool _isWaitingForKey = false;
    private ConfigEntry<KeyCode>? _currentEditingKey;
    private Button? _currentEditingButton;

    public bool IsVisible => _isVisible;
    public bool IsWaitingForKey => _isWaitingForKey;

    public void Initialize()
    {
        CreateUI();
        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (_configUI == null) return;

        _isVisible = visible;
        _configUI.SetActive(visible);
        SetCursorState(visible);

        Plugin.Log.LogInfo($"Settings UI {(visible ? "opened" : "closed")} - UI Active: {_configUI.activeSelf}, Cursor Locked: {Cursor.lockState}, Cursor Visible: {Cursor.visible}");
    }

    public void HandleKeyInput()
    {
        if (_isWaitingForKey)
        {
            HandleKeyCapture();
        }
    }

    public void Destroy()
    {
        DestroyUIComponents();
    }

    private void CreateUI()
    {
        CreateCanvas();
        CreateMainPanel();
        CreateContent();
    }

    private void CreateCanvas()
    {
        if (_canvas != null) return;

        var canvasGO = new GameObject("ModSettings_Canvas");
        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = CANVAS_SORT_ORDER;
        
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        canvasGO.AddComponent<GraphicRaycaster>();
        GameObject.DontDestroyOnLoad(canvasGO);
    }

    private void CreateMainPanel()
    {
        if (_canvas == null) return;

        _configUI = CreateUIElement("SettingsPanel", _canvas.transform);
        
        var panelBg = _configUI.AddComponent<Image>();
        panelBg.color = PANEL_BACKGROUND;
        
        var panelRect = _configUI.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.1f);
        panelRect.anchorMax = new Vector2(0.8f, 0.9f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
    }

    private void CreateContent()
    {
        if (_configUI == null) return;

        CreateTitle();
        var contentParent = CreateScrollView();
        CreateConfigEntries(contentParent);
        CreateBottomButtons();
    }

    private void CreateTitle()
    {
        if (_configUI == null) return;

        var titleGO = CreateUIElement("Title", _configUI.transform);
        var title = titleGO.AddComponent<TextMeshProUGUI>();
        title.text = "AncestralMod Settings";
        title.fontSize = TITLE_FONT_SIZE;
        title.color = Color.white;
        title.alignment = TextAlignmentOptions.Center;
        title.fontStyle = FontStyles.Bold;
        
        var titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.9f);
        titleRect.anchorMax = new Vector2(1, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    private GameObject CreateScrollView()
    {
        if (_configUI == null) throw new InvalidOperationException("Settings UI not initialized");

        var scrollViewGO = CreateUIElement("ScrollView", _configUI.transform);
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        
        var scrollViewRect = scrollViewGO.GetComponent<RectTransform>();
        scrollViewRect.anchorMin = new Vector2(0.05f, 0.15f);
        scrollViewRect.anchorMax = new Vector2(0.95f, 0.85f);
        scrollViewRect.offsetMin = Vector2.zero;
        scrollViewRect.offsetMax = Vector2.zero;

        var contentGO = CreateUIElement("Content", scrollViewGO.transform);
        var contentRect = contentGO.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        
        var contentLayout = contentGO.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = LAYOUT_SPACING;
        contentLayout.padding = new RectOffset(PADDING, PADDING, PADDING, PADDING);
        contentLayout.childControlHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childForceExpandWidth = true;
        
        var contentSizeFitter = contentGO.AddComponent<ContentSizeFitter>();
        contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.content = contentRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return contentGO;
    }

    private void CreateConfigEntries(GameObject parent)
    {
        var configHandlerType = typeof(ConfigHandler);
        var properties = configHandlerType.GetProperties(BindingFlags.Public | BindingFlags.Static);

        foreach (var property in properties)
        {
            if (IsConfigEntry(property))
            {
                CreateConfigEntry(parent, property);
            }
        }
    }

    private static bool IsConfigEntry(PropertyInfo property)
    {
        return property.PropertyType.IsGenericType && 
               property.PropertyType.GetGenericTypeDefinition() == typeof(ConfigEntry<>);
    }

    private void CreateConfigEntry(GameObject parent, PropertyInfo configProperty)
    {
        var configEntry = configProperty.GetValue(null);
        if (configEntry == null) return;

        var entryType = configEntry.GetType();
        var valueType = entryType.GetGenericArguments()[0];

        var (entryGO, labelGO) = CreateEntryContainer(parent, configProperty.Name);
        CreateValueEditor(entryGO, configEntry, valueType);
    }

    private (GameObject entryGO, GameObject labelGO) CreateEntryContainer(GameObject parent, string propertyName)
    {
        var entryGO = CreateUIElement($"Entry_{propertyName}", parent.transform);
        var entryRect = entryGO.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(0, ENTRY_HEIGHT);

        var entryBg = entryGO.AddComponent<Image>();
        entryBg.color = ENTRY_BACKGROUND;

        var labelGO = CreateUIElement("Label", entryGO.transform);
        var label = labelGO.AddComponent<TextMeshProUGUI>();
        label.text = propertyName;
        label.fontSize = LABEL_FONT_SIZE;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        
        var labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0.05f, 0);
        labelRect.anchorMax = new Vector2(0.4f, 1);
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        return (entryGO, labelGO);
    }

    private void CreateValueEditor(GameObject parent, object configEntry, Type valueType)
    {
        if (valueType == typeof(KeyCode))
            CreateKeyCodeEditor(parent, configEntry as ConfigEntry<KeyCode>);
        else if (valueType == typeof(bool))
            CreateBoolEditor(parent, configEntry as ConfigEntry<bool>);
        else if (valueType == typeof(float))
            CreateFloatEditor(parent, configEntry as ConfigEntry<float>);
        else if (valueType == typeof(int))
            CreateIntEditor(parent, configEntry as ConfigEntry<int>);
        else if (valueType == typeof(string))
            CreateStringEditor(parent, configEntry as ConfigEntry<string>);
    }

    private void CreateKeyCodeEditor(GameObject parent, ConfigEntry<KeyCode>? keyEntry)
    {
        if (keyEntry == null) return;

        var buttonGO = CreateUIElement("KeyButton", parent.transform);
        var buttonImg = buttonGO.AddComponent<Image>();
        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImg;
        buttonImg.color = BUTTON_BLUE;
        
        var buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.45f, 0.2f);
        buttonRect.anchorMax = new Vector2(0.95f, 0.8f);
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        var textGO = CreateUIElement("Text", buttonGO.transform);
        var buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = keyEntry.Value.ToString();
        buttonText.fontSize = INPUT_FONT_SIZE;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(() => StartKeyCapture(keyEntry, button, buttonText));
    }

    private void CreateBoolEditor(GameObject parent, ConfigEntry<bool>? boolEntry)
    {
        if (boolEntry == null) return;

        // ✅ Create toggle with proper positioning
        var toggleGO = CreateUIElement("Toggle", parent.transform);
        var toggle = toggleGO.AddComponent<Toggle>();
        toggle.isOn = boolEntry.Value;
        
        var toggleRect = toggleGO.GetComponent<RectTransform>();
        // ✅ Use anchored position instead of just anchors
        toggleRect.anchorMin = new Vector2(0.45f, 0.5f);
        toggleRect.anchorMax = new Vector2(0.45f, 0.5f);
        toggleRect.anchoredPosition = Vector2.zero;
        toggleRect.sizeDelta = new Vector2(30, 30);

        // ✅ Create background properly
        var bgGO = CreateUIElement("Background", toggleGO.transform);
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = TOGGLE_BACKGROUND;
        toggle.targetGraphic = bgImg;

        var bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // ✅ Create checkmark with proper setup
        var checkGO = CreateUIElement("Checkmark", bgGO.transform);
        var checkImg = checkGO.AddComponent<Image>();
        checkImg.color = Color.green;
        toggle.graphic = checkImg;
        
        var checkRect = checkGO.GetComponent<RectTransform>();
        checkRect.anchorMin = new Vector2(0.15f, 0.15f);
        checkRect.anchorMax = new Vector2(0.85f, 0.85f);
        checkRect.offsetMin = Vector2.zero;
        checkRect.offsetMax = Vector2.zero;

        // ✅ Status text positioned properly next to toggle
        var statusTextGO = CreateUIElement("StatusText", parent.transform);
        var statusText = statusTextGO.AddComponent<TextMeshProUGUI>();
        statusText.text = boolEntry.Value ? "Enabled" : "Disabled";
        statusText.fontSize = INPUT_FONT_SIZE;
        statusText.color = boolEntry.Value ? Color.green : Color.red;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;

        var statusRect = statusTextGO.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.52f, 0.2f);
        statusRect.anchorMax = new Vector2(0.95f, 0.8f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;

        toggle.onValueChanged.AddListener(value => 
        {
            boolEntry.Value = value;
            statusText.text = value ? "Enabled" : "Disabled";
            statusText.color = value ? Color.green : Color.red;
        });
    }

    private void CreateFloatEditor(GameObject parent, ConfigEntry<float>? floatEntry)
    {
        if (floatEntry == null) return;

        AcceptableValueRange<float>? range = floatEntry.Description.AcceptableValues as AcceptableValueRange<float>;

        if (range != null)
        {
            // ✅ Create slider for ranged floats
            CreateSlider(parent, floatEntry.Value, range.MinValue, range.MaxValue, false,
                        value => floatEntry.Value = value,
                        value => value.ToString("F2"));
        }
        else
        {
            // ✅ Create input field for unrestricted floats
            TMP_InputField inputField = CreateInputField(parent, "FloatInput", floatEntry.Value.ToString("F2"));
            inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
            inputField.onEndEdit.AddListener(value =>
            {
                if (float.TryParse(value, out float result))
                {
                    floatEntry.Value = result;
                }
            });
        }
    }

    private void CreateIntEditor(GameObject parent, ConfigEntry<int>? intEntry)
    {
        if (intEntry == null) return;

        AcceptableValueRange<int>? range = intEntry.Description.AcceptableValues as AcceptableValueRange<int>;

        if (range != null)
        {
            // ✅ Create slider for ranged ints
            CreateSlider(parent, intEntry.Value, range.MinValue, range.MaxValue, true, 
                        value => intEntry.Value = Mathf.RoundToInt(value), 
                        value => Mathf.RoundToInt(value).ToString());
        }
        else
        {
            // ✅ Create input field for unrestricted ints
            var inputField = CreateInputField(parent, "IntInput", intEntry.Value.ToString());
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            inputField.onEndEdit.AddListener(value => {
                if (int.TryParse(value, out int result))
                {
                    intEntry.Value = result;
                }
            });
        }
    }


    // ✅ Fixed slider creation with proper sizing
    private void CreateSlider(GameObject parent, float currentValue, float minValue, float maxValue, bool wholeNumbers, System.Action<float> onValueChanged, System.Func<float, string> formatValue)
    {
        // Create slider container - better proportioned
        var sliderContainer = CreateUIElement("SliderContainer", parent.transform);
        var containerRect = sliderContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.45f, 0.2f);
        containerRect.anchorMax = new Vector2(0.95f, 0.8f);
        containerRect.offsetMin = Vector2.zero;
        containerRect.offsetMax = Vector2.zero;

        // Create the slider - properly sized within container
        var sliderGO = CreateUIElement("Slider", sliderContainer.transform);
        var slider = sliderGO.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = currentValue;
        slider.wholeNumbers = wholeNumbers;

        var sliderRect = sliderGO.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.45f);     // Centered vertically
        sliderRect.anchorMax = new Vector2(0.65f, 0.55f); // Much thinner - only 10% height
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;

        // Create slider background
        var backgroundGO = CreateUIElement("Background", sliderGO.transform);
        var backgroundImg = backgroundGO.AddComponent<Image>();
        backgroundImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        slider.targetGraphic = backgroundImg;

        var backgroundRect = backgroundGO.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        // Create fill area
        var fillAreaGO = CreateUIElement("Fill Area", sliderGO.transform);
        var fillAreaRect = fillAreaGO.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        var fillGO = CreateUIElement("Fill", fillAreaGO.transform);
        var fillImg = fillGO.AddComponent<Image>();
        fillImg.color = wholeNumbers ? BUTTON_GREEN : BUTTON_BLUE;
        slider.fillRect = fillGO.GetComponent<RectTransform>();

        var fillRect = fillGO.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Create handle area - properly sized
        var handleAreaGO = CreateUIElement("Handle Slide Area", sliderGO.transform);
        var handleAreaRect = handleAreaGO.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(10, 0);  // Handle margin
        handleAreaRect.offsetMax = new Vector2(-10, 0);

        var handleGO = CreateUIElement("Handle", handleAreaGO.transform);
        var handleImg = handleGO.AddComponent<Image>();
        handleImg.color = Color.white;
        var handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20); // Fixed handle size
        slider.handleRect = handleRect;

        // Create value display - positioned to the right
        var valueDisplayGO = CreateUIElement("ValueDisplay", sliderContainer.transform);
        var valueText = valueDisplayGO.AddComponent<TextMeshProUGUI>();
        valueText.text = formatValue(currentValue);
        valueText.fontSize = INPUT_FONT_SIZE;
        valueText.color = Color.white;
        valueText.alignment = TextAlignmentOptions.Center;

        var valueRect = valueDisplayGO.GetComponent<RectTransform>();
        valueRect.anchorMin = new Vector2(0.7f, 0.3f);
        valueRect.anchorMax = new Vector2(1f, 0.7f);
        valueRect.offsetMin = Vector2.zero;
        valueRect.offsetMax = Vector2.zero;

        // Add min/max labels - smaller and positioned at bottom
        var minLabelGO = CreateUIElement("MinLabel", sliderContainer.transform);
        var minLabel = minLabelGO.AddComponent<TextMeshProUGUI>();
        minLabel.text = formatValue(minValue);
        minLabel.fontSize = INPUT_FONT_SIZE - 4;
        minLabel.color = Color.gray;
        minLabel.alignment = TextAlignmentOptions.Center;

        var minRect = minLabelGO.GetComponent<RectTransform>();
        minRect.anchorMin = new Vector2(0, 0);
        minRect.anchorMax = new Vector2(0.15f, 0.25f);
        minRect.offsetMin = Vector2.zero;
        minRect.offsetMax = Vector2.zero;

        var maxLabelGO = CreateUIElement("MaxLabel", sliderContainer.transform);
        var maxLabel = maxLabelGO.AddComponent<TextMeshProUGUI>();
        maxLabel.text = formatValue(maxValue);
        maxLabel.fontSize = INPUT_FONT_SIZE - 4;
        maxLabel.color = Color.gray;
        maxLabel.alignment = TextAlignmentOptions.Center;

        var maxRect = maxLabelGO.GetComponent<RectTransform>();
        maxRect.anchorMin = new Vector2(0.5f, 0);
        maxRect.anchorMax = new Vector2(0.65f, 0.25f);
        maxRect.offsetMin = Vector2.zero;
        maxRect.offsetMax = Vector2.zero;

        // Add slider event listener
        slider.onValueChanged.AddListener(value =>
        {
            onValueChanged(value);
            valueText.text = formatValue(value);
        });
    }

    private void CreateStringEditor(GameObject parent, ConfigEntry<string>? stringEntry)
    {
        if (stringEntry == null) return;

        var inputField = CreateInputField(parent, "StringInput", stringEntry.Value);
        inputField.onEndEdit.AddListener(value => stringEntry.Value = value);
    }

    private TMP_InputField CreateInputField(GameObject parent, string name, string initialValue)
    {
        var inputGO = CreateUIElement(name, parent.transform);
        var inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.text = initialValue;
        
        var inputRect = inputGO.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.45f, 0.2f);
        inputRect.anchorMax = new Vector2(0.95f, 0.8f);
        inputRect.offsetMin = Vector2.zero;
        inputRect.offsetMax = Vector2.zero;

        var inputBg = inputGO.AddComponent<Image>();
        inputBg.color = INPUT_BACKGROUND;

        // ✅ Fix the text component setup
        var textGO = CreateUIElement("Text", inputGO.transform);
        var text = textGO.AddComponent<TextMeshProUGUI>();
        text.color = Color.white;
        text.fontSize = INPUT_FONT_SIZE;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        inputField.textComponent = text;

        // ✅ Add proper RectTransform for the text
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0); // Left padding
        textRect.offsetMax = new Vector2(-10, 0); // Right padding

        // ✅ Add placeholder if needed
        var placeholderGO = CreateUIElement("Placeholder", inputGO.transform);
        var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
        placeholder.text = "Enter text...";
        placeholder.fontSize = INPUT_FONT_SIZE;
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholder.alignment = TextAlignmentOptions.MidlineLeft;
        inputField.placeholder = placeholder;

        var placeholderRect = placeholderGO.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);

        return inputField;
    }

    private void CreateBottomButtons()
    {
        if (_configUI == null) return;

        CreateButton("CloseButton", "Close", BUTTON_RED, 
                    new Vector2(0.1f, 0.02f), new Vector2(0.35f, 0.12f), 
                    () => OnCloseRequested?.Invoke());

        CreateButton("SaveButton", "Save Settings", BUTTON_GREEN, 
                    new Vector2(0.65f, 0.02f), new Vector2(0.9f, 0.12f), 
                    () => OnSaveRequested?.Invoke());
    }

    private void CreateButton(string name, string text, Color color, Vector2 anchorMin, Vector2 anchorMax, UnityEngine.Events.UnityAction action)
    {
        if (_configUI == null) return;

        var buttonGO = CreateUIElement(name, _configUI.transform);
        var btnImg = buttonGO.AddComponent<Image>();
        var button = buttonGO.AddComponent<Button>();
        button.targetGraphic = btnImg;
        btnImg.color = color;
        
        var rect = buttonGO.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var textGO = CreateUIElement("Text", buttonGO.transform);
        var buttonText = textGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = text;
        buttonText.fontSize = BUTTON_FONT_SIZE;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        var textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        button.onClick.AddListener(action);
    }

    private void StartKeyCapture(ConfigEntry<KeyCode> keyEntry, Button button, TextMeshProUGUI buttonText)
    {
        _isWaitingForKey = true;
        _currentEditingKey = keyEntry;
        _currentEditingButton = button;
        buttonText.text = "Press any key...";
        buttonText.color = Color.yellow;
        
        SetCursorState(true);
    }

    private void HandleKeyCapture()
    {
        foreach (KeyCode keyCode in Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(keyCode))
            {
                CompleteKeyCapture(keyCode);
                break;
            }
        }
    }

    private void CompleteKeyCapture(KeyCode keyCode)
    {
        if (_currentEditingKey == null || _currentEditingButton == null) return;

        _currentEditingKey.Value = keyCode;
        
        var buttonText = _currentEditingButton.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.text = keyCode.ToString();
            buttonText.color = Color.white;
        }

        ResetKeyCapture();
        
        if (_isVisible)
        {
            SetCursorState(true);
        }
    }

    private void ResetKeyCapture()
    {
        _isWaitingForKey = false;
        _currentEditingKey = null;
        _currentEditingButton = null;
    }

    private static void SetCursorState(bool uiOpen)
    {
        if (uiOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Plugin.Log.LogInfo("Cursor unlocked and made visible for UI interaction");
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            Plugin.Log.LogInfo("Cursor locked and hidden for gameplay");
        }
    }

    private static GameObject CreateUIElement(string name, Transform parent)
    {
        var element = new GameObject(name);
        element.transform.SetParent(parent, false);
        element.AddComponent<RectTransform>();
        return element;
    }

    private void DestroyUIComponents()
    {
        if (_configUI != null)
        {
            GameObject.Destroy(_configUI);
            _configUI = null;
        }
        
        if (_canvas != null)
        {
            GameObject.Destroy(_canvas.gameObject);
            _canvas = null;
        }

        ResetKeyCapture();
    }
}
