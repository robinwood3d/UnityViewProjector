using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public class ManualButtonCreator : MonoBehaviour
{
    // 按钮文本（可在Inspector中手动设置）
    [Header("按钮文本设置")]
    public string button1Text = "1";
    public string button2Text = "2";
    public string button3Text = "3";

    // 按钮样式设置
    [Header("按钮样式设置")]
    public float buttonWidth = 100f;
    public float buttonHeight = 60f;
    public float buttonSpacing = 15f;
    public float topMargin = 30f;
    public float leftMargin = 20f;

    OrbitCameraSettings defaultCameraSettings;

    // 字体设置
    [Header("字体设置")]
    public Font customFont;  // 可在Inspector中拖入自定义字体，不设置则使用默认字体

    void Start()
    {
        defaultCameraSettings = FindAnyObjectByType<OrbitCameraController>().orbitCameraSettings;
        CreateButtons();
    }

    void CreateButtons()
    {
        // 创建Canvas
        Canvas canvas = GetOrCreateCanvas();

        // 创建按钮1 - 使用匿名函数传入回调
        CreateButton(canvas.transform, button1Text, 0, () => {
            Debug.Log("按钮1被按下");
            var camera = FindAnyObjectByType<OrbitCameraController>();
            var target = GameObject.Find("P_ViewProjector1");
            camera.orbitCameraSettings.orbitLocation = target.transform.position;
            camera.orbitCameraSettings.orbitRotation = target.transform.eulerAngles;
            camera.orbitCameraSettings.zoomDistance = 0.1f;
            camera.StartCameraTransition(camera.orbitCameraSettings);
            
        });

        // 创建按钮2 - 使用匿名函数传入回调
        CreateButton(canvas.transform, button2Text, 1, () => {
            Debug.Log("按钮2被按下");
            var camera = FindAnyObjectByType<OrbitCameraController>();
            var target = GameObject.Find("P_ViewProjector2");
            camera.orbitCameraSettings.orbitLocation = target.transform.position;
            camera.orbitCameraSettings.orbitRotation = target.transform.eulerAngles;
            camera.orbitCameraSettings.zoomDistance = 0.1f;
            camera.StartCameraTransition(camera.orbitCameraSettings);
        });

        // 创建按钮3 - 使用匿名函数传入回调
        CreateButton(canvas.transform, button3Text, 2, () => {
            Debug.Log("按钮3被按下");
            var camera = FindAnyObjectByType<OrbitCameraController>();
            camera.StartCameraTransition(defaultCameraSettings);
        });
    }

    /// <summary>
    /// 创建按钮
    /// </summary>
    /// <param name="parent">父物体Transform</param>
    /// <param name="buttonText">按钮文本</param>
    /// <param name="index">按钮索引（用于计算位置）</param>
    /// <param name="onClickCallback">点击回调函数（支持匿名函数）</param>
    void CreateButton(Transform parent, string buttonText, int index, Action onClickCallback)
    {
        // 创建按钮GameObject
        GameObject buttonObj = new GameObject($"Button_{buttonText}");
        buttonObj.transform.SetParent(parent, false);

        // 添加RectTransform
        RectTransform rectTransform = buttonObj.AddComponent<RectTransform>();

        // 设置锚点为左上角
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);

        // 计算位置（竖向排列）
        float yPos = -(topMargin + index * (buttonHeight + buttonSpacing));
        rectTransform.anchoredPosition = new Vector2(leftMargin, yPos);
        rectTransform.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        // 添加Image作为背景
        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 0.8f, 1f);

        // 添加Button组件
        Button button = buttonObj.AddComponent<Button>();

        // 设置按钮颜色
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.6f, 0.8f, 1f);
        colors.highlightedColor = new Color(0.3f, 0.7f, 0.9f, 1f);
        colors.pressedColor = new Color(0.1f, 0.4f, 0.6f, 1f);
        button.colors = colors;

        // 添加回调函数（支持匿名函数）
        if (onClickCallback != null)
        {
            button.onClick.AddListener(() => onClickCallback());
        }

        // 添加文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.fontSize = 28;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // 设置字体
        if (customFont != null)
        {
            text.font = customFont;
        }
        else
        {
            // 使用Unity内置的Arial字体
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // 设置文本RectTransform填满按钮
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
    }

    Canvas GetOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("DynamicUICanvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasObj.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<EventSystem>();
                eventSystemObj.AddComponent<StandaloneInputModule>();
            }
        }

        return canvas;
    }
}