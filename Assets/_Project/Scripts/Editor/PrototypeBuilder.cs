using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace PixelSurvivor.EditorTools
{
    /// <summary>
    /// One-click setup: layers, sorting, prefabs, upgrade assets, UI and the Game scene,
    /// wired to the Undead Survivor + GUI_Parts art. Safe to run again: prefabs and the
    /// scene are regenerated, upgrade assets are only created if missing (your tweaks stay).
    /// </summary>
    public static partial class PrototypeBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PrefabDir = Root + "/Prefabs";
        private const string SceneDir = Root + "/Scenes";
        private const string DataDir = Root + "/Data";
        private const string UpgradeDir = DataDir + "/Upgrades";
        private const string CharacterDir = DataDir + "/Characters";
        private const string EnemyDataDir = DataDir + "/Enemies";
        private const string ScenePath = SceneDir + "/Game.unity";
        private const string MenuScenePath = SceneDir + "/MainMenu.unity";

        private const string Art = "Assets/Undead Survivor";
        private const string Props = Art + "/Sprites/Props.png";
        private const string UiSheet = Art + "/Sprites/UI.png";
        private const string Gui = "Assets/GUI_Parts/Gui_parts";
        private const string Brackeys = "Assets/Brackeys/2D Mega Pack";
        private const string Horror = "Assets/Horror Atmosphere LITE";
        private const string DragonFx = "Assets/Dragon Warrior Files/Effects";
        private const string SceneTemplate = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
        private const string Renderer2DPath = "Assets/Settings/Renderer2D.asset";

        /// <summary>Bump when the builder changes, so the Console shows which version ran.</summary>
        private const int BuilderVersion = 17;

        [MenuItem("PixelSurvivor/Build Prototype", priority = 0)]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("PixelSurvivor", "Exit Play mode first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Debug.Log($"[PixelSurvivor] Build Prototype v{BuilderVersion} starting...");

            try
            {
                EnsureFolders();
                EnsureLayers();
                ConfigureYSorting();
                ConfigureGuiSprites();
                ConfigurePixelFont();

                BuildUpgradeAssets();
                BuildCharacterAssets();
                BuildMonsterAnimations();

                PhysicsMaterial2D noFriction = GetOrCreateNoFriction();
                Projectile projectile = BuildProjectilePrefab();
                Projectile axe = BuildAxePrefab();
                BuildEnemyProjectilePrefab();
                OrbitBlade blade = BuildOrbitBladePrefab();
                BuildPickupPrefab();
                BuildEnemyPrefab(noFriction);
                BuildPlayerPrefab(projectile, axe, blade, noFriction);
                AssetDatabase.SaveAssets();

                // Each scene build loads what it needs fresh from disk after opening the scene.
                BuildScene();
                BuildMenuScene();
                SetBuildScenes(MenuScenePath, ScenePath);

                AssetDatabase.SaveAssets();
                Debug.Log($"[PixelSurvivor] Prototype built. {MenuScenePath} is open: press Play to start from the menu.");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("PixelSurvivor", "Build failed, see the Console.\n\n" + e.Message, "OK");
            }
        }

        // ------------------------------------------------------------------ project setup

        private static void EnsureFolders()
        {
            foreach (string dir in new[] { PrefabDir, SceneDir, DataDir, UpgradeDir, CharacterDir, EnemyDataDir })
            {
                if (AssetDatabase.IsValidFolder(dir)) continue;
                int slash = dir.LastIndexOf('/');
                AssetDatabase.CreateFolder(dir[..slash], dir[(slash + 1)..]);
            }
        }

        private static void EnsureLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");

            foreach (string layerName in GameLayers.All)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize && !exists; i++)
                    exists = layers.GetArrayElementAtIndex(i).stringValue == layerName;
                if (exists) continue;

                int slot = -1;
                for (int i = 6; i < layers.arraySize && slot < 0; i++) // 0-5 are reserved by Unity
                    if (string.IsNullOrEmpty(layers.GetArrayElementAtIndex(i).stringValue)) slot = i;
                if (slot < 0) throw new InvalidOperationException("No free user layer for " + layerName);

                layers.GetArrayElementAtIndex(slot).stringValue = layerName;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Lower sprites draw in front of higher ones (top-down depth).</summary>
        private static void ConfigureYSorting()
        {
            Object renderer = AssetDatabase.LoadMainAssetAtPath(Renderer2DPath);
            if (renderer == null)
            {
                Debug.LogWarning("[PixelSurvivor] Renderer2D.asset not found; Y-sorting not configured.");
                return;
            }

            var so = new SerializedObject(renderer);
            so.FindProperty("m_TransparencySortMode").intValue = (int)TransparencySortMode.CustomAxis;
            so.FindProperty("m_TransparencySortAxis").vector3Value = new Vector3(0f, 1f, 0f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>9-slice borders (in source pixels) so frames and buttons stretch without distorting.</summary>
        private static void ConfigureGuiSprites()
        {
            var borders = new Dictionary<string, float>
            {
                { "button_ready_off", 52f },
                { "button_ready_on", 52f },
                { "Frame_mid", 150f },
                { "mid_background", 60f },
                { "Hp_frame", 16f },
            };

            foreach (var (file, b) in borders.Select(kv => (kv.Key, kv.Value)))
            {
                string path = $"{Gui}/{file}.png";
                if (AssetImporter.GetAtPath(path) is not TextureImporter ti)
                    throw new InvalidOperationException("Texture not found: " + path);

                var border = new Vector4(b, b, b, b);
                if (ti.textureType == TextureImporterType.Sprite && ti.spriteBorder == border && !ti.mipmapEnabled) continue;

                ti.textureType = TextureImporterType.Sprite;
                ti.spriteImportMode = SpriteImportMode.Single;
                ti.mipmapEnabled = false;
                ti.spriteBorder = border;
                ti.SaveAndReimport();
            }
        }

        /// <summary>
        /// neodgm is a pixel font: render it without anti-aliasing so text stays crisp
        /// (the default smooth mode blurs it, especially at sizes that aren't multiples of 16).
        /// </summary>
        private static void ConfigurePixelFont()
        {
            string path = Art + "/Fonts/neodgm.ttf";
            if (AssetImporter.GetAtPath(path) is not TrueTypeFontImporter importer)
                throw new InvalidOperationException("Font not found: " + path);
            if (importer.fontRenderingMode == FontRenderingMode.HintedRaster) return;

            importer.fontRenderingMode = FontRenderingMode.HintedRaster;
            importer.SaveAndReimport();
        }

        private static PhysicsMaterial2D GetOrCreateNoFriction()
        {
            string path = DataDir + "/NoFriction.physicsMaterial2D";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (mat != null) return mat;

            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ------------------------------------------------------------------ prefabs

        private static Projectile BuildProjectilePrefab()
        {
            var go = new GameObject("Projectile") { layer = Layer(GameLayers.PlayerProjectile) };

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt(Props, 31, 22); // small fireball, points up
            sr.sortingOrder = 5;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.15f;

            go.AddComponent<Projectile>();
            return SavePrefab(go, "Projectile").GetComponent<Projectile>();
        }

        private static OrbitBlade BuildOrbitBladePrefab()
        {
            var go = new GameObject("OrbitBlade") { layer = Layer(GameLayers.PlayerProjectile) };

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt(Props, 0, 13); // shovel, blade pointing up
            sr.sortingOrder = 4;

            // Own kinematic body so its trigger isn't merged into the player's collider.
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.35f, 0.95f);

            go.AddComponent<OrbitBlade>();
            return SavePrefab(go, "OrbitBlade").GetComponent<OrbitBlade>();
        }

        private static void BuildPickupPrefab()
        {
            var go = new GameObject("Pickup"); // scaled per sprite at runtime (Pickup.Init)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt(Props, 0, 0);
            sr.sortingOrder = -1; // on the ground: above shadows, below characters

            var pickup = go.AddComponent<Pickup>();
            Set(pickup, "spriteRenderer", sr);
            Set(pickup, "goldSfx", BrackeysSound("Bonus.wav"));
            Set(pickup, "healthSfx", Load<AudioClip>(Art + "/Audio/Select.wav"));
            Set(pickup, "magnetSfx", BrackeysSound("Whoosh.wav"));
            Set(pickup, "chestSfx", BrackeysSound("Spawn.wav"));
            SavePrefab(go, "Pickup");
        }

        private static void BuildEnemyPrefab(PhysicsMaterial2D material)
        {
            var go = new GameObject("Enemy") { layer = Layer(GameLayers.Enemy) };

            var rb = go.AddComponent<Rigidbody2D>();
            ConfigureTopDownBody(rb, mass: 1f, material);

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.35f;
            col.offset = new Vector2(0f, -0.1f);

            go.AddComponent<Health>();

            AddShadow(go.transform, -0.45f);
            var body = CreateChild(go.transform, "Body", go.layer);
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = SpritesNamed(Art + "/Sprites/Enemy 0.png", "Run")[0];
            var animator = body.AddComponent<Animator>();
            animator.runtimeAnimatorController = Load<RuntimeAnimatorController>(Art + "/Animations/Enemy/AcEnemy 0.controller");

            var enemy = go.AddComponent<Enemy>();
            Set(enemy, "body", sr);
            Set(enemy, "animator", animator);
            Set(enemy, "hitSfx", Load<AudioClip>(Art + "/Audio/Hit0.wav"));
            Set(enemy, "deathSfx", Load<AudioClip>(Art + "/Audio/Dead.wav"));

            SavePrefab(go, "Enemy");
        }

        private static void BuildPlayerPrefab(Projectile projectile, Projectile axeProjectile, OrbitBlade blade,
                                              PhysicsMaterial2D material)
        {
            var go = new GameObject("Player") { layer = Layer(GameLayers.Player), tag = "Player" };

            var rb = go.AddComponent<Rigidbody2D>();
            ConfigureTopDownBody(rb, mass: 5f, material);
            rb.sleepMode = RigidbodySleepMode2D.NeverSleep; // keep contact-damage callbacks coming

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = 0.3f;
            col.offset = new Vector2(0f, -0.25f);

            var health = go.AddComponent<Health>();
            health.SetMax(100f);

            var xp = go.AddComponent<PlayerExperience>();
            Set(xp, "levelUpSfx", Load<AudioClip>(Art + "/Audio/LevelUp.wav"));

            AddShadow(go.transform, -0.55f);
            var body = CreateChild(go.transform, "Body", go.layer);
            var sr = body.AddComponent<SpriteRenderer>();
            string farmer = Art + "/Sprites/Farmer 0.png";
            Sprite[] stand = SpritesNamed(farmer, "Stand");
            sr.sprite = stand[0];

            var anim = body.AddComponent<SpriteFrameAnimator>();
            var animSo = new SerializedObject(anim);
            animSo.FindProperty("target").objectReferenceValue = sr;
            SerializedProperty clips = animSo.FindProperty("clips");
            clips.arraySize = 3;
            SetClip(clips.GetArrayElementAtIndex(0), "Stand", stand, 6f, true);
            SetClip(clips.GetArrayElementAtIndex(1), "Run", SpritesNamed(farmer, "Run"), 10f, true);
            SetClip(clips.GetArrayElementAtIndex(2), "Dead", SpritesNamed(farmer, "Dead"), 4f, false);
            animSo.ApplyModifiedPropertiesWithoutUndo();

            var controller = go.AddComponent<PlayerController>();
            Set(controller, "body", sr);
            Set(controller, "frameAnimator", anim);

            var loadout = go.AddComponent<PlayerLoadout>();
            Set(loadout, "defaultCharacter", Load<CharacterDefinition>($"{CharacterDir}/{CharacterTable[0].file}.asset"));

            AddWorldHealthBar(go.transform, health);

            var gun = CreateChild(go.transform, "AutoGun", go.layer).AddComponent<AutoGun>();
            Set(gun, "projectilePrefab", projectile);
            Set(gun, "fireSfx", Load<AudioClip>(Art + "/Audio/Range.wav"));

            var ring = CreateChild(go.transform, "RingShooter", go.layer).AddComponent<RingShooter>();
            Set(ring, "projectilePrefab", projectile);
            Set(ring, "fireSfx", Load<AudioClip>(Art + "/Audio/Melee1.wav"));

            var orbit = CreateChild(go.transform, "OrbitWeapon", go.layer).AddComponent<OrbitWeapon>();
            Set(orbit, "bladePrefab", blade);
            Set(orbit, "hitSfx", Load<AudioClip>(Art + "/Audio/Melee0.wav"));

            // Weapons unlocked by upgrades (start with zero).
            var axe = CreateChild(go.transform, "AxeThrower", go.layer).AddComponent<AxeThrower>();
            Set(axe, "axePrefab", axeProjectile);
            Set(axe, "throwSfx", BrackeysSound("Whoosh.wav"));

            var bomb = CreateChild(go.transform, "BombThrower", go.layer).AddComponent<BombThrower>();
            Sprite[] blast = BrackeysSlices("Effects/Explosion.png");
            SetArray(bomb, "explosionFrames", blast);
            var bombSo = new SerializedObject(bomb);
            bombSo.FindProperty("explosionSpriteWidth").floatValue = blast[^1].bounds.size.x;
            bombSo.ApplyModifiedPropertiesWithoutUndo();
            Set(bomb, "explosionSfx", BrackeysSound("Explosion.wav"));

            Set(controller, "hurtSfx", BrackeysSound("Hit.wav"));
            Set(controller, "deathSfx", BrackeysSound("DeathVoice.wav"));
            Set(controller, "dashSfx", BrackeysSound("Whoosh.wav"));
            SetArray(controller, "dashEffectFrames",
                Enumerable.Range(1, 6).Select(i => (Object)Load<Sprite>($"{DragonFx}/dashwind_0{i}.png")).ToArray());

            SavePrefab(go, "Player");
        }

        /// <summary>Fireball shot by ranged enemies (Brackeys Fireball, 2-frame flicker).</summary>
        private static void BuildEnemyProjectilePrefab()
        {
            var go = new GameObject("EnemyProjectile") { layer = Layer(GameLayers.EnemyProjectile) };

            Sprite[] frames = BrackeysSlices("Effects/Fireball.png");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 7;
            go.transform.localScale = Vector3.one * (frames[0].pixelsPerUnit / 18f * 0.55f); // ~0.7 units tall

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.22f / go.transform.localScale.x; // ~0.22 world units

            var projectile = go.AddComponent<EnemyProjectile>();
            Set(projectile, "spriteRenderer", sr);
            SetArray(projectile, "frames", frames);
            SavePrefab(go, "EnemyProjectile");
        }

        /// <summary>Spinning axe that is thrown up and falls back down (Brackeys Axe sprite).</summary>
        private static Projectile BuildAxePrefab()
        {
            var go = new GameObject("AxeProjectile") { layer = Layer(GameLayers.PlayerProjectile) };

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = BrackeysSprite("Weapons & Tools/Axe.png");
            sr.sortingOrder = 6;
            // Match the Undead Survivor pixel size (18 px per unit).
            go.transform.localScale = Vector3.one * (sr.sprite.pixelsPerUnit / 18f);

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f / go.transform.localScale.x; // ~0.4 world units

            var projectile = go.AddComponent<Projectile>();
            var so = new SerializedObject(projectile);
            so.FindProperty("lifetime").floatValue = 2.5f;
            so.FindProperty("gravity").floatValue = 16f;
            so.FindProperty("spinSpeed").floatValue = -720f;
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(go, "AxeProjectile").GetComponent<Projectile>();
        }

        private static void AddWorldHealthBar(Transform parent, Health health)
        {
            var bar = CreateChild(parent, "HealthBar", parent.gameObject.layer);
            bar.transform.localPosition = new Vector3(0f, -0.85f, 0f);
            var scale = new Vector3(2.2f, 0.5f, 1f); // 8x8 px sprites -> ~1.0 x 0.22 units

            var back = CreateChild(bar.transform, "Back", bar.layer).AddComponent<SpriteRenderer>();
            back.sprite = SpriteNamed(UiSheet, "Back 1");
            back.sortingOrder = 20;
            back.transform.localScale = scale;

            var fill = CreateChild(bar.transform, "Fill", bar.layer).AddComponent<SpriteRenderer>();
            fill.sprite = SpriteNamed(UiSheet, "Front 1");
            fill.sortingOrder = 21;
            fill.transform.localScale = scale;

            var view = bar.AddComponent<WorldHealthBar>();
            Set(view, "health", health);
            Set(view, "fill", fill);
        }

        private static void ConfigureTopDownBody(Rigidbody2D rb, float mass, PhysicsMaterial2D material)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.mass = mass;
            rb.linearDamping = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.sharedMaterial = material;
        }

        private static void AddShadow(Transform parent, float y)
        {
            var shadow = CreateChild(parent, "Shadow", parent.gameObject.layer);
            shadow.transform.localPosition = new Vector3(0f, y, 0f);
            var sr = shadow.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAt(Props, 37, 25);
            sr.sortingOrder = -2;
        }

        private static void SetClip(SerializedProperty clip, string name, Sprite[] frames, float fps, bool loop)
        {
            clip.FindPropertyRelative("name").stringValue = name;
            clip.FindPropertyRelative("fps").floatValue = fps;
            clip.FindPropertyRelative("loop").boolValue = loop;
            SerializedProperty f = clip.FindPropertyRelative("frames");
            f.arraySize = frames.Length;
            for (int i = 0; i < frames.Length; i++)
                f.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }

        // ------------------------------------------------------------------ upgrades

        private static readonly (string file, string name, string desc, string icon, UpgradeKind kind, float value, int max, float weight)[] UpgradeTable =
        {
            ("GunDamage",    "Ateş Gücü",         "Tüfek hasarı +%25",               "Select 3", UpgradeKind.GunDamage,      0.25f, 5, 1f),
            ("GunFireRate",  "Seri Atış",         "Ateş aralığı %12 kısalır",        "Select 4", UpgradeKind.GunFireRate,    0.12f, 5, 1f),
            ("GunMultishot", "Saçma",             "Her atışta +1 mermi",             "Select 5", UpgradeKind.GunProjectiles, 1f,    4, 1f),
            ("GunPierce",    "Delici Mermi",      "Mermiler +1 düşmanı delip geçer", "Select 9", UpgradeKind.GunPierce,      1f,    3, 0.8f),
            ("OrbitShovel",  "Dönen Kürek",       "Etrafında dönen +1 kürek",        "Select 0", UpgradeKind.OrbitCount,     1f,    6, 1.3f),
            ("OrbitPower",   "Keskin Kürek",      "Kürek hızı ve hasarı +%20",       "Select 2", UpgradeKind.OrbitPower,     0.2f,  4, 1f),
            ("MoveSpeed",    "Hızlı Botlar",      "Hareket hızı +%10",               "Select 7", UpgradeKind.MoveSpeed,      0.1f,  5, 0.8f),
            ("MaxHealth",    "Dayanıklılık",      "Maksimum can +20",                "Select 8", UpgradeKind.MaxHealth,      20f,   5, 0.8f),
            ("PickupRadius", "Toplayıcı Eldiven", "XP toplama alanı +%30",           "Select 6", UpgradeKind.PickupRadius,   0.3f,  4, 0.7f),
            ("RingCount",    "Ateş Çemberi",      "Çember atışına +2 ateş topu",     "Props@31,22", UpgradeKind.RingCount,   2f,    5, 1.2f),
            ("RingFireRate", "Alev Dalgası",      "Çember atış aralığı %15 kısalır", "Props@26,15", UpgradeKind.RingFireRate, 0.15f, 4, 1f),
            ("RingDamage",   "Kızgın Kor",        "Çember ateş topları +%25 hasar",  "B:Items & Icons/Pixel Art/Fire.png", UpgradeKind.RingDamage, 0.25f, 5, 1f),
            ("AxeCount",     "Fırlatma Baltası",  "Havaya +1 dönen balta fırlatır",  "B:Weapons & Tools/Axe.png", UpgradeKind.AxeCount, 1f, 5, 1.2f),
            ("AxePower",     "Keskin Balta",      "Balta hasarı +%30",               "B:Weapons & Tools/Pickaxe.png", UpgradeKind.AxePower, 0.3f, 4, 1f),
            ("BombCount",    "Alev Bombası",      "Düşmanların arasına +1 bomba",    "B:Effects/Explosion.png#3", UpgradeKind.BombCount, 1f, 5, 1.2f),
            ("BombRadius",   "Büyük Patlama",     "Bomba patlama alanı +%20",        "B:Effects/Explosion.png#2", UpgradeKind.BombRadius, 0.2f, 4, 1f),
            // Evolutions (max level 1): offered only once all upgrades in UpgradeLinks' "maxed" list are maxed.
            ("EvolveGun",    "EJDERHA TÜFEĞİ",    "Tüfek hasarı x2, +5 delme, %30 daha hızlı", "B:Items & Icons/Pixel Art/Crown.png", UpgradeKind.EvolveGun, 1f, 1, 3f),
            ("EvolveRing",   "CEHENNEM ÇEMBERİ",  "+8 ateş topu, x1.5 hasar, %40 daha sık",    "B:Items & Icons/Pixel Art/MagicEssence.png", UpgradeKind.EvolveRing, 1f, 1, 3f),
            ("EvolveOrbit",  "KÜREK FIRTINASI",   "+2 kürek, x2 hasar, daha geniş ve hızlı",   "B:Items & Icons/Pixel Art/World Ender.png", UpgradeKind.EvolveOrbit, 1f, 1, 3f),
        };

        /// <summary>Which upgrades unlock others: "requires" = owned once, "maxed" = all at max level.</summary>
        private static readonly (string file, string requires, string[] maxed, bool evolution)[] UpgradeLinks =
        {
            ("OrbitPower",  "OrbitShovel", null, false),
            ("AxePower",    "AxeCount",    null, false),
            ("BombRadius",  "BombCount",   null, false),
            ("EvolveGun",   null, new[] { "GunDamage", "GunMultishot" }, true),
            ("EvolveRing",  null, new[] { "RingCount", "RingFireRate" }, true),
            ("EvolveOrbit", null, new[] { "OrbitShovel", "OrbitPower" }, true),
        };

        private const string FallbackUpgradeFile = "Heal";

        private static void BuildUpgradeAssets()
        {
            foreach (var u in UpgradeTable)
                GetOrCreateUpgrade(u.file, u.name, u.desc, u.icon, u.kind, u.value, u.max, u.weight);

            GetOrCreateUpgrade(FallbackUpgradeFile, "Şifa", "30 can yeniler", "Select 8", UpgradeKind.Heal, 30f, 0, 1f);

            // Links are only filled in when empty, so changes made in the Inspector are kept.
            foreach (var link in UpgradeLinks)
            {
                var def = Load<UpgradeDefinition>($"{UpgradeDir}/{link.file}.asset");
                bool changed = false;
                if (link.requires != null && def.requires == null)
                {
                    def.requires = Load<UpgradeDefinition>($"{UpgradeDir}/{link.requires}.asset");
                    changed = true;
                }
                if (link.maxed != null && (def.requiresMaxed == null || def.requiresMaxed.Length == 0))
                {
                    def.requiresMaxed = link.maxed.Select(f => Load<UpgradeDefinition>($"{UpgradeDir}/{f}.asset")).ToArray();
                    changed = true;
                }
                if (link.evolution && !def.isEvolution)
                {
                    def.isEvolution = true;
                    changed = true;
                }
                if (changed) EditorUtility.SetDirty(def);
            }
        }

        private static void GetOrCreateUpgrade(string file, string name, string desc, string icon,
                                               UpgradeKind kind, float value, int max, float weight)
        {
            string path = $"{UpgradeDir}/{file}.asset";
            if (AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path) != null) return; // keep user tweaks

            var def = ScriptableObject.CreateInstance<UpgradeDefinition>();
            def.displayName = name;
            def.description = desc;
            def.icon = ResolveIcon(icon);
            def.kind = kind;
            def.value = value;
            def.maxLevel = max;
            def.weight = weight;
            AssetDatabase.CreateAsset(def, path);
        }

        // ------------------------------------------------------------------ characters

        private static readonly (string file, string name, string desc, int sheet, float hp, float speed, float pickup,
            bool gun, int gunShots, bool ring, int ringCount, float ringInterval, int orbit)[] CharacterTable =
        {
            ("Hunter",   "Avcı",     "Tüfek + Ateş Çemberi.\nDengeli başlangıç.\n\nCan 100",
                0, 100f, 3.0f, 2.5f, true,  1, true,   8, 1.4f, 0),
            ("Gardener", "Bahçıvan", "Tüfek + 3 Dönen Kürek.\nSağlam ama yavaş.\n\nCan 130",
                1, 130f, 2.7f, 2.5f, true,  1, false,  8, 1.4f, 3),
            ("Firestarter", "Ateşçi", "Güçlü Ateş Çemberi (12 top).\nTüfeği yok, cam gibi.\n\nCan 80",
                2,  80f, 3.0f, 2.5f, false, 1, true,  12, 1.1f, 0),
            ("Wanderer", "Gezgin",   "Çift namlulu tüfek.\nÇok hızlı, geniş toplama alanı.\n\nCan 90",
                3,  90f, 3.6f, 3.5f, true,  2, false,  8, 1.4f, 0),
        };

        // How each character is unlocked: gold cost, or beating the final boss once. Not listed = free.
        private static readonly Dictionary<string, (int cost, bool victory)> CharacterUnlocks = new()
        {
            { "Gardener", (100, false) },
            { "Firestarter", (200, false) },
            { "Wanderer", (0, true) },
        };

        /// <summary>Character assets are created once; edits made in the Inspector are kept.</summary>
        private static void BuildCharacterAssets()
        {
            foreach (var c in CharacterTable)
            {
                string path = $"{CharacterDir}/{c.file}.asset";
                var existing = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (existing != null)
                {
                    // Assets made before unlocks existed: fill the lock in once (free = untouched).
                    if (existing.unlockCost == 0 && !existing.requiresVictory &&
                        CharacterUnlocks.TryGetValue(c.file, out var lockInfo))
                    {
                        existing.unlockCost = lockInfo.cost;
                        existing.requiresVictory = lockInfo.victory;
                        EditorUtility.SetDirty(existing);
                    }
                    continue;
                }

                string sheet = $"{Art}/Sprites/Farmer {c.sheet}.png";
                var def = ScriptableObject.CreateInstance<CharacterDefinition>();
                def.displayName = c.name;
                def.description = c.desc;
                def.standFrames = SpritesNamed(sheet, "Stand");
                def.runFrames = SpritesNamed(sheet, "Run");
                def.deadFrames = SpritesNamed(sheet, "Dead");
                def.maxHealth = c.hp;
                def.moveSpeed = c.speed;
                def.pickupRadius = c.pickup;
                def.gun = c.gun;
                def.gunProjectiles = c.gunShots;
                def.ring = c.ring;
                def.ringCount = c.ringCount;
                def.ringInterval = c.ringInterval;
                def.orbitBlades = c.orbit;
                if (CharacterUnlocks.TryGetValue(c.file, out var unlock))
                {
                    def.unlockCost = unlock.cost;
                    def.requiresVictory = unlock.victory;
                }
                AssetDatabase.CreateAsset(def, path);
            }
        }

        private static CharacterDefinition[] LoadCharacters() =>
            CharacterTable.Select(c => Load<CharacterDefinition>($"{CharacterDir}/{c.file}.asset")).ToArray();

        // ------------------------------------------------------------------ monster animations

        private const string MonsterArt = "Assets/Monsters Creatures Fantasy/Sprites";

        // key, folder in the pack, name of the movement sheet
        private static readonly (string key, string folder, string move)[] MonsterTable =
        {
            ("Goblin", "Goblin", "Run"),
            ("Mushroom", "Mushroom", "Run"),
            ("FlyingEye", "Flying eye", "Flight"),
            ("Skeleton", "Skeleton", "Walk"),
        };

        /// <summary>
        /// Builds Run/Hit/Dead clips from the monster sprite sheets and an override of the Undead
        /// enemy animator, so the monsters use the same Hit trigger / Dead bool as everything else.
        /// </summary>
        // Brackeys walk cycles: no hit/death frames, so those states reuse the first frame
        // (the enemy flashes red on hit and fades out on death anyway).
        private static readonly (string key, string sheet)[] SimpleMonsterTable =
        {
            ("Beetle", "Enemies/Insects/Beetle.png"),
            ("Hooded", "Enemies/Gothic/GothicEnemy01.png"),
            ("Cultist", "Enemies/Gothic/GothicEnemy02.png"),
            ("Firehead", "Enemies/Gothic/FireheadEnemy.png"),
            ("Warlock", "Enemies/Gothic/GothicEnemy03.png"),
        };

        /// <summary>Enemies (by name in EnemyTable / BossTable) that shoot: range, interval, damage, shot speed.</summary>
        private static readonly Dictionary<string, (float range, float interval, float damage, float speed)> RangedStats = new()
        {
            { "Firehead", (7f, 2.2f, 10f, 6f) },
            { "Warlock", (8.5f, 3f, 18f, 5f) },
            { "Boss: Bone Lord", (8f, 2f, 18f, 5.5f) },
            { "Final Boss: Great Eye", (10f, 1.4f, 18f, 6.5f) },
        };

        private static void BuildMonsterAnimations()
        {
            foreach (var m in MonsterTable)
            {
                string dir = $"{MonsterArt}/{m.folder}";
                SaveMonsterController(m.key,
                    SpritesIn($"{dir}/{m.move}.png"), SpritesIn($"{dir}/Take Hit.png"), SpritesIn($"{dir}/Death.png"));
            }

            foreach (var m in SimpleMonsterTable)
            {
                Sprite[] walk = BrackeysSlices(m.sheet);
                SaveMonsterController(m.key, walk, new[] { walk[0] }, new[] { walk[0] }, runFps: 8f);
            }
        }

        private static void SaveMonsterController(string key, Sprite[] runFrames, Sprite[] hitFrames, Sprite[] deadFrames,
                                                  float runFps = 12f)
        {
            var baseController = Load<RuntimeAnimatorController>(Art + "/Animations/Enemy/AcEnemy 0.controller");
            AnimationClip run = SaveSpriteClip($"{EnemyDataDir}/{key}_Run.anim", runFrames, runFps, true);
            AnimationClip hit = SaveSpriteClip($"{EnemyDataDir}/{key}_Hit.anim", hitFrames, 14f, false);
            AnimationClip dead = SaveSpriteClip($"{EnemyDataDir}/{key}_Dead.anim", deadFrames, 8f, false);

            string path = $"{EnemyDataDir}/{key}.overrideController";
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            bool isNew = aoc == null;
            if (isNew) aoc = new AnimatorOverrideController(baseController);
            else aoc.runtimeAnimatorController = baseController;

            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                string original = overrides[i].Key.name; // "RunEnemy 0", "HitEnemy 0", "DeadEnemy 0"
                AnimationClip replacement = original.StartsWith("Run") ? run
                    : original.StartsWith("Hit") ? hit
                    : original.StartsWith("Dead") ? dead
                    : null;
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, replacement);
            }
            aoc.ApplyOverrides(overrides);

            if (isNew) AssetDatabase.CreateAsset(aoc, path);
            else EditorUtility.SetDirty(aoc);
        }

        /// <summary>Sprite-swap clip on the SpriteRenderer of the Animator's own GameObject.</summary>
        private static AnimationClip SaveSpriteClip(string path, Sprite[] frames, float fps, bool loop)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool isNew = clip == null;
            if (isNew) clip = new AnimationClip();
            clip.frameRate = fps;

            // One key per frame plus a closing key, so the last frame is shown for a full frame too.
            var keys = new ObjectReferenceKeyframe[frames.Length + 1];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
            keys[^1] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[^1] };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            if (isNew) AssetDatabase.CreateAsset(clip, path);
            else EditorUtility.SetDirty(clip);
            return clip;
        }

        /// <summary>
        /// "Select 3" = slice of UI.png; "Props@31,22" = Props.png slice at that rect position;
        /// "B:Weapons &amp; Tools/Axe.png" = Brackeys single sprite; "B:Effects/Explosion.png#3" = 4th slice.
        /// </summary>
        private static Sprite ResolveIcon(string icon)
        {
            if (icon.StartsWith("B:"))
            {
                string rel = icon[2..];
                int hash = rel.IndexOf('#');
                if (hash < 0) return BrackeysSprite(rel);
                string file = rel[..hash];
                return BrackeysSlices(file)[int.Parse(rel[(hash + 1)..])];
            }
            if (!icon.StartsWith("Props@")) return SpriteNamed(UiSheet, icon);
            string[] xy = icon["Props@".Length..].Split(',');
            return SpriteAt(Props, int.Parse(xy[0]), int.Parse(xy[1]));
        }

        private static Sprite BrackeysSprite(string relativePath) => Load<Sprite>($"{Brackeys}/{relativePath}");

        /// <summary>Slices of a Brackeys sheet in frame order ("Explosion_0", "Explosion_1"...).</summary>
        private static Sprite[] BrackeysSlices(string relativePath) =>
            SpritesNamed($"{Brackeys}/{relativePath}", System.IO.Path.GetFileNameWithoutExtension(relativePath));

        private static AudioClip BrackeysSound(string file) => Load<AudioClip>($"{Brackeys}/Sounds/{file}");
        private static AudioClip HorrorSound(string relativePath) => Load<AudioClip>($"{Horror}/{relativePath}");

        private static Sprite[] DragonExplosionFrames() =>
            Enumerable.Range(1, 6).Select(i => Load<Sprite>($"{DragonFx}/explosion_0{i}.png")).ToArray();

        // ------------------------------------------------------------------ scene

        private static void BuildScene()
        {
            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) != null) AssetDatabase.DeleteAsset(ScenePath);
            if (!AssetDatabase.CopyAsset(SceneTemplate, ScenePath))
                throw new InvalidOperationException("Could not copy scene template " + SceneTemplate);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            // Opening a scene unloads unused assets, which invalidates references loaded before.
            // Everything below is loaded fresh from disk.
            GameObject playerPrefab = Load<GameObject>($"{PrefabDir}/Player.prefab");
            Enemy enemyPrefab = Load<GameObject>($"{PrefabDir}/Enemy.prefab").GetComponent<Enemy>();
            Pickup pickupPrefab = Load<GameObject>($"{PrefabDir}/Pickup.prefab").GetComponent<Pickup>();
            Font font = Load<Font>(Art + "/Fonts/neodgm.ttf");

            // Camera (comes from the URP 2D template, together with a Global Light 2D).
            Camera cam = Object.FindAnyObjectByType<Camera>(); // the template has exactly one
            cam.orthographicSize = 7.5f; // ~27 x 15 tiles visible at 16:9
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.12f);
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // Endless ground.
            var grid = new GameObject("Ground", typeof(Grid));
            var tilemapGo = CreateChild(grid.transform, "Tilemap", 0);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>().sortingOrder = -100;
            var ground = tilemapGo.AddComponent<InfiniteGround>();
            Set(ground, "tile", Load<TileBase>(Art + "/Tiles/RanTile.asset"));

            // Player + camera follow.
            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = Vector3.zero;
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            Set(follow, "target", player.transform);

            // Props scattered over the ground (Brackeys pack).
            var decor = new GameObject("Decor").AddComponent<DecorScatter>();
            var decorSo = new SerializedObject(decor);
            SerializedProperty decorItems = decorSo.FindProperty("items");
            var decorTable = new (string sprite, float weight, bool flat)[]
            {
                ("Environment/Gothic/Gravestone.png", 3f, false),
                ("Environment/Gothic/Stone.png", 3f, false),
                ("Environment/Farming/Barrel.png", 1.5f, false),
                ("Items & Icons/Cute/Crate.png", 1f, false),
                ("Items & Icons/Gothic/Skull.png", 1.5f, true),
                ("Environment/Gothic/Pentagram.png", 0.5f, true),
            };
            decorItems.arraySize = decorTable.Length;
            for (int i = 0; i < decorTable.Length; i++)
            {
                SerializedProperty item = decorItems.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("sprite").objectReferenceValue = BrackeysSprite(decorTable[i].sprite);
                item.FindPropertyRelative("weight").floatValue = decorTable[i].weight;
                item.FindPropertyRelative("flat").boolValue = decorTable[i].flat;
            }
            decorSo.ApplyModifiedPropertiesWithoutUndo();

            new GameObject("Effects").AddComponent<EffectPlayer>();

            var enemyShots = new GameObject("EnemyProjectiles").AddComponent<EnemyProjectiles>();
            Set(enemyShots, "prefab", Load<GameObject>($"{PrefabDir}/EnemyProjectile.prefab").GetComponent<EnemyProjectile>());
            Set(enemyShots, "fireSfx", BrackeysSound("Shot.wav"));

            // Audio.
            var music = CreateMusic(Load<AudioClip>(Art + "/Audio/BGM.wav"));
            var sfxGo = new GameObject("Sfx", typeof(AudioSource));
            sfxGo.AddComponent<SfxPlayer>();

            // UI.
            // The module assigns its default UI actions itself in OnEnable when none are set.
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            uiClickSfx = BrackeysSound("ButtonPress.wav");
            uiHoverSfx = BrackeysSound("ButtonHover.wav");
            var (_, levelUpView, gameOverView) = BuildUi(font);

            // Game state.
            var gm = new GameObject("GameManager").AddComponent<GameManager>();
            Set(gm, "gameOverView", gameOverView);
            Set(gm, "music", music.GetComponent<AudioSource>());
            Set(gm, "loseSfx", Load<AudioClip>(Art + "/Audio/Lose.wav"));
            Set(gm, "winSfx", Load<AudioClip>(Art + "/Audio/Win.wav"));

            // Upgrades.
            var upgrades = new GameObject("UpgradeManager").AddComponent<UpgradeManager>();
            SetArray(upgrades, "upgrades",
                UpgradeTable.Select(u => (Object)Load<UpgradeDefinition>($"{UpgradeDir}/{u.file}.asset")).ToArray());
            Set(upgrades, "fallback", Load<UpgradeDefinition>($"{UpgradeDir}/{FallbackUpgradeFile}.asset"));
            Set(upgrades, "view", levelUpView);
            Set(upgrades, "selectSfx", Load<AudioClip>(Art + "/Audio/Select.wav"));

            // Drops.
            var drops = new GameObject("DropManager").AddComponent<DropManager>();
            Set(drops, "pickupPrefab", pickupPrefab);
            SetArray(drops, "gemSprites", new Object[] { SpriteAt(Props, 0, 0), SpriteAt(Props, 11, 0), SpriteAt(Props, 22, 0) });
            Set(drops, "magnetSprite", SpriteAt(Props, 33, 0));
            Set(drops, "healthSprite", SpriteAt(Props, 46, 0));
            Set(drops, "chestSprite", SpriteNamed(UiSheet, "Box 0"));
            Set(drops, "goldSprite", BrackeysSprite("Items & Icons/Pixel Art/Coin.png"));

            // Enemies.
            var spawner = new GameObject("EnemySpawner").AddComponent<EnemySpawner>();
            Set(spawner, "enemyPrefab", enemyPrefab);
            SetEnemyTypes(spawner);

            // Bosses: giant versions that can't be pushed back and drop a chest. One per boss time.
            var bosses = spawner.gameObject.AddComponent<BossDirector>();
            Set(bosses, "spawner", spawner);
            WriteEnemyList(bosses, "bosses", BossTable);
            Set(bosses, "announceSfx", HorrorSound("Ambience/Jumpscare and Hits/Strike_002.wav"));
            Set(bosses, "finalAnnounceSfx", HorrorSound("Ambience/Ambience FX/Short_Rising-010.wav"));
            Set(bosses, "music", music);
            Set(bosses, "finalBossMusic", HorrorSound("Ambience/Atmosphere And Chaos/Chaos_001(Loop).wav"));
            SetArray(bosses, "deathExplosionFrames", DragonExplosionFrames());
            Set(bosses, "deathSfx", BrackeysSound("Explosion.wav"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static MusicPlayer CreateMusic(AudioClip clip)
        {
            var source = new GameObject("Music").AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = true;
            source.playOnAwake = true;
            return source.gameObject.AddComponent<MusicPlayer>(); // sets the volume from settings
        }

        // art: "U0".."U4" = Undead Survivor sheet Enemy N; otherwise a key from MonsterTable.
        // vScale/vOffset size the 150x150 monster frames to match the 18 px Undead sprites.
        private static readonly (string art, string name, float hp, float speed, float dps, int xp, float unlock, float retire,
            float weight, float scale, float vScale, float vOffset, float kbRes, Color tint, bool chest)[] EnemyTable =
        {
            ("U0",        "Zombie",         15f, 1.4f, 10f, 1,   0f, 150f, 3f,   1f,    1f,   0f,    0f,   Color.white, false),
            ("Goblin",    "Goblin",         12f, 2.3f, 10f, 1,  15f, 240f, 2.5f, 1f,    3.2f, 0.24f, 0f,   Color.white, false),
            ("Hooded",    "Hooded",         16f, 1.8f, 11f, 1,  30f, 270f, 2f,   1f,    0.7f, 0f,    0f,   Color.white, false),
            ("U1",        "Ghoul",          22f, 1.7f, 12f, 1,  40f, 330f, 2f,   1f,    1f,   0f,    0f,   Color.white, false),
            ("Firehead",  "Firehead",       12f, 2.2f,  6f, 2,  75f,   0f, 1.2f, 1f,    0.9f, 0f,    0f,   Color.white, false),
            ("Beetle",    "Beetle",         30f, 2.0f, 14f, 2, 100f,   0f, 1.5f, 1f,    0.4f, 0f,    0.2f, Color.white, false),
            ("Warlock",   "Warlock",        35f, 1.5f, 10f, 3, 150f,   0f, 1f,   1f,    0.75f, 0f,   0f,   Color.white, false),
            ("Cultist",   "Cultist",        45f, 1.7f, 16f, 3, 180f,   0f, 1.5f, 1f,    0.75f, 0f,   0.3f, Color.white, false),
            ("FlyingEye", "Flying Eye",     14f, 2.5f, 10f, 1,  60f,   0f, 1.5f, 1f,    3.2f, 0.3f,  0f,   Color.white, false),
            ("U2",        "Skeleton",       35f, 1.6f, 15f, 2,  90f,   0f, 2f,   1f,    1f,   0f,    0f,   Color.white, false),
            ("Mushroom",  "Mushroom",       70f, 1.1f, 17f, 3, 120f,   0f, 1f,   1.2f,  3.2f, 0.2f,  0.5f, Color.white, false),
            ("U4",        "Runner",         18f, 2.7f, 10f, 2, 150f,   0f, 1.2f, 0.85f, 1f,   0f,    0f,   Color.white, false),
            ("Skeleton",  "Dark Skeleton",  55f, 1.6f, 19f, 3, 210f,   0f, 1.5f, 1.1f,  3.2f, 0.3f,  0.6f, Color.white, false),
            ("U3",        "Brute",          90f, 1.3f, 22f, 5, 240f,   0f, 0.8f, 1.35f, 1f,   0f,    0.3f, Color.white, false),
        };

        private static readonly (string art, string name, float hp, float speed, float dps, int xp, float unlock, float retire,
            float weight, float scale, float vScale, float vOffset, float kbRes, Color tint, bool chest)[] BossTable =
        {
            // One per minute (BossDirector.bossTimes). Odd minutes: smaller, faster mini-bosses.
            // Base health; the spawner multiplies it by the run's time scaling (~1.45x at 1:00, ~10x at 8:00).
            // Tuned down after playtest ("a bit too hard"): ~25 % less health, ~20 % less contact damage.
            ("Goblin",    "Mini Boss: Goblin Chief",  230f, 2.6f, 24f, 15, 0f, 0f, 1f, 2.0f, 3.2f, 0.24f, 1f, new Color(1f, 0.85f, 0.5f),  true),
            ("U3",        "Boss: Red Brute",          380f, 2.0f, 32f, 25, 0f, 0f, 1f, 2.6f, 1f,   0f,    1f, new Color(1f, 0.55f, 0.55f), true),
            ("FlyingEye", "Mini Boss: Watcher",       300f, 2.8f, 24f, 15, 0f, 0f, 1f, 2.0f, 3.2f, 0.3f,  1f, new Color(0.7f, 1f, 0.7f),   true),
            ("Mushroom",  "Boss: Mushroom King",      520f, 1.7f, 36f, 30, 0f, 0f, 1f, 2.4f, 3.2f, 0.2f,  1f, new Color(1f, 0.8f, 0.6f),   true),
            ("Skeleton",  "Mini Boss: Bone Knight",   450f, 2.3f, 32f, 20, 0f, 0f, 1f, 2.0f, 3.2f, 0.3f,  1f, new Color(0.85f, 0.85f, 0.85f), true),
            ("U1",        "Boss: Ghoul Giant",        680f, 2.1f, 40f, 35, 0f, 0f, 1f, 2.6f, 1f,   0f,    1f, new Color(0.6f, 1f, 0.6f),   true),
            ("Skeleton",  "Boss: Bone Lord",          750f, 2.1f, 44f, 40, 0f, 0f, 1f, 2.6f, 3.2f, 0.3f,  1f, new Color(0.75f, 0.75f, 1f), true),
            // Final boss (last entry): defeating it wins the run.
            ("FlyingEye", "Final Boss: Great Eye",   1900f, 2.5f, 56f, 60, 0f, 0f, 1f, 3.4f, 3.2f, 0.3f,  1f, new Color(1f, 0.45f, 0.9f),  true),
        };

        private static void SetEnemyTypes(EnemySpawner spawner) => WriteEnemyList(spawner, "enemyTypes", EnemyTable);

        private static void WriteEnemyList(Object target, string property,
            (string art, string name, float hp, float speed, float dps, int xp, float unlock, float retire,
             float weight, float scale, float vScale, float vOffset, float kbRes, Color tint, bool chest)[] table)
        {
            var so = new SerializedObject(target);
            SerializedProperty arr = so.FindProperty(property);
            arr.arraySize = table.Length;
            for (int i = 0; i < table.Length; i++)
            {
                var t = table[i];
                WriteEnemyStats(arr.GetArrayElementAtIndex(i), t.name, EnemyController(t.art), t.hp, t.speed, t.dps, t.xp,
                    t.unlock, t.retire, t.weight, t.scale, t.tint, t.kbRes, t.chest);
                SerializedProperty e = arr.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("visualScale").floatValue = t.vScale;
                e.FindPropertyRelative("visualOffset").vector2Value = new Vector2(0f, t.vOffset);

                bool ranged = RangedStats.TryGetValue(t.name, out var r);
                e.FindPropertyRelative("attackRange").floatValue = ranged ? r.range : 0f;
                e.FindPropertyRelative("attackInterval").floatValue = ranged ? r.interval : 2.5f;
                e.FindPropertyRelative("projectileDamage").floatValue = ranged ? r.damage : 0f;
                e.FindPropertyRelative("projectileSpeed").floatValue = ranged ? r.speed : 6f;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static RuntimeAnimatorController EnemyController(string art)
        {
            if (art.Length == 2 && art[0] == 'U')
            {
                int sheet = art[1] - '0';
                return Load<RuntimeAnimatorController>(sheet == 0
                    ? Art + "/Animations/Enemy/AcEnemy 0.controller"
                    : $"{Art}/Animations/Enemy/AcEnemy {sheet}.overrideController");
            }
            return Load<RuntimeAnimatorController>($"{EnemyDataDir}/{art}.overrideController");
        }

        private static void WriteEnemyStats(SerializedProperty e, string name, RuntimeAnimatorController controller,
            float hp, float speed, float dps, int xp, float unlock, float retire, float weight, float scale, Color tint,
            float knockbackResistance, bool dropsChest)
        {
            e.FindPropertyRelative("name").stringValue = name;
            e.FindPropertyRelative("animator").objectReferenceValue = controller;
            e.FindPropertyRelative("maxHealth").floatValue = hp;
            e.FindPropertyRelative("moveSpeed").floatValue = speed;
            e.FindPropertyRelative("contactDps").floatValue = dps;
            e.FindPropertyRelative("xpValue").intValue = xp;
            e.FindPropertyRelative("unlockTime").floatValue = unlock;
            e.FindPropertyRelative("retireTime").floatValue = retire;
            e.FindPropertyRelative("spawnWeight").floatValue = weight;
            e.FindPropertyRelative("knockbackResistance").floatValue = knockbackResistance;
            e.FindPropertyRelative("dropsChest").boolValue = dropsChest;
            e.FindPropertyRelative("scale").floatValue = scale;
            e.FindPropertyRelative("tint").colorValue = tint;
        }

        /// <summary>Exactly the game's scenes, menu first (it is what the build starts with).</summary>
        private static void SetBuildScenes(params string[] ordered)
        {
            EditorBuildSettings.scenes = ordered.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }

        private static void BuildMenuScene()
        {
            if (AssetDatabase.LoadMainAssetAtPath(MenuScenePath) != null) AssetDatabase.DeleteAsset(MenuScenePath);
            if (!AssetDatabase.CopyAsset(SceneTemplate, MenuScenePath))
                throw new InvalidOperationException("Could not copy scene template " + SceneTemplate);

            var scene = EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Font font = Load<Font>(Art + "/Fonts/neodgm.ttf");

            Camera cam = Object.FindAnyObjectByType<Camera>();
            cam.orthographicSize = 7.5f;
            cam.backgroundColor = new Color(0.12f, 0.14f, 0.12f);
            cam.transform.position = new Vector3(0f, 0f, -10f);

            // Same grass as the game, behind the menu.
            var grid = new GameObject("Ground", typeof(Grid));
            var tilemapGo = CreateChild(grid.transform, "Tilemap", 0);
            tilemapGo.AddComponent<Tilemap>();
            tilemapGo.AddComponent<TilemapRenderer>().sortingOrder = -100;
            Set(tilemapGo.AddComponent<InfiniteGround>(), "tile", Load<TileBase>(Art + "/Tiles/RanTile.asset"));

            CreateMusic(Load<AudioClip>(Art + "/Audio/BGM.wav"));
            new GameObject("Sfx", typeof(AudioSource)).AddComponent<SfxPlayer>();
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            uiClickSfx = BrackeysSound("ButtonPress.wav");
            uiHoverSfx = BrackeysSound("ButtonHover.wav");
            BuildMenuUi(font, LoadCharacters(), BrackeysSound("Bonus.wav"));

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        // ------------------------------------------------------------------ helpers

        private static GameObject SavePrefab(GameObject go, string name)
        {
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabDir}/{name}.prefab");
            Object.DestroyImmediate(go);
            if (asset == null) throw new InvalidOperationException("Failed to save prefab " + name);
            return asset;
        }

        private static GameObject CreateChild(Transform parent, string name, int layer)
        {
            var child = new GameObject(name) { layer = layer };
            child.transform.SetParent(parent, false);
            return child;
        }

        private static int Layer(string name)
        {
            int layer = LayerMask.NameToLayer(name);
            if (layer < 0) throw new InvalidOperationException($"Layer '{name}' missing.");
            return layer;
        }

        private static T Load<T>(string path) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException($"{typeof(T).Name} not found at {path}");
            return asset;
        }

        /// <summary>All sprites in a sheet, in reading order (top-left to bottom-right).</summary>
        private static Sprite[] SpritesIn(string path)
        {
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                .OrderByDescending(s => s.rect.y).ThenBy(s => s.rect.x).ToArray();
            if (sprites.Length == 0) throw new InvalidOperationException("No sprites in " + path);
            return sprites;
        }

        /// <summary>
        /// Frames of one animation, e.g. "Run" returns "Run 0", "Run 1", ... ordered by frame number.
        /// A slice named exactly "Hit" (no number) also matches "Hit".
        /// </summary>
        private static Sprite[] SpritesNamed(string path, string spriteName)
        {
            var pattern = new Regex("^" + Regex.Escape(spriteName) + @"(?:[ _](?<n>\d+))?$");
            Sprite[] all = SpritesIn(path);

            Sprite[] result = all
                .Select(s => (sprite: s, match: pattern.Match(s.name)))
                .Where(t => t.match.Success)
                .OrderBy(t => t.match.Groups["n"].Success ? int.Parse(t.match.Groups["n"].Value) : -1)
                .Select(t => t.sprite)
                .ToArray();

            if (result.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No '{spriteName}' sprites in {path}. Found: {string.Join(", ", all.Select(s => s.name))}");
            }
            return result;
        }

        /// <summary>The single sprite with exactly this name, e.g. "Select 3".</summary>
        private static Sprite SpriteNamed(string path, string exactName)
        {
            Sprite[] all = SpritesIn(path);
            return all.FirstOrDefault(s => s.name == exactName)
                ?? throw new InvalidOperationException(
                    $"No sprite '{exactName}' in {path}. Found: {string.Join(", ", all.Select(s => s.name))}");
        }

        private static Sprite SpriteAt(string path, int x, int y)
        {
            Sprite s = SpritesIn(path).FirstOrDefault(sp => (int)sp.rect.x == x && (int)sp.rect.y == y);
            if (s == null) throw new InvalidOperationException($"No sprite at ({x},{y}) in {path}");
            return s;
        }

        /// <summary>Single-file sprite (e.g. a GUI_Parts texture).</summary>
        private static Sprite GuiSprite(string file) => Load<Sprite>($"{Gui}/{file}.png");

        private static void Set(Object target, string property, Object value)
        {
            if (value == null) // also catches destroyed/unloaded objects
                throw new InvalidOperationException($"{target.GetType().Name}.{property}: value is null");

            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property)
                ?? throw new InvalidOperationException($"{target.GetType().Name}.{property} not found");
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string property, Object[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty p = so.FindProperty(property)
                ?? throw new InvalidOperationException($"{target.GetType().Name}.{property} not found");
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == null)
                    throw new InvalidOperationException($"{target.GetType().Name}.{property}[{i}]: value is null");
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
