#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NFramework;
using Game.UI;
using Game.Gameplay;
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
            GenerateConfirmPopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Successfully generated all UI Prefabs under " + SAVE_PATH);
        }

        [MenuItem("Game/Generate Win+Lose Popups")]
        public static void GenerateWinLosePopups()
        {
            if (!Directory.Exists(SAVE_PATH))
                Directory.CreateDirectory(SAVE_PATH);

            GenerateWinPopup();
            GenerateLosePopup();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated WinPopup and LosePopup under " + SAVE_PATH);
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

        private static Sprite LoadSprite(string assetPath)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        }

        private static void GenerateHomeMenu()
        {
            GameObject go = new GameObject("HomeMenu", typeof(RectTransform), typeof(HomeMenu));
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Background base (dark purple)
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.10f, 0.06f, 0.18f, 1f);

            // Background gradient overlay (center glow effect)
            GameObject bgGlow = new GameObject("BackgroundGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGlow.transform.SetParent(go.transform, false);
            var bgGlowRt = bgGlow.GetComponent<RectTransform>();
            bgGlowRt.anchorMin = Vector2.zero; bgGlowRt.anchorMax = Vector2.one; bgGlowRt.sizeDelta = Vector2.zero;
            var bgGlowImg = bgGlow.GetComponent<Image>();
            bgGlowImg.sprite = LoadSprite("Assets/Sprite/Background Gradient.asset");
            bgGlowImg.color = new Color(0.35f, 0.15f, 0.70f, 0.30f);
            bgGlowImg.preserveAspect = false;

            // Settings button (top-left, icon only)
            GameObject settingsGo = new GameObject("SettingsButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            settingsGo.transform.SetParent(go.transform, false);
            var settingsRt = settingsGo.GetComponent<RectTransform>();
            settingsRt.sizeDelta = new Vector2(110, 110);
            settingsRt.anchoredPosition = new Vector2(-460, 840);
            var settingsImg = settingsGo.GetComponent<Image>();
            settingsImg.sprite = LoadSprite("Assets/Sprite/ButtonSettings.asset");
            settingsImg.preserveAspect = true;
            var settingsBtn = settingsGo.GetComponent<Button>();

            // Level circle group
            GameObject circleGroup = new GameObject("LevelCircleGroup", typeof(RectTransform));
            circleGroup.transform.SetParent(go.transform, false);
            var circleRt = circleGroup.GetComponent<RectTransform>();
            circleRt.sizeDelta = new Vector2(600, 600);
            circleRt.anchoredPosition = new Vector2(0, 80);

            // Arc background ring
            GameObject progressBack = new GameObject("ProgressBack", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            progressBack.transform.SetParent(circleGroup.transform, false);
            var pbRt = progressBack.GetComponent<RectTransform>();
            pbRt.sizeDelta = new Vector2(600, 495);
            var pbImg = progressBack.GetComponent<Image>();
            pbImg.sprite = LoadSprite("Assets/Sprite/Lobby Hard Level Progress Back.asset");
            pbImg.preserveAspect = false;

            // Arc green fill
            GameObject progressFill = new GameObject("ProgressFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            progressFill.transform.SetParent(circleGroup.transform, false);
            var pfRt = progressFill.GetComponent<RectTransform>();
            pfRt.sizeDelta = new Vector2(574, 470);
            var pfImg = progressFill.GetComponent<Image>();
            pfImg.sprite = LoadSprite("Assets/Sprite/Lobby Hard Level Progress.asset");
            pfImg.preserveAspect = false;

            // "LEVEL" label
            var levelLabel = CreateText(circleGroup, "LevelLabel", "LEVEL", 50);
            levelLabel.fontStyle = FontStyles.Bold;
            levelLabel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 70);

            // Level number (dynamic)
            var levelNumber = CreateText(circleGroup, "LevelNumber", "1", 160);
            levelNumber.fontStyle = FontStyles.Bold;
            levelNumber.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);

            // Hard Level Badge (below circle)
            GameObject badgeGo = new GameObject("HardBadge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            badgeGo.transform.SetParent(go.transform, false);
            var badgeRt = badgeGo.GetComponent<RectTransform>();
            badgeRt.sizeDelta = new Vector2(363, 260);
            badgeRt.anchoredPosition = new Vector2(0, -220);
            var badgeImg = badgeGo.GetComponent<Image>();
            badgeImg.sprite = LoadSprite("Assets/Sprite/Lobby Hard Level Badge.asset");
            badgeImg.preserveAspect = true;

            // Play button
            GameObject playGo = new GameObject("PlayButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            playGo.transform.SetParent(go.transform, false);
            var playRt = playGo.GetComponent<RectTransform>();
            playRt.sizeDelta = new Vector2(560, 140);
            playRt.anchoredPosition = new Vector2(0, -730);
            var playImg = playGo.GetComponent<Image>();
            playImg.sprite = LoadSprite("Assets/Sprite/Button_green_shop.asset");
            playImg.type = Image.Type.Sliced;
            var playBtn = playGo.GetComponent<Button>();

            var playText = CreateText(playGo, "Text", "PLAY", 72);
            playText.fontStyle = FontStyles.Bold;
            var playTextRt = playText.GetComponent<RectTransform>();
            playTextRt.anchorMin = Vector2.zero; playTextRt.anchorMax = Vector2.one; playTextRt.sizeDelta = Vector2.zero;

            // Wire fields
            var menu = go.GetComponent<HomeMenu>();
            ConfigureBaseUIView(menu, EUILayer.Menu, true, false);

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("_playButton").objectReferenceValue = playBtn;
            serialized.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
            serialized.FindProperty("_levelText").objectReferenceValue = levelNumber;
            serialized.FindProperty("_hardBadge").objectReferenceValue = badgeGo;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "HomeMenu.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateLoadingPopup()
        {
            GameObject go = new GameObject("LoadingPopup", typeof(RectTransform), typeof(LoadingPopup));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Dark background
            GameObject bg = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            var bgRt = bg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bg.GetComponent<Image>().color = new Color(0.09f, 0.10f, 0.13f, 1f);

            // Purple glow overlay
            GameObject bgGlow = new GameObject("BackgroundGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGlow.transform.SetParent(go.transform, false);
            var glowRt = bgGlow.GetComponent<RectTransform>();
            glowRt.anchorMin = Vector2.zero; glowRt.anchorMax = Vector2.one; glowRt.sizeDelta = Vector2.zero;
            var glowImg = bgGlow.GetComponent<Image>();
            glowImg.sprite = LoadSprite("Assets/Sprite/Background Gradient.asset");
            glowImg.color = new Color(0.35f, 0.15f, 0.70f, 0.25f);
            glowImg.preserveAspect = false;

            // App logo
            GameObject logo = new GameObject("Logo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            logo.transform.SetParent(go.transform, false);
            var logoRt = logo.GetComponent<RectTransform>();
            logoRt.anchoredPosition = new Vector2(0, 300);
            logoRt.sizeDelta = new Vector2(380, 130);
            var logoImg = logo.GetComponent<Image>();
            logoImg.sprite = LoadSprite("Assets/Sprite/IconNameApp.asset");
            logoImg.preserveAspect = true;

            // Loading text (centered, higher up since no spinner)
            var loadTxt = CreateText(go, "LoadingText", "LOADING", 38);
            var loadTxtRt = loadTxt.GetComponent<RectTransform>();
            loadTxtRt.anchoredPosition = new Vector2(0, 40);
            loadTxtRt.sizeDelta = new Vector2(500, 60);
            loadTxt.color = new Color(0.7f, 0.7f, 0.9f, 1f);
            loadTxt.characterSpacing = 8f;

            // Progress bar background
            GameObject barBg = new GameObject("BarBg", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barBg.transform.SetParent(go.transform, false);
            var barBgRt = barBg.GetComponent<RectTransform>();
            barBgRt.anchoredPosition = new Vector2(0, -60);
            barBgRt.sizeDelta = new Vector2(700, 28);
            var barBgImg = barBg.GetComponent<Image>();
            barBgImg.sprite = LoadSprite("Assets/Sprite/ProgressBar_empty.asset");
            barBgImg.type = Image.Type.Sliced;
            barBgImg.color = new Color(1f, 1f, 1f, 0.15f);

            // Progress bar fill
            GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            barFill.transform.SetParent(barBg.transform, false);
            var barFillRt = barFill.GetComponent<RectTransform>();
            barFillRt.anchorMin = Vector2.zero; barFillRt.anchorMax = Vector2.one; barFillRt.sizeDelta = Vector2.zero;
            var fillImg = barFill.GetComponent<Image>();
            fillImg.sprite = LoadSprite("Assets/Sprite/ProgressBar_winstrick_fill.asset");
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = new Color(0.40f, 0.80f, 1.0f, 1f);

            var popup = go.GetComponent<LoadingPopup>();
            ConfigureBaseUIView(popup, EUILayer.AlwaysOnTop, true, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_loadingBarFill").objectReferenceValue = fillImg;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "LoadingPopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateGamePlayMenu()
        {
            GameObject go = new GameObject("GamePlayMenu", typeof(RectTransform), typeof(GamePlayMenu));
            var rt = go.GetComponent<RectTransform>();
            // Root stretch full + sizeDelta=0 → SafeArea component đặt anchor đúng vùng an toàn (tránh notch).
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
            go.AddComponent<SafeArea>();

            var menu = go.GetComponent<GamePlayMenu>();
            ConfigureBaseUIView(menu, EUILayer.Menu, true, false);

            // Nút dùng anchor TOP-LEFT (0,1): x = cách trái, y = -(cách trên) tính TỪ mép safe-area.
            // → luôn nằm dưới notch và không tràn đỉnh kể cả màn hình rộng hơn 9:16 (iPad).
            Button MakeIconBtn(string name, string spritePath, float fromLeft, float fromTop)
            {
                GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
                btnGo.transform.SetParent(go.transform, false);
                var btnRt = btnGo.GetComponent<RectTransform>();
                btnRt.anchorMin = new Vector2(0f, 1f);
                btnRt.anchorMax = new Vector2(0f, 1f);
                btnRt.pivot     = new Vector2(0.5f, 0.5f);
                btnRt.sizeDelta = new Vector2(110, 110);
                btnRt.anchoredPosition = new Vector2(fromLeft, -fromTop);
                var img = btnGo.GetComponent<Image>();
                img.sprite = LoadSprite(spritePath);
                img.preserveAspect = true;
                return btnGo.GetComponent<Button>();
            }

            var settingsBtn = MakeIconBtn("SettingsButton", "Assets/Sprite/ButtonSettings.asset", 85f,  105f);
            var homeBtn     = MakeIconBtn("HomeButton",     "Assets/Sprite/ButtonHome.asset",     205f, 105f);
            var retryBtn    = MakeIconBtn("RetryButton",    "Assets/Sprite/ButtonReset.asset",    325f, 105f);

            // ── HUD Level + đếm màu (Screen-Space, CÙNG canvas với nút) — anchor TOP-CENTER ──
            // Nút ở trái-trên, HUD ở giữa-trên; với CanvasScaler Match=Width chúng co giãn cùng
            // nhau → khoảng hở cố định, KHÔNG đè/tràn trên mọi tỉ lệ điện thoại.
            var levelLabel = CreateText(go, "LevelLabel", "Level 1", 44);
            levelLabel.fontStyle = FontStyles.Bold;
            var levelLabelRt = levelLabel.GetComponent<RectTransform>();
            levelLabelRt.anchorMin = new Vector2(0.5f, 1f);
            levelLabelRt.anchorMax = new Vector2(0.5f, 1f);
            levelLabelRt.pivot     = new Vector2(0.5f, 0.5f);
            levelLabelRt.sizeDelta        = new Vector2(500, 70);
            levelLabelRt.anchoredPosition = new Vector2(0f, -75f);
            var levelTextUI = levelLabel.gameObject.AddComponent<LevelTextUI>();
            var levelSo = new SerializedObject(levelTextUI);
            levelSo.FindProperty("_label").objectReferenceValue = levelLabel; // _label != null → KHÔNG Bootstrap World-Space
            levelSo.ApplyModifiedProperties();

            var goalGo = new GameObject("GoalCounterContainer", typeof(RectTransform));
            goalGo.transform.SetParent(go.transform, false);
            var goalRt = goalGo.GetComponent<RectTransform>();
            goalRt.anchorMin = new Vector2(0.5f, 1f);
            goalRt.anchorMax = new Vector2(0.5f, 1f);
            goalRt.pivot     = new Vector2(0.5f, 0.5f);
            goalRt.sizeDelta = new Vector2(900, 100);
            // -250: nằm HẲN dưới hàng nút (nút bottom ≈ -160; badge cao tối đa ~150) → tách theo
            // trục Y nên dù level tới 7 màu (Level_20) hàng badge cũng KHÔNG BAO GIỜ đè lên nút.
            goalRt.anchoredPosition = new Vector2(0f, -250f);
            goalGo.AddComponent<GoalCounterUI>();

            var serialized = new SerializedObject(menu);
            serialized.FindProperty("_settingsButton").objectReferenceValue = settingsBtn;
            serialized.FindProperty("_homeButton").objectReferenceValue = homeBtn;
            serialized.FindProperty("_retryButton").objectReferenceValue = retryBtn;
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

        private static void GenerateWinPopup()
        {
            const string SP = "Assets/Sprite/";
            GameObject go = new GameObject("WinPopup", typeof(RectTransform), typeof(Game.UI.WinPopup));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Dark overlay
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(go.transform, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            // Gold glow behind panel
            var glow = new GameObject("BackGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glow.transform.SetParent(go.transform, false);
            var glowRt = glow.GetComponent<RectTransform>();
            glowRt.anchoredPosition = new Vector2(0, 80);
            glowRt.sizeDelta = new Vector2(720, 720);
            var glowImg = glow.GetComponent<Image>();
            glowImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "light-effect-circ.asset");
            glowImg.color = new Color(1f, 0.85f, 0.1f, 0.40f);

            // Panel (window_back 9-sliced, dark navy)
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(860, 820);
            var panelImg = panel.GetComponent<Image>();
            panelImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "window_back.asset");
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.10f, 0.13f, 0.22f);

            // Trophy icon
            var trophy = new GameObject("TrophyIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trophy.transform.SetParent(panel.transform, false);
            var trophyRt = trophy.GetComponent<RectTransform>();
            trophyRt.anchoredPosition = new Vector2(0, 275);
            trophyRt.sizeDelta = new Vector2(200, 248);
            trophy.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Winstreak Icon_3.asset");

            // Title text (dynamic: "Level X Complete!")
            var title = CreateText(panel, "TitleText", "Level Complete!", 52);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 115);
            titleRt.sizeDelta = new Vector2(720, 75);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.85f, 0.15f);

            // Stars row
            var starsRow = new GameObject("StarsRow", typeof(RectTransform));
            starsRow.transform.SetParent(panel.transform, false);
            var starsRt = starsRow.GetComponent<RectTransform>();
            starsRt.anchoredPosition = new Vector2(0, 30);
            starsRt.sizeDelta = new Vector2(500, 160);

            var starSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "collectible-star.asset");
            foreach (var (name, pos, size) in new (string, Vector2, Vector2)[] {
                ("StarLeft",   new Vector2(-165, -10), new Vector2(110, 115)),
                ("StarCenter", new Vector2(0,     15), new Vector2(140, 146)),
                ("StarRight",  new Vector2( 165, -10), new Vector2(110, 115)),
            }) {
                var star = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                star.transform.SetParent(starsRow.transform, false);
                var starRt = star.GetComponent<RectTransform>();
                starRt.anchoredPosition = pos;
                starRt.sizeDelta = size;
                star.GetComponent<Image>().sprite = starSprite;
            }

            // NEXT LEVEL button (green, 9-sliced)
            var nextBtn = CreateButton(panel, "NextLevelButton", "NEXT LEVEL");
            var nextRt = nextBtn.GetComponent<RectTransform>();
            nextRt.anchoredPosition = new Vector2(0, -130);
            nextRt.sizeDelta = new Vector2(540, 120);
            var nextImg = nextBtn.GetComponent<Image>();
            nextImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Button_green_shop.asset");
            nextImg.type = Image.Type.Sliced;
            nextImg.color = Color.white;
            var nextTxt = nextBtn.GetComponentInChildren<TextMeshProUGUI>();
            nextTxt.text = "NEXT LEVEL";
            nextTxt.color = Color.white;
            nextTxt.fontSize = 42;
            nextTxt.fontStyle = FontStyles.Bold;

            // HOME button (blue, 9-sliced)
            var homeBtn = CreateButton(panel, "HomeButton", "HOME");
            var homeRt = homeBtn.GetComponent<RectTransform>();
            homeRt.anchoredPosition = new Vector2(0, -280);
            homeRt.sizeDelta = new Vector2(540, 120);
            var homeImg = homeBtn.GetComponent<Image>();
            homeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Button_blue_shop.asset");
            homeImg.type = Image.Type.Sliced;
            homeImg.color = Color.white;
            var homeTxt = homeBtn.GetComponentInChildren<TextMeshProUGUI>();
            homeTxt.text = "HOME";
            homeTxt.color = Color.white;
            homeTxt.fontSize = 42;
            homeTxt.fontStyle = FontStyles.Bold;

            var popup = go.GetComponent<Game.UI.WinPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_titleText").objectReferenceValue = title;
            serialized.FindProperty("_nextLevelButton").objectReferenceValue = nextBtn;
            serialized.FindProperty("_homeButton").objectReferenceValue = homeBtn;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "WinPopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateLosePopup()
        {
            const string SP = "Assets/Sprite/";
            GameObject go = new GameObject("LosePopup", typeof(RectTransform), typeof(Game.UI.LosePopup));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Dark overlay
            var overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(go.transform, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            // Red glow behind panel
            var glow = new GameObject("BackGlow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            glow.transform.SetParent(go.transform, false);
            var glowRt = glow.GetComponent<RectTransform>();
            glowRt.anchoredPosition = new Vector2(0, 80);
            glowRt.sizeDelta = new Vector2(620, 620);
            var glowImg = glow.GetComponent<Image>();
            glowImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "light-effect-circ.asset");
            glowImg.color = new Color(0.9f, 0.2f, 0.15f, 0.35f);

            // Panel (window_back 9-sliced, dark)
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(860, 800);
            var panelImg = panel.GetComponent<Image>();
            panelImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "window_back.asset");
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.10f, 0.10f, 0.18f);

            // Broken heart (left piece)
            var heartL = new GameObject("HeartLeft", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartL.transform.SetParent(panel.transform, false);
            var heartLRt = heartL.GetComponent<RectTransform>();
            heartLRt.anchoredPosition = new Vector2(-60, 260);
            heartLRt.sizeDelta = new Vector2(165, 274);
            heartL.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "BrokenHeartIcon _left.asset");

            // Broken heart (right piece)
            var heartR = new GameObject("HeartRight", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            heartR.transform.SetParent(panel.transform, false);
            var heartRRt = heartR.GetComponent<RectTransform>();
            heartRRt.anchoredPosition = new Vector2(60, 260);
            heartRRt.sizeDelta = new Vector2(165, 286);
            heartR.GetComponent<Image>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "BrokenHeartIcon _right.asset");

            // Title "LEVEL FAILED"
            var title = CreateText(panel, "TitleText", "LEVEL FAILED", 52);
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 95);
            titleRt.sizeDelta = new Vector2(720, 75);
            title.fontStyle = FontStyles.Bold;
            title.color = new Color(1f, 0.35f, 0.25f);

            // Subtitle (static decoration)
            var subtitle = CreateText(panel, "SubtitleText", "Better luck next time!", 34);
            var subtitleRt = subtitle.GetComponent<RectTransform>();
            subtitleRt.anchoredPosition = new Vector2(0, 25);
            subtitleRt.sizeDelta = new Vector2(680, 55);
            subtitle.color = new Color(0.72f, 0.72f, 0.82f);

            // RETRY button (red, 9-sliced)
            var retryBtn = CreateButton(panel, "RetryButton", "RETRY");
            var retryRt = retryBtn.GetComponent<RectTransform>();
            retryRt.anchoredPosition = new Vector2(0, -120);
            retryRt.sizeDelta = new Vector2(540, 120);
            var retryImg = retryBtn.GetComponent<Image>();
            retryImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Button_red.asset");
            retryImg.type = Image.Type.Sliced;
            retryImg.color = Color.white;
            var retryTxt = retryBtn.GetComponentInChildren<TextMeshProUGUI>();
            retryTxt.text = "RETRY";
            retryTxt.color = Color.white;
            retryTxt.fontSize = 42;
            retryTxt.fontStyle = FontStyles.Bold;

            // HOME button (blue, 9-sliced)
            var homeBtn = CreateButton(panel, "HomeButton", "HOME");
            var homeRt = homeBtn.GetComponent<RectTransform>();
            homeRt.anchoredPosition = new Vector2(0, -270);
            homeRt.sizeDelta = new Vector2(540, 120);
            var homeImg = homeBtn.GetComponent<Image>();
            homeImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SP + "Button_blue_shop.asset");
            homeImg.type = Image.Type.Sliced;
            homeImg.color = Color.white;
            var homeTxt = homeBtn.GetComponentInChildren<TextMeshProUGUI>();
            homeTxt.text = "HOME";
            homeTxt.color = Color.white;
            homeTxt.fontSize = 42;
            homeTxt.fontStyle = FontStyles.Bold;

            var popup = go.GetComponent<Game.UI.LosePopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);
            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_titleText").objectReferenceValue = title;
            serialized.FindProperty("_retryButton").objectReferenceValue = retryBtn;
            serialized.FindProperty("_homeButton").objectReferenceValue = homeBtn;
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "LosePopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateSettingPopup()
        {
            GameObject go = new GameObject("SettingPopup", typeof(RectTransform), typeof(SettingPopup));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Dark overlay
            GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(go.transform, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

            // Panel (dark rounded card)
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(860, 640);
            var panelImg = panel.GetComponent<Image>();
            panelImg.sprite = LoadSprite("Assets/Sprite/window_back.asset");
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.12f, 0.13f, 0.18f, 1f);

            // Gear icon
            GameObject gearGo = new GameObject("GearIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            gearGo.transform.SetParent(panel.transform, false);
            var gearRt = gearGo.GetComponent<RectTransform>();
            gearRt.anchoredPosition = new Vector2(0, 240);
            gearRt.sizeDelta = new Vector2(72, 72);
            gearGo.GetComponent<Image>().sprite = LoadSprite("Assets/Sprite/ButtonSettings.asset");

            // Title text
            var title = CreateText(panel, "SettingsTitle", "SETTINGS", 56);
            title.fontStyle = FontStyles.Bold;
            var titleRt = title.GetComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 178);
            titleRt.sizeDelta = new Vector2(700, 75);

            // Divider
            GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider.transform.SetParent(panel.transform, false);
            var divRt = divider.GetComponent<RectTransform>();
            divRt.anchoredPosition = new Vector2(0, 138);
            divRt.sizeDelta = new Vector2(740, 2);
            divider.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            // Helper: create a settings row with icon, label and iOS-style toggle
            Toggle MakeSwitchRow(string rowName, string iconPath, string label, float rowY)
            {
                // Icon
                GameObject iconGo = new GameObject(rowName + "Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(panel.transform, false);
                var iconRt = iconGo.GetComponent<RectTransform>();
                iconRt.anchoredPosition = new Vector2(-330, rowY);
                iconRt.sizeDelta = new Vector2(52, 52);
                var iconImg = iconGo.GetComponent<Image>();
                iconImg.sprite = LoadSprite(iconPath);
                iconImg.preserveAspect = true;
                iconImg.color = new Color(0.65f, 0.75f, 1f, 1f);

                // Label
                var labelTmp = CreateText(panel, rowName + "Label", label, 44);
                labelTmp.color = Color.white;
                labelTmp.alignment = TextAlignmentOptions.Left;
                labelTmp.GetComponent<RectTransform>().anchoredPosition = new Vector2(-195, rowY);
                labelTmp.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 55);

                // iOS-style switch toggle
                GameObject toggleGo = new GameObject(rowName + "Toggle", typeof(RectTransform), typeof(Toggle));
                toggleGo.transform.SetParent(panel.transform, false);
                var tRt = toggleGo.GetComponent<RectTransform>();
                tRt.anchoredPosition = new Vector2(295, rowY);
                tRt.sizeDelta = new Vector2(130, 62);

                // Off background
                GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                bgGo.transform.SetParent(toggleGo.transform, false);
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
                var bgImg = bgGo.GetComponent<Image>();
                bgImg.sprite = LoadSprite("Assets/Sprite/Button_swith_off.asset");
                bgImg.preserveAspect = false;

                // On checkmark (full size overlay)
                GameObject checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                checkGo.transform.SetParent(bgGo.transform, false);
                var checkRt = checkGo.GetComponent<RectTransform>();
                checkRt.anchorMin = Vector2.zero; checkRt.anchorMax = Vector2.one; checkRt.sizeDelta = Vector2.zero;
                var checkImg = checkGo.GetComponent<Image>();
                checkImg.sprite = LoadSprite("Assets/Sprite/Button_swith_on.asset");
                checkImg.preserveAspect = false;

                var toggle = toggleGo.GetComponent<Toggle>();
                toggle.targetGraphic = bgImg;
                toggle.graphic = checkImg;
                toggle.isOn = true;
                return toggle;
            }

            var soundsToggle    = MakeSwitchRow("Sounds",    "Assets/Sprite/Icon_volume.asset",    "Sound",     50f);
            var vibrationToggle = MakeSwitchRow("Vibration", "Assets/Sprite/Icon_vibration.asset", "Vibration", -80f);

            // Separator above close
            GameObject divider2 = new GameObject("Divider2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            divider2.transform.SetParent(panel.transform, false);
            var div2Rt = divider2.GetComponent<RectTransform>();
            div2Rt.anchoredPosition = new Vector2(0, -155);
            div2Rt.sizeDelta = new Vector2(740, 2);
            divider2.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

            // Close button (icon button, circular)
            GameObject closeBtnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(panel.transform, false);
            var closeRt = closeBtnGo.GetComponent<RectTransform>();
            closeRt.anchoredPosition = new Vector2(0, -240);
            closeRt.sizeDelta = new Vector2(400, 90);
            var closeSp = LoadSprite("Assets/Sprite/Button_green_shop.asset");
            var closeImg = closeBtnGo.GetComponent<Image>();
            closeImg.sprite = closeSp;
            closeImg.type = Image.Type.Sliced;
            closeImg.color = new Color(0.25f, 0.65f, 0.35f, 1f);

            GameObject closeTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            closeTxtGo.transform.SetParent(closeBtnGo.transform, false);
            var closeTxtRt = closeTxtGo.GetComponent<RectTransform>();
            closeTxtRt.anchorMin = Vector2.zero; closeTxtRt.anchorMax = Vector2.one; closeTxtRt.sizeDelta = Vector2.zero;
            var closeTmp = closeTxtGo.GetComponent<TextMeshProUGUI>();
            closeTmp.text = "CLOSE";
            closeTmp.alignment = TextAlignmentOptions.Center;
            closeTmp.fontSize = 44;
            closeTmp.fontStyle = FontStyles.Bold;
            closeTmp.color = Color.white;

            var popup = go.GetComponent<SettingPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, true, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_soundsToggle").objectReferenceValue    = soundsToggle;
            serialized.FindProperty("_vibrationToggle").objectReferenceValue = vibrationToggle;
            serialized.FindProperty("_closeButton").objectReferenceValue     = closeBtnGo.GetComponent<Button>();
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "SettingPopup.prefab");
            Object.DestroyImmediate(go);
        }

        private static void GenerateConfirmPopup()
        {
            GameObject go = new GameObject("ConfirmPopup", typeof(RectTransform), typeof(ConfirmPopup));
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(1080, 1920);
            go.AddComponent<SafeArea>();

            // Dim overlay
            GameObject overlay = new GameObject("Overlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.transform.SetParent(go.transform, false);
            var overlayRt = overlay.GetComponent<RectTransform>();
            overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one; overlayRt.sizeDelta = Vector2.zero;
            overlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            // Center panel
            GameObject panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(go.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.sizeDelta = new Vector2(700, 400);
            panelRt.anchoredPosition = Vector2.zero;
            var panelImg = panel.GetComponent<Image>();
            panelImg.color = new Color(0.13f, 0.14f, 0.18f, 1f);
            panelImg.sprite = LoadSprite("Assets/Sprite/Rounded Border Background Insert Popup.asset");
            panelImg.type = Image.Type.Sliced;

            // Message text
            GameObject msgGo = new GameObject("MessageText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            msgGo.transform.SetParent(panel.transform, false);
            var msgRt = msgGo.GetComponent<RectTransform>();
            msgRt.sizeDelta = new Vector2(600, 160);
            msgRt.anchoredPosition = new Vector2(0, 70);
            var msgTmp = msgGo.GetComponent<TextMeshProUGUI>();
            msgTmp.text = "Are you sure?";
            msgTmp.alignment = TextAlignmentOptions.Center;
            msgTmp.fontSize = 52;
            msgTmp.color = Color.white;

            // YES button (green)
            GameObject yesBtnGo = new GameObject("YesButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            yesBtnGo.transform.SetParent(panel.transform, false);
            var yesRt = yesBtnGo.GetComponent<RectTransform>();
            yesRt.sizeDelta = new Vector2(260, 100);
            yesRt.anchoredPosition = new Vector2(-150, -100);
            var yesImg = yesBtnGo.GetComponent<Image>();
            yesImg.sprite = LoadSprite("Assets/Sprite/Button_green_shop.asset");
            yesImg.type = Image.Type.Sliced;

            GameObject yesTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            yesTxtGo.transform.SetParent(yesBtnGo.transform, false);
            var yesTxtRt = yesTxtGo.GetComponent<RectTransform>();
            yesTxtRt.anchorMin = Vector2.zero; yesTxtRt.anchorMax = Vector2.one; yesTxtRt.sizeDelta = Vector2.zero;
            var yesTmp = yesTxtGo.GetComponent<TextMeshProUGUI>();
            yesTmp.text = "Yes"; yesTmp.alignment = TextAlignmentOptions.Center;
            yesTmp.fontSize = 48; yesTmp.color = Color.white; yesTmp.fontStyle = FontStyles.Bold;

            // CANCEL button (red)
            GameObject cancelBtnGo = new GameObject("CancelButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            cancelBtnGo.transform.SetParent(panel.transform, false);
            var cancelRt = cancelBtnGo.GetComponent<RectTransform>();
            cancelRt.sizeDelta = new Vector2(260, 100);
            cancelRt.anchoredPosition = new Vector2(150, -100);
            var cancelImg = cancelBtnGo.GetComponent<Image>();
            cancelImg.sprite = LoadSprite("Assets/Sprite/Button_red.asset");
            cancelImg.type = Image.Type.Sliced;

            GameObject cancelTxtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            cancelTxtGo.transform.SetParent(cancelBtnGo.transform, false);
            var cancelTxtRt = cancelTxtGo.GetComponent<RectTransform>();
            cancelTxtRt.anchorMin = Vector2.zero; cancelTxtRt.anchorMax = Vector2.one; cancelTxtRt.sizeDelta = Vector2.zero;
            var cancelTmp = cancelTxtGo.GetComponent<TextMeshProUGUI>();
            cancelTmp.text = "No"; cancelTmp.alignment = TextAlignmentOptions.Center;
            cancelTmp.fontSize = 48; cancelTmp.color = Color.white; cancelTmp.fontStyle = FontStyles.Bold;

            var popup = go.GetComponent<ConfirmPopup>();
            ConfigureBaseUIView(popup, EUILayer.Popup, false, false);

            var serialized = new SerializedObject(popup);
            serialized.FindProperty("_messageText").objectReferenceValue = msgTmp;
            serialized.FindProperty("_yesButton").objectReferenceValue   = yesBtnGo.GetComponent<Button>();
            serialized.FindProperty("_cancelButton").objectReferenceValue = cancelBtnGo.GetComponent<Button>();
            serialized.ApplyModifiedProperties();

            PrefabUtility.SaveAsPrefabAsset(go, SAVE_PATH + "ConfirmPopup.prefab");
            Object.DestroyImmediate(go);
        }
    }
}
#endif
