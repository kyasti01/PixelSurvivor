using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor.EditorTools
{
    /// <summary>Ekran UI'ını kurar: HUD, seviye atlama, duraklatma ve oyun sonu panelleri ile ana menü.</summary>
    public static partial class PrototypeBuilder
    {
        private static readonly Color Gold = new(1f, 0.82f, 0.35f);
        private static readonly Color Muted = new(0.85f, 0.8f, 0.75f);
        private static Font uiFont;
        // UI kurulmadan önce ayarlanır; UiButton ile yapılan her buton bu sesleri alır.
        private static AudioClip uiClickSfx;
        private static AudioClip uiHoverSfx;

        private static (HudView hud, LevelUpView levelUp, GameOverView gameOver) BuildUi(Font font)
        {
            uiFont = font;
            Transform root = CreateCanvas("UI").transform;

            HudView hud = BuildHud(root);
            BuildDamageNumbers(root);   // panellerin altında, HUD'un üstünde
            LevelUpView levelUp = BuildLevelUp(root);
            BuildPauseMenu(root, levelUp);
            GameOverView gameOver = BuildGameOver(root);
            return (hud, levelUp, gameOver);
        }

        /// <summary>1920x1080 referansına göre ölçeklenen ekran canvas'ı.</summary>
        private static Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            {
                layer = LayerMask.NameToLayer("UI")
            };
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            // Expand: 1920x1080 düzenin tamamı, en-boy oranı ne olursa olsun her zaman ekrana sığar
            // (match-width/height modu geniş ya da küçük Game pencerelerinde panelleri kesiyordu).
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            return canvas;
        }

        // ------------------------------------------------------------------ HUD

        private static HudView BuildHud(Transform canvas)
        {
            RectTransform hud = UiRect("HUD", canvas);
            Stretch(hud);

            // Üst kenar boyunca XP barı (Undead Survivor bar sprite'ları).
            Image xpBack = UiImage("XpBar", hud, null, new Color(0.08f, 0.08f, 0.1f, 0.9f));
            Place(xpBack.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 36));
            Image xpFill = UiImage("Fill", xpBack.transform, SpriteNamed(UiSheet, "Front 0"), Color.white);
            Stretch(xpFill.rectTransform, 3f);
            MakeFilled(xpFill);
            Text level = UiText("Level", xpBack.transform, "Lv.1", 30, TextAnchor.MiddleRight, Color.white);
            Stretch(level.rectTransform);
            level.rectTransform.offsetMax = new Vector2(-14, 0);

            // Can barı, sol üst (GUI_Parts çerçeve + çizgi).
            RectTransform hp = UiRect("Health", hud);
            // Orijinal görselden daha kısa ve kalın; çerçeve 9-slice olduğu için kenarları bozulmaz.
            Place(hp, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -46), new Vector2(340, 54));
            Image hpFill = UiImage("Line", hp, GuiSprite("Hp_line"), Color.white);
            Stretch(hpFill.rectTransform, 4f);
            MakeFilled(hpFill);
            Stretch(UiImage("Frame", hp, GuiSprite("Hp_frame"), Color.white, Image.Type.Sliced, 0.8f).rectTransform);
            Text hpText = UiText("Value", hp, "100 / 100", 34, TextAnchor.MiddleCenter, Color.white);
            Stretch(hpText.rectTransform);

            // Atılma hazırlık barı, can barının altında ince bir bar.
            Image dashBack = UiImage("DashBar", hud, null, new Color(0f, 0f, 0f, 0.6f));
            Place(dashBack.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(30, -104), new Vector2(328, 12));
            Image dashFill = UiImage("Fill", dashBack.transform, null, Color.white);
            Stretch(dashFill.rectTransform, 2f);
            MakeFilled(dashFill);

            // Kill ve altın sayaçları, sağ üst.
            Text killText = IconCounter(hud, "Kills", SpriteNamed(UiSheet, "Icon 0"), -42);
            Text goldText = IconCounter(hud, "Gold", BrackeysSprite("Items & Icons/Pixel Art/Coin.png"), -94);

            // Son boss'a geri sayım, üst orta.
            Text timer = UiText("Timer", hud, "00:00", 44, TextAnchor.MiddleCenter, Color.white);
            Place(timer.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(500, 56));

            // Sayacın altında boss can barı (boss gelene kadar gizli) ve uyarı yazısı.
            RectTransform bossBar = UiRect("BossBar", hud);
            Place(bossBar, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -100), new Vector2(720, 46));
            Image bossFill = UiImage("Line", bossBar, GuiSprite("Hp_line"), new Color(0.75f, 0.35f, 1f));
            Stretch(bossFill.rectTransform, 4f);
            MakeFilled(bossFill);
            Stretch(UiImage("Frame", bossBar, GuiSprite("Hp_frame"), Color.white, Image.Type.Sliced, 0.8f).rectTransform);
            Stretch(UiText("Label", bossBar, "BOSS", 30, TextAnchor.MiddleCenter, Color.white).rectTransform);
            bossBar.gameObject.SetActive(false);

            Text announcement = UiText("Announcement", hud, "", 72, TextAnchor.MiddleCenter, new Color(1f, 0.3f, 0.25f));
            Place(announcement.rectTransform, Center, Center, Center, new Vector2(0, 220), new Vector2(1400, 100));
            announcement.enabled = false;

            Text hint = UiText("Hint", hud, "Shift/Space: Atıl     Esc: Duraklat", 28, TextAnchor.LowerLeft, new Color(1f, 1f, 1f, 0.75f));
            Place(hint.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20, 12), new Vector2(700, 36));

            var view = hud.gameObject.AddComponent<HudView>();
            Set(view, "bossBar", bossBar.gameObject);
            Set(view, "bossFill", bossFill);
            Set(view, "announcementText", announcement);
            Set(view, "healthFill", hpFill);
            Set(view, "healthText", hpText);
            Set(view, "xpFill", xpFill);
            Set(view, "levelText", level);
            Set(view, "killText", killText);
            Set(view, "goldText", goldText);
            Set(view, "dashFill", dashFill);
            Set(view, "timerText", timer);
            return view;
        }

        /// <summary>Solda ikon, sağa hizalı sayı; sağ üst köşeye sabitlenir.</summary>
        private static Text IconCounter(Transform hud, string name, Sprite icon, float y)
        {
            RectTransform root = UiRect(name, hud);
            Place(root, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, y), new Vector2(240, 48));
            Image img = UiImage("Icon", root, icon, Color.white);
            img.preserveAspect = true;
            Place(img.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(40, 40));
            Text text = UiText("Value", root, "0", 40, TextAnchor.MiddleRight, Color.white);
            Stretch(text.rectTransform);
            return text;
        }

        private static void BuildDamageNumbers(Transform canvas)
        {
            RectTransform layer = UiRect("DamageNumbers", canvas);
            Stretch(layer);
            Set(layer.gameObject.AddComponent<DamageNumbers>(), "font", BodyFont);
        }

        // ------------------------------------------------------------------ seviye atlama

        private static LevelUpView BuildLevelUp(Transform canvas)
        {
            RectTransform holder = UiRect("LevelUp", canvas);
            Stretch(holder);
            var view = holder.gameObject.AddComponent<LevelUpView>();

            // Alt kenar boyunca bir şerit: ekran kararmaz, kartların dışındaki tıklamaları engellemez
            // ve oyuncu seçim yaparken oyun devam eder.
            RectTransform strip = UiRect("Strip", holder);
            Place(strip, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(1820, 300));

            // Kalabalık savaş alanının üstünde yazılar net okunsun diye koyu arka plan.
            Image backing = UiImage("Backing", strip, null, new Color(0f, 0f, 0f, 0.72f));
            Stretch(backing.rectTransform);

            Text title = UiText("Title", strip, "SEVİYE ATLADIN!", 48, TextAnchor.MiddleCenter, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -6), new Vector2(1400, 60));

            var so = new SerializedObject(view);
            so.FindProperty("root").objectReferenceValue = strip.gameObject;
            so.FindProperty("titleText").objectReferenceValue = title;
            SerializedProperty options = so.FindProperty("options");
            options.arraySize = 3;

            for (int i = 0; i < 3; i++)
            {
                Button button = UiButton($"Option{i + 1}", strip, Vector2.zero, Vector2.zero);
                Place((RectTransform)button.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0),
                    new Vector2(-600 + i * 600, 16), new Vector2(580, 210));

                // Sol sütun: basılacak tuş (üstte), ikon, seviye rozeti (altta).
                Text key = UiText("Key", button.transform, (i + 1).ToString(), 48, TextAnchor.UpperLeft, Gold);
                Place(key.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(16, -8), new Vector2(50, 56));

                Image icon = UiImage("Icon", button.transform, SpriteNamed(UiSheet, "Select 0"), Color.white);
                icon.preserveAspect = true;
                Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Center, new Vector2(108, 0), new Vector2(96, 96));

                Text badge = UiText("Badge", button.transform, "YENİ!", 32, TextAnchor.MiddleCenter, Color.white);
                Place(badge.rectTransform, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(10, 12), new Vector2(196, 40));

                // Sağ taraf: isim, ardından açıklama.
                Text name = UiText("Title", button.transform, "Upgrade", 34, TextAnchor.UpperLeft, Gold);
                Place(name.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), Vector2.zero, Vector2.zero);
                name.rectTransform.offsetMin = new Vector2(210, -62);
                name.rectTransform.offsetMax = new Vector2(-20, -22);

                Text desc = BodyText("Description", button.transform, "Description", 32, TextAnchor.UpperLeft, Color.white);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc.rectTransform, Vector2.zero, Vector2.one, Center, Vector2.zero, Vector2.zero);
                desc.rectTransform.offsetMin = new Vector2(210, 18);
                desc.rectTransform.offsetMax = new Vector2(-22, -74);

                SerializedProperty o = options.GetArrayElementAtIndex(i);
                o.FindPropertyRelative("button").objectReferenceValue = button;
                o.FindPropertyRelative("icon").objectReferenceValue = icon;
                o.FindPropertyRelative("title").objectReferenceValue = name;
                o.FindPropertyRelative("description").objectReferenceValue = desc;
                o.FindPropertyRelative("badge").objectReferenceValue = badge;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        // ------------------------------------------------------------------ duraklatma

        private static void BuildPauseMenu(Transform canvas, LevelUpView levelUp)
        {
            RectTransform holder = UiRect("PauseMenu", canvas);
            Stretch(holder);
            var view = holder.gameObject.AddComponent<PauseMenuView>();

            Image panel = UiImage("Panel", holder, null, new Color(0f, 0f, 0f, 0.65f));
            panel.raycastTarget = true;
            Stretch(panel.rectTransform);

            Image window = Window(panel.transform, new Vector2(900, 640));
            Text title = UiText("Title", window.transform, "DURAKLATILDI", 60, TextAnchor.MiddleCenter, Gold);
            Place(title.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(800, 90));

            Slider music = LabeledSlider(window.transform, "Müzik", new Vector2(0, 120));
            Slider sfx = LabeledSlider(window.transform, "Efektler", new Vector2(0, 30));

            Button resume = UiButton("Resume", window.transform, new Vector2(0, -100), new Vector2(460, 110));
            Stretch(UiText("Label", resume.transform, "DEVAM  [Esc]", 38, TextAnchor.MiddleCenter, Gold).rectTransform);
            Button menu = UiButton("Menu", window.transform, new Vector2(0, -225), new Vector2(460, 110));
            Stretch(UiText("Label", menu.transform, "ANA MENÜ", 38, TextAnchor.MiddleCenter, Gold).rectTransform);

            Set(view, "root", panel.gameObject);
            Set(view, "resumeButton", resume);
            Set(view, "menuButton", menu);
            Set(view, "musicSlider", music);
            Set(view, "sfxSlider", sfx);
            Set(view, "levelUpView", levelUp);
        }

        private static Slider LabeledSlider(Transform parent, string label, Vector2 position)
        {
            Text text = UiText(label, parent, label, 34, TextAnchor.MiddleRight, Color.white);
            Place(text.rectTransform, Center, Center, Center, position + new Vector2(-250, 0), new Vector2(220, 60));
            return UiSlider(label + "Slider", parent, position + new Vector2(110, 0), new Vector2(440, 40));
        }

        // ------------------------------------------------------------------ oyun sonu

        private static GameOverView BuildGameOver(Transform canvas)
        {
            RectTransform holder = UiRect("GameOver", canvas);
            Stretch(holder);
            var view = holder.gameObject.AddComponent<GameOverView>();

            // Arka plan rengi kazanma/kaybetmeye göre çalışma anında değişir (GameOverView).
            Image panel = UiImage("Panel", holder, null, new Color(0f, 0f, 0f, 0.75f));
            panel.raycastTarget = true;
            Stretch(panel.rectTransform);

            // Konfeti, panelin üstünde ama yazıların ve butonların altında.
            RectTransform confettiLayer = UiRect("Confetti", panel.transform);
            Stretch(confettiLayer);
            var confetti = confettiLayer.gameObject.AddComponent<Confetti>();

            // Büyük KAZANDIN! / ÖLDÜN... yazısı: durum ilk bakışta anlaşılsın.
            Text headline = UiText("Headline", panel.transform, "KAZANDIN!", 112, TextAnchor.MiddleCenter, Gold);
            headline.GetComponent<Outline>().effectDistance = new Vector2(4f, -4f);
            Place(headline.rectTransform, Center, Center, Center, new Vector2(0, 300), new Vector2(1600, 140));

            Image title = UiImage("Title", panel.transform, SpriteNamed(UiSheet, "Title 2"), Color.white);
            title.preserveAspect = true;
            Place(title.rectTransform, Center, Center, Center, new Vector2(0, 160), new Vector2(252, 66));

            Text stats = UiText("Stats", panel.transform, "", 42, TextAnchor.MiddleCenter, Color.white);
            Place(stats.rectTransform, Center, Center, Center, new Vector2(0, 20), new Vector2(1700, 130));

            Button restart = UiButton("Restart", panel.transform, new Vector2(-260, -170), new Vector2(460, 120));
            Stretch(UiText("Label", restart.transform, "TEKRAR OYNA  [R]", 38, TextAnchor.MiddleCenter, Gold).rectTransform);
            Button menu = UiButton("Menu", panel.transform, new Vector2(260, -170), new Vector2(460, 120));
            Stretch(UiText("Label", menu.transform, "ANA MENÜ", 38, TextAnchor.MiddleCenter, Gold).rectTransform);

            Set(view, "root", panel.gameObject);
            Set(view, "background", panel);
            Set(view, "headline", headline);
            Set(view, "confetti", confetti);
            Set(view, "title", title);
            Set(view, "defeatTitle", SpriteNamed(UiSheet, "Title 2"));  // "DEAD..."
            Set(view, "victoryTitle", SpriteNamed(UiSheet, "Title 1")); // "SURVIVED!"
            Set(view, "stats", stats);
            Set(view, "restartButton", restart);
            Set(view, "menuButton", menu);
            return view;
        }

        // ------------------------------------------------------------------ ana menü

        private static void BuildMenuUi(Font font, CharacterDefinition[] characters, AudioClip buySfx)
        {
            uiFont = font;
            Transform canvas = CreateCanvas("UI").transform;

            RectTransform holder = UiRect("MainMenu", canvas);
            Stretch(holder);
            var view = holder.gameObject.AddComponent<MainMenuView>();

            // Menü net okunsun diye çimeni karart.
            Stretch(UiImage("Dim", holder, null, new Color(0f, 0f, 0f, 0.45f)).rectTransform);

            // --- Başlık ekranı
            RectTransform main = UiRect("MainPanel", holder);
            Stretch(main);

            // Oyunun adı, piksel fontla iki satır: beyaz "PIXEL", altın "SURVIVOR".
            Text logoTop = UiText("LogoPixel", main, "PIXEL", 112, TextAnchor.MiddleCenter, Color.white);
            logoTop.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
            Place(logoTop.rectTransform, Center, Center, Center, new Vector2(0, 330), new Vector2(1400, 130));
            Text logoBottom = UiText("LogoSurvivor", main, "SURVIVOR", 144, TextAnchor.MiddleCenter, Gold);
            logoBottom.GetComponent<Outline>().effectDistance = new Vector2(6f, -6f);
            Place(logoBottom.rectTransform, Center, Center, Center, new Vector2(0, 190), new Vector2(1600, 160));

            Button start = UiButton("Start", main, new Vector2(0, -10), new Vector2(480, 100));
            Stretch(UiText("Label", start.transform, "BAŞLA", 48, TextAnchor.MiddleCenter, Gold).rectTransform);
            Button howTo = UiButton("HowTo", main, new Vector2(0, -125), new Vector2(480, 100));
            Stretch(UiText("Label", howTo.transform, "NASIL OYNANIR", 48, TextAnchor.MiddleCenter, Gold).rectTransform);
            Button shop = UiButton("Shop", main, new Vector2(0, -240), new Vector2(480, 100));
            Stretch(UiText("Label", shop.transform, "DÜKKAN", 48, TextAnchor.MiddleCenter, Gold).rectTransform);
            Button quit = UiButton("Quit", main, new Vector2(0, -355), new Vector2(480, 100));
            Stretch(UiText("Label", quit.transform, "ÇIKIŞ", 48, TextAnchor.MiddleCenter, Gold).rectTransform);

            // --- Karakter seçimi
            RectTransform select = UiRect("SelectPanel", holder);
            Stretch(select);

            Heading(select, "KARAKTERİNİ SEÇ");
            Text selectGold = UiText("Gold", select, "Altın: 0", 44, TextAnchor.MiddleRight, Gold);
            Place(selectGold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -70), new Vector2(500, 60));

            var so = new SerializedObject(view);

            // Başlık ile kartlar arasında zorluk satırı.
            SerializedProperty diffButtons = so.FindProperty("difficultyButtons");
            SerializedProperty diffLabels = so.FindProperty("difficultyLabels");
            diffButtons.arraySize = 3;
            diffLabels.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                Button b = UiButton($"Difficulty{i}", select, new Vector2(-300 + i * 300, 300), new Vector2(280, 70));
                Text label = UiText("Label", b.transform, DifficultySettings.DisplayName((Difficulty)i), 38, TextAnchor.MiddleCenter, Gold);
                Stretch(label.rectTransform);
                diffButtons.GetArrayElementAtIndex(i).objectReferenceValue = b;
                diffLabels.GetArrayElementAtIndex(i).objectReferenceValue = label;
            }

            SerializedProperty cards = so.FindProperty("cards");
            cards.arraySize = 4;

            for (int i = 0; i < 4; i++)
            {
                Button card = UiButton($"Card{i + 1}", select, new Vector2(-660 + i * 440, -50), new Vector2(400, 600));

                Image portrait = UiImage("Portrait", card.transform, null, Color.white);
                portrait.preserveAspect = true;
                Place(portrait.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(180, 200));

                Text name = UiText("Name", card.transform, "Name", 48, TextAnchor.MiddleCenter, Gold);
                Place(name.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -275), new Vector2(360, 60));

                Text desc = BodyText("Description", card.transform, "", 30, TextAnchor.UpperCenter, Color.white);
                desc.horizontalOverflow = HorizontalWrapMode.Wrap;
                Place(desc.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -330), new Vector2(330, 240));

                SerializedProperty c = cards.GetArrayElementAtIndex(i);
                c.FindPropertyRelative("button").objectReferenceValue = card;
                c.FindPropertyRelative("portrait").objectReferenceValue = portrait;
                c.FindPropertyRelative("title").objectReferenceValue = name;
                c.FindPropertyRelative("description").objectReferenceValue = desc;
            }

            Button back = UiButton("Back", select, new Vector2(0, -440), new Vector2(360, 90));
            Stretch(UiText("Label", back.transform, "GERİ  [Esc]", 38, TextAnchor.MiddleCenter, Gold).rectTransform);
            so.FindProperty("goldText").objectReferenceValue = selectGold;
            so.FindProperty("unlockSfx").objectReferenceValue = buySfx;

            // --- Dükkan ve nasıl oynanır
            RectTransform shopPanel = BuildShop(holder, buySfx, out Button shopBack);
            RectTransform howToPanel = BuildHowToPlay(holder, out Button howToBack);

            so.FindProperty("mainPanel").objectReferenceValue = main.gameObject;
            so.FindProperty("selectPanel").objectReferenceValue = select.gameObject;
            so.FindProperty("shopPanel").objectReferenceValue = shopPanel.gameObject;
            so.FindProperty("howToPanel").objectReferenceValue = howToPanel.gameObject;
            so.FindProperty("startButton").objectReferenceValue = start;
            so.FindProperty("howToButton").objectReferenceValue = howTo;
            so.FindProperty("shopButton").objectReferenceValue = shop;
            so.FindProperty("quitButton").objectReferenceValue = quit;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.FindProperty("shopBackButton").objectReferenceValue = shopBack;
            so.FindProperty("howToBackButton").objectReferenceValue = howToBack;
            SerializedProperty chars = so.FindProperty("characters");
            chars.arraySize = characters.Length;
            for (int i = 0; i < characters.Length; i++)
                chars.GetArrayElementAtIndex(i).objectReferenceValue = characters[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RectTransform BuildShop(Transform holder, AudioClip buySfx, out Button back)
        {
            RectTransform panel = UiRect("ShopPanel", holder);
            Stretch(panel);
            var view = panel.gameObject.AddComponent<ShopView>();

            Heading(panel, "DÜKKAN");
            Text gold = UiText("Gold", panel, "Altın: 0", 44, TextAnchor.MiddleRight, Gold);
            Place(gold.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -70), new Vector2(500, 60));

            var stats = new (MetaStat stat, Sprite icon)[]
            {
                (MetaStat.MaxHealth, SpriteNamed(UiSheet, "Select 8")),
                (MetaStat.Damage, SpriteNamed(UiSheet, "Select 3")),
                (MetaStat.MoveSpeed, SpriteNamed(UiSheet, "Select 7")),
                (MetaStat.PickupRadius, SpriteNamed(UiSheet, "Select 6")),
                (MetaStat.Greed, BrackeysSprite("Items & Icons/Pixel Art/Coin.png")),
            };

            var so = new SerializedObject(view);
            SerializedProperty rows = so.FindProperty("rows");
            rows.arraySize = stats.Length;
            for (int i = 0; i < stats.Length; i++)
            {
                Button row = UiButton(stats[i].stat.ToString(), panel, new Vector2(0, 250 - i * 135), new Vector2(1100, 120));

                Image icon = UiImage("Icon", row.transform, stats[i].icon, Color.white);
                icon.preserveAspect = true;
                Place(icon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), Center, new Vector2(80, 0), new Vector2(84, 84));

                Text title = UiText("Title", row.transform, "", 40, TextAnchor.LowerLeft, Gold);
                Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(1, 1), Center, Vector2.zero, Vector2.zero);
                title.rectTransform.offsetMin = new Vector2(150, 0);
                title.rectTransform.offsetMax = new Vector2(-260, -14);

                Text desc = BodyText("Description", row.transform, "", 30, TextAnchor.UpperLeft, Color.white);
                Place(desc.rectTransform, Vector2.zero, new Vector2(1, 0.5f), Center, Vector2.zero, Vector2.zero);
                desc.rectTransform.offsetMin = new Vector2(150, 14);
                desc.rectTransform.offsetMax = new Vector2(-260, -4);

                Text cost = UiText("Cost", row.transform, "", 40, TextAnchor.MiddleRight, Gold);
                Place(cost.rectTransform, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(240, 0));

                SerializedProperty r = rows.GetArrayElementAtIndex(i);
                r.FindPropertyRelative("stat").enumValueIndex = (int)stats[i].stat;
                r.FindPropertyRelative("buyButton").objectReferenceValue = row;
                r.FindPropertyRelative("title").objectReferenceValue = title;
                r.FindPropertyRelative("description").objectReferenceValue = desc;
                r.FindPropertyRelative("cost").objectReferenceValue = cost;
            }
            so.FindProperty("goldText").objectReferenceValue = gold;
            so.FindProperty("buySfx").objectReferenceValue = buySfx;
            so.ApplyModifiedPropertiesWithoutUndo();

            back = UiButton("Back", panel, new Vector2(0, -440), new Vector2(360, 100));
            Stretch(UiText("Label", back.transform, "GERİ  [Esc]", 38, TextAnchor.MiddleCenter, Gold).rectTransform);
            return panel;
        }

        private const string HowToControls =
            "<b>WASD / Ok tuşları</b>  –  Hareket\n" +
            "<b>Shift / Space</b>  –  Atıl: kısa süre hasar almazsın ve düşmanların içinden geçersin\n" +
            "<b>1 / 2 / 3</b> veya fare  –  Yükseltme seç (oyun durmaz)\n" +
            "<b>Esc</b>  –  Duraklat, ses ayarları";

        private const string HowToWeapons =
            "Silahların kendiliğinden ateş eder. Sen hareket et, kalabalıktan kaç ve XP topla!";

        private const string HowToGoal =
            "Düşmanların düşürdüğü XP taşlarını topla, seviye atla ve her seviyede bir yükseltme seç.\n" +
            "Her dakika bir <b>BOSS</b> gelir: yen ve düşürdüğü hazine sandığını al.\n" +
            "8:00'de gelen <b>SON BOSS</b>'u yenersen oyunu kazanırsın!";

        private const string HowToTips =
            "• Altın renkli düşmanlar <b>elit</b>tir, daha çok ödül verir.\n" +
            "• Topladığın altınla <b>Dükkan</b>'dan kalıcı güç al.\n" +
            "• Bir silahın iki yükseltmesini doldur: <b>EVRİM!</b>";

        private const string Credits =
            "Görseller / sesler: Undead Survivor (Goldmetal), Monsters Creatures Fantasy (Luiz Melo), " +
            "Brackeys 2D Mega Pack, Dragon Warrior (Graphictoon), Horror Atmosphere LITE, GUI Parts, Neo둥근모 font";

        private static RectTransform BuildHowToPlay(Transform holder, out Button back)
        {
            RectTransform panel = UiRect("HowToPanel", holder);
            Stretch(panel);
            Heading(panel, "NASIL OYNANIR");

            Image window = Window(panel, new Vector2(1620, 700));
            window.rectTransform.anchoredPosition = new Vector2(0, 0);

            // Sol sütun: kontroller + silahlar. Sağ sütun: amaç + ipuçları.
            // Bölüm başlıkları piksel fontta, paragraflar okunaklı sade fontta.
            HowToSection(window.transform, "KONTROLLER", HowToControls, new Vector2(80, -60), 300);
            HowToSection(window.transform, "SİLAHLAR", HowToWeapons, new Vector2(80, -450), 140);
            HowToSection(window.transform, "AMAÇ", HowToGoal, new Vector2(850, -60), 300);
            HowToSection(window.transform, "İPUÇLARI", HowToTips, new Vector2(850, -400), 220);

            Text credits = BodyText("Credits", panel, Credits, 22, TextAnchor.MiddleCenter, Muted);
            credits.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(credits.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 6), new Vector2(1800, 40));

            back = UiButton("Back", panel, new Vector2(0, -420), new Vector2(360, 80));
            Stretch(UiText("Label", back.transform, "GERİ  [Esc]", 38, TextAnchor.MiddleCenter, Gold).rectTransform);
            return panel;
        }

        /// <summary>Altın renkli piksel font başlık ve altında satır kaydıran bir paragraf (konumlar pencerenin sol üstüne göre).</summary>
        private static void HowToSection(Transform window, string title, string body, Vector2 topLeft, float bodyHeight)
        {
            var anchor = new Vector2(0, 1);
            Text heading = UiText(title, window, title, 38, TextAnchor.UpperLeft, Gold);
            Place(heading.rectTransform, anchor, anchor, anchor, topLeft, new Vector2(660, 44));

            Text text = BodyText(title + "Text", window, body, 33, TextAnchor.UpperLeft, Color.white);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Place(text.rectTransform, anchor, anchor, anchor, topLeft + new Vector2(0, -52), new Vector2(660, bodyHeight));
        }

        private static void Heading(Transform parent, string text)
        {
            Text heading = UiText("Heading", parent, text, 64, TextAnchor.MiddleCenter, Gold);
            Place(heading.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(1200, 100));
        }

        // ------------------------------------------------------------------ UI yardımcı metotları

        private static readonly Vector2 Center = new(0.5f, 0.5f);

        private static RectTransform UiRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
                                  Vector2 position, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = position;
            rt.sizeDelta = size;
        }

        private static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Center;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        /// <summary>GUI_Parts arka planı + süslü çerçeve, ortalanmış.</summary>
        private static Image Window(Transform parent, Vector2 size)
        {
            Image window = UiImage("Window", parent, GuiSprite("mid_background"), Color.white, Image.Type.Sliced, 1f);
            Place(window.rectTransform, Center, Center, Center, Vector2.zero, size);
            Stretch(UiImage("Frame", window.transform, GuiSprite("Frame_mid"), Color.white, Image.Type.Sliced, 1.5f).rectTransform);
            return window;
        }

        private static Image UiImage(string name, Transform parent, Sprite sprite, Color color,
                                     Image.Type type = Image.Type.Simple, float pixelsPerUnitMultiplier = 1f)
        {
            var img = UiRect(name, parent).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = type;
            img.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            img.raycastTarget = false;
            return img;
        }

        private static void MakeFilled(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
        }

        /// <summary>
        /// Unity'nin yerleşik sans-serif fontu: her boyutta okunur ve bütün Türkçe harfleri içerir.
        /// Uzun metinlerde kullanılır; başlıklar ve butonlar piksel fontta kalır.
        /// </summary>
        private static Font BodyFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        /// <summary>Okunaklı sade fontta paragraf metni (bkz. <see cref="BodyFont"/>).</summary>
        private static Text BodyText(string name, Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            Text t = UiText(name, parent, text, size, anchor, color);
            t.font = BodyFont;
            t.fontStyle = FontStyle.Normal;
            t.supportRichText = true;
            t.lineSpacing = 1.1f;
            t.GetComponent<Outline>().effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        /// <summary>Piksel font sadece bu boyutta ve üstünde kullanılır; altında, arayüz küçüldüğünde bulanıklaşıyor.</summary>
        private const int PixelFontMinSize = 48;

        /// <summary>
        /// Etiket metni. Büyük başlıklar stil için piksel fontu kullanır; daha küçük her şey kalın sade fontu kullanır,
        /// böylece 1920x1080 arayüz küçük bir pencerede ya da dizüstünde gösterilse bile okunur kalır.
        /// </summary>
        private static Text UiText(string name, Transform parent, string text, int size, TextAnchor anchor, Color color)
        {
            var t = UiRect(name, parent).gameObject.AddComponent<Text>();
            bool pixel = size >= PixelFontMinSize;
            t.font = pixel ? uiFont : BodyFont;
            t.fontStyle = pixel ? FontStyle.Normal : FontStyle.Bold;
            t.text = text;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;

            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            return t;
        }

        /// <summary>GUI_Parts butonu; üstüne gelince/basılınca parlak sprite'a geçer ve UI sesleri çalar.</summary>
        private static Button UiButton(string name, Transform parent, Vector2 position, Vector2 size)
        {
            Image bg = UiImage(name, parent, GuiSprite("button_ready_off"), Color.white, Image.Type.Sliced, 2f);
            bg.raycastTarget = true;
            Place(bg.rectTransform, Center, Center, Center, position, size);

            var button = bg.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.SpriteSwap;
            Sprite lit = GuiSprite("button_ready_on");
            button.spriteState = new SpriteState { highlightedSprite = lit, pressedSprite = lit, selectedSprite = bg.sprite };
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            if (uiClickSfx != null || uiHoverSfx != null)
            {
                var sfx = bg.gameObject.AddComponent<ButtonSfx>();
                var so = new SerializedObject(sfx);
                so.FindProperty("clickSfx").objectReferenceValue = uiClickSfx;
                so.FindProperty("hoverSfx").objectReferenceValue = uiHoverSfx;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return button;
        }

        /// <summary>0-1 yatay kaydırıcı: koyu iz, altın dolgu, küçük tutamak.</summary>
        private static Slider UiSlider(string name, Transform parent, Vector2 position, Vector2 size)
        {
            RectTransform root = UiRect(name, parent);
            Place(root, Center, Center, Center, position, size);

            Image track = UiImage("Background", root, null, new Color(0f, 0f, 0f, 0.6f));
            track.raycastTarget = true; // ize tıklayınca tutamak oraya atlar
            Stretch(track.rectTransform);
            track.rectTransform.anchorMin = new Vector2(0f, 0.3f);
            track.rectTransform.anchorMax = new Vector2(1f, 0.7f);

            RectTransform fillArea = UiRect("Fill Area", root);
            Stretch(fillArea);
            fillArea.anchorMin = new Vector2(0f, 0.3f);
            fillArea.anchorMax = new Vector2(1f, 0.7f);
            Image fill = UiImage("Fill", fillArea, null, Gold);
            Stretch(fill.rectTransform);

            RectTransform handleArea = UiRect("Handle Slide Area", root);
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(12f, 0f);
            handleArea.offsetMax = new Vector2(-12f, 0f);
            Image handle = UiImage("Handle", handleArea, null, Color.white);
            handle.raycastTarget = true;
            handle.rectTransform.sizeDelta = new Vector2(24f, 0f);

            var slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.navigation = new Navigation { mode = Navigation.Mode.None };
            return slider;
        }
    }
}
