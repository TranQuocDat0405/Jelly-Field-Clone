#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.UI;
using System.IO;

namespace Game.Editor
{
    public static class UIPrefabGenerator
    {
        private const string SAVE_PATH = "Assets/Game/Resources/UI/";

        [MenuItem("Game/Generate UI Prefabs")]
        public static void GenerateAllUI()
        {
            if (!Directory.Exists(SAVE_PATH))
            {
                Directory.CreateDirectory(SAVE_PATH);
            }

            GenerateHomeMenu();
            GenerateLoadingPopup();
            GenerateGamePlayMenu();
            GenerateResultPopup();
            GenerateSettingPopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all UI Prefabs under " + SAVE_PATH);
        }

        private static void ConfigureBaseUIView(BaseUIView view, EUILayer layer, bool unique, bool canDestroy)
        {
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_uiLayer").enumValueIndex = (int)layer;
            serialized.FindProperty("_isUnique").boolValue = unique;
            serialized.FindProperty("_canDestroy").boolValue = canDestroy;
            serialized.ApplyModifiedProperties();
        }

        private static Button CreateButton(GameObject parent, string name, string labelText)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent.transform, false);
            
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 100);

            // Add text child
            GameObject txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(btnGo.transform, false);
            
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.black;
            tmp.fontSize = 32;

            return btnGo.GetComponent<Button>();
        }

        private static TextMeshProUGUI CreateText(GameObject parent, string name, string defaultText, int fontSize)
        {
            GameObject txtGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGo.transform.SetParent(parent.transform, false);
            
            var tmp = txtGo.GetComponent<TextMeshProUGUI>();
            tmp.text = defaultText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = fontSize;
            tmp.color = Color.white;

            return tmp;
        }

        private static Slider CreateSlider(GameObject parent, string name)
        {
            GameObject sliderGo = new GameObject(name, typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(parent.transform, false);
            var rt = sliderGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(350, 40);

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0, 0.25f);
            bgRt.anchorMax = new Vector2(1, 0.75f);
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = Color.gray;

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0, 0.25f);
            faRt.anchorMax = new Vector2(1, 0.75f);
            faRt.sizeDelta = Vector2.zero;

            GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            var fillRt = fill.GetComponent<RectTransform>();
            fillRt.sizeDelta = Vector2.zero;
            fill.GetComponent<Image>().color = Color.green;

            // Handle Slide Area
            GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(sliderGo.transform, false);
            var haRt = handleArea.GetComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.sizeDelta = Vector2.zero;

            GameObject handle = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            handle.transform.SetParent(handleArea.transform, false);
            var handleRt = handle.GetComponent<RectTransform>();
            handleRt.sizeDelta = new Vector2(40, 40);
            handle.GetComponent<Image>().color = Color.white;

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRt;
            slider.handleRect = handleRt;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            return slider;
        }

        private static Toggle CreateToggle(GameObject parent, string name)
        {
            GameObject toggleGo = new GameObject(name, typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(parent.transform, false);
            var rt = toggleGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(60, 60);

            // Background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(toggleGo.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = Color.white;

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            checkmark.transform.SetParent(bg.transform, false);
            var checkRt = checkmark.GetComponent<RectTransform>();
            checkRt.anchorMin = Vector2.one * 0.1f;
            checkRt.anchorMax = Vector2.one * 0.9f;
            checkRt.sizeDelta = Vector2.zero;
            checkmark.GetComponent<Image>().color = Color.black;

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            toggle.isOn = true;

            return toggle;
        }

        private static void GenerateHomeMenu()
        {
            GameObject go = new GameObject("HomeMenu", typeof(RectTransform), typeof(HomeMenu));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);

            // Safe Area Component
            go.AddComponent<SafeArea>();

            // Add title
            var title = CreateText(go, "Title", "JELLY FIELD CLONE", 64);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 500);

            // Add buttons
            var play = CreateButton(go, "PlayButton", "PLAY");
            var settings = CreateButton(go, "SettingsButton", "SETTINGS");
            var shop = CreateButton(go, "ShopButton", "SHOP");

            // Position buttons vertically
            play.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            settings.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -160);
            shop.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -320);

            var menu = go.GetComponent<HomeMenu>();
            ConfigureBaseUIView(menu, EUILayer.Menu, true, false);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("_playButton").objectReferenceValue = play;
            serialized.FindProperty("_settingsButton").objectReferenceValue = settings;
            serialized.FindProperty("_shopButton").objectReferenceValue = shop;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "HomeMenu.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateLoadingPopup()
        {
            GameObject go = new GameObject("LoadingPopup", typeof(RectTransform), typeof(LoadingPopup));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);

            // Safe Area Component
            go.AddComponent<SafeArea>();

            // Background panel
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            bg.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            var txt = CreateText(go, "LoadingText", "LOADING...", 48);
            txt.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);

            // Spinner
            GameObject spinner = new GameObject("Spinner", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            spinner.transform.SetParent(go.transform, false);
            spinner.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 0);
            spinner.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 120);
            spinner.GetComponent<Image>().color = Color.white;

            // Fill Bar Background
            GameObject fillBg = new GameObject("FillBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillBg.transform.SetParent(go.transform, false);
            fillBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -200);
            fillBg.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 40);
            fillBg.GetComponent<Image>().color = Color.gray;

            // Fill Bar Foreground
            GameObject fillFg = new GameObject("FillFg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillFg.transform.SetParent(fillBg.transform, false);
            var fillRt = fillFg.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.sizeDelta = Vector2.zero;
            var fillImg = fillFg.GetComponent<Image>();
            fillImg.color = Color.green;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;

            var popup = go.GetComponent<LoadingPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_loadingBarFill").objectReferenceValue = fillImg;
            serialized.FindProperty("_spinner").objectReferenceValue = spinner.transform;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "LoadingPopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateGamePlayMenu()
        {
            GameObject go = new GameObject("GamePlayMenu", typeof(RectTransform), typeof(GamePlayMenu));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);

            // Safe Area Component
            go.AddComponent<SafeArea>();

            var menu = go.GetComponent<GamePlayMenu>();
            ConfigureBaseUIView(menu, EUILayer.Menu, true, false);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("_scoreText").objectReferenceValue = null;
            serialized.FindProperty("_coinText").objectReferenceValue = null;
            serialized.FindProperty("_pauseButton").objectReferenceValue = null;
            serialized.FindProperty("_boosterButton").objectReferenceValue = null;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "GamePlayMenu.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateResultPopup()
        {
            GameObject go = new GameObject("ResultPopup", typeof(RectTransform), typeof(ResultPopup));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);

            // Safe Area Component
            go.AddComponent<SafeArea>();

            // Panel Background
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(900, 1100);
            panel.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            var title = CreateText(panel, "ResultTitle", "GAME OVER", 64);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 350);

            var score = CreateText(panel, "ScoreText", "Score: 0", 40);
            score.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

            var bestScore = CreateText(panel, "BestScoreText", "Best Score: 0", 40);
            bestScore.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 50);

            var replay = CreateButton(panel, "ReplayButton", "REPLAY");
            replay.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
            replay.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);

            var home = CreateButton(panel, "HomeButton", "HOME");
            home.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -250);
            home.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 100);

            var popup = go.GetComponent<ResultPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_scoreText").objectReferenceValue = score;
            serialized.FindProperty("_bestScoreText").objectReferenceValue = bestScore;
            serialized.FindProperty("_replayButton").objectReferenceValue = replay;
            serialized.FindProperty("_homeButton").objectReferenceValue = home;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "ResultPopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateSettingPopup()
        {
            GameObject go = new GameObject("SettingPopup", typeof(RectTransform), typeof(SettingPopup));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);

            // Safe Area Component
            go.AddComponent<SafeArea>();

            // Panel Background
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            panel.GetComponent<RectTransform>().sizeDelta = new Vector2(950, 1300);
            panel.GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 0.95f);

            var title = CreateText(panel, "SettingsTitle", "SETTINGS", 60);
            title.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 450);

            // Sliders & Toggles labels
            var mVolLabel = CreateText(panel, "MusicLabel", "Music Volume", 32);
            mVolLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 250);
            var mSlider = CreateSlider(panel, "MusicVolumeSlider");
            mSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 250);

            var sVolLabel = CreateText(panel, "SfxLabel", "SFX Volume", 32);
            sVolLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 150);
            var sSlider = CreateSlider(panel, "SfxVolumeSlider");
            sSlider.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 150);

            var mToggleLabel = CreateText(panel, "MusicToggleLabel", "Music Enable", 32);
            mToggleLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, 50);
            var mToggle = CreateToggle(panel, "MusicToggle");
            mToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, 50);

            var sToggleLabel = CreateText(panel, "SfxToggleLabel", "SFX Enable", 32);
            sToggleLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, -50);
            var sToggle = CreateToggle(panel, "SfxToggle");
            sToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -50);

            var vToggleLabel = CreateText(panel, "VibrationToggleLabel", "Vibration", 32);
            vToggleLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-220, -150);
            var vToggle = CreateToggle(panel, "VibrationToggle");
            vToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(120, -150);

            var close = CreateButton(panel, "CloseButton", "CLOSE");
            close.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -350);
            close.GetComponent<RectTransform>().sizeDelta = new Vector2(350, 90);

            var popup = go.GetComponent<SettingPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_musicVolumeSlider").objectReferenceValue = mSlider;
            serialized.FindProperty("_sfxVolumeSlider").objectReferenceValue = sSlider;
            serialized.FindProperty("_musicToggle").objectReferenceValue = mToggle;
            serialized.FindProperty("_sfxToggle").objectReferenceValue = sToggle;
            serialized.FindProperty("_vibrationToggle").objectReferenceValue = vToggle;
            serialized.FindProperty("_closeButton").objectReferenceValue = close;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "SettingPopup.prefab");
            Object.DestroyImmediate(go);
        }
    }
}
#endif
