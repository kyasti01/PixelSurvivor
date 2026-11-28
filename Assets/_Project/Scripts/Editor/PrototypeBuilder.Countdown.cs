using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor.EditorTools
{
    /// <summary>
    /// Oyun başındaki "3, 2, 1, HAYATTA KAL!" geri sayımı: UI nesneleri + Animation Clip + Animator Controller.
    ///
    /// Klip SADECE YOKSA oluşturulur. Yani Animation penceresinde keyframe'leri elle değiştirirsen
    /// "Build Prototype" tekrar çalışsa bile değişikliklerin korunur. Klibi sıfırlamak istersen
    /// Data/Countdown klasöründeki iki dosyayı silip Build Prototype'ı yeniden çalıştır.
    /// </summary>
    public static partial class PrototypeBuilder
    {
        private const string CountdownDir = DataDir + "/Countdown";
        private const string CountdownClipPath = CountdownDir + "/StartCountdown.anim";
        private const string CountdownControllerPath = CountdownDir + "/StartCountdown.controller";

        // Klipteki yollar bu adlara bağlıdır: nesneleri yeniden adlandırırsan animasyon onları bulamaz.
        private static readonly string[] CountdownNumbers = { "Three", "Two", "One" };
        private const string CountdownGo = "Go";
        private const string CountdownDim = "Dim";

        private static void BuildCountdown(Transform canvas)
        {
            RectTransform holder = UiRect("Countdown", canvas);
            Stretch(holder);

            // Arkadaki hafif karartma; animasyon başta açar, "HAYATTA KAL!" ile birlikte kaldırır.
            Image dim = UiImage(CountdownDim, holder, null, new Color(0f, 0f, 0f, 0f));
            Stretch(dim.rectTransform);

            // Hepsi görünmez başlar (alfa 0); görünürlüğü animasyon verir.
            for (int i = 0; i < CountdownNumbers.Length; i++)
            {
                Text number = UiText(CountdownNumbers[i], holder, (CountdownNumbers.Length - i).ToString(), 260,
                    TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0f));
                number.GetComponent<Outline>().effectDistance = new Vector2(6f, -6f);
                Place(number.rectTransform, Center, Center, Center, new Vector2(0, 80), new Vector2(600, 320));
            }

            Text go = UiText(CountdownGo, holder, "HAYATTA KAL!", 150, TextAnchor.MiddleCenter,
                new Color(Gold.r, Gold.g, Gold.b, 0f));
            go.GetComponent<Outline>().effectDistance = new Vector2(5f, -5f);
            Place(go.rectTransform, Center, Center, Center, new Vector2(0, 80), new Vector2(1600, 220));

            var animator = holder.gameObject.AddComponent<Animator>();
            animator.runtimeAnimatorController = GetOrCreateCountdownController();
            var countdown = holder.gameObject.AddComponent<StartCountdown>();
            Set(countdown, "tickSfx", BrackeysSound("ButtonPress.wav"));
            Set(countdown, "goSfx", BrackeysSound("Spawn.wav"));
        }

        private static RuntimeAnimatorController GetOrCreateCountdownController()
        {
            if (!AssetDatabase.IsValidFolder(CountdownDir)) AssetDatabase.CreateFolder(DataDir, "Countdown");

            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CountdownClipPath);
            if (clip == null)
            {
                clip = CreateCountdownClip();
                AssetDatabase.CreateAsset(clip, CountdownClipPath);
            }

            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CountdownControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(CountdownControllerPath, clip);
            return controller;
        }

        /// <summary>
        /// Zaman çizelgesi (saniye):
        ///   0-1 "3", 1-2 "2", 2-3 "1": her sayı büyükten küçülerek "pat" diye gelir, sonra solar.
        ///   3.0  "HAYATTA KAL!" sallanarak belirir, oyun başlar, karartma kalkar.
        ///   4.4  yazı büyüyerek kaybolur; 4.45'te geri sayım ekranı kapanır.
        /// </summary>
        private static AnimationClip CreateCountdownClip()
        {
            var clip = new AnimationClip { name = "StartCountdown", frameRate = 60f };

            Curve(clip, CountdownDim, typeof(Image), "m_Color.a", (0f, 0.45f), (3f, 0.45f), (3.4f, 0f));

            for (int i = 0; i < CountdownNumbers.Length; i++)
            {
                float t = i;
                string path = CountdownNumbers[i];
                Curve(clip, path, typeof(Text), "m_Color.a", (t, 0f), (t + 0.12f, 1f), (t + 0.8f, 1f), (t + 1f, 0f));
                Scale(clip, path, (t, 2.5f), (t + 0.25f, 1f), (t + 0.8f, 1f), (t + 1f, 0.6f));
            }

            Curve(clip, CountdownGo, typeof(Text), "m_Color.a", (3f, 0f), (3.1f, 1f), (3.9f, 1f), (4.4f, 0f), (4.5f, 0f));
            Scale(clip, CountdownGo, (3f, 0.3f), (3.2f, 1.25f), (3.35f, 1f), (3.9f, 1f), (4.4f, 1.5f));
            Curve(clip, CountdownGo, typeof(RectTransform), "localEulerAnglesRaw.x", (3f, 0f));
            Curve(clip, CountdownGo, typeof(RectTransform), "localEulerAnglesRaw.y", (3f, 0f));
            Curve(clip, CountdownGo, typeof(RectTransform), "localEulerAnglesRaw.z", (3f, -10f), (3.2f, 6f), (3.35f, 0f));

            AnimationUtility.SetAnimationEvents(clip, new[]
            {
                new AnimationEvent { time = 0f, functionName = nameof(StartCountdown.Tick) },
                new AnimationEvent { time = 1f, functionName = nameof(StartCountdown.Tick) },
                new AnimationEvent { time = 2f, functionName = nameof(StartCountdown.Tick) },
                new AnimationEvent { time = 3f, functionName = nameof(StartCountdown.Go) },
                new AnimationEvent { time = 4.45f, functionName = nameof(StartCountdown.Finished) },
            });

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = false; // bir kez oynar
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            return clip;
        }

        private static void Curve(AnimationClip clip, string path, Type type, string property,
                                  params (float time, float value)[] keys)
        {
            var curve = new AnimationCurve();
            foreach ((float time, float value) in keys) curve.AddKey(time, value);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, type, property), curve);
        }

        private static void Scale(AnimationClip clip, string path, params (float time, float value)[] keys)
        {
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.x", keys);
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.y", keys);
            Curve(clip, path, typeof(RectTransform), "m_LocalScale.z", keys);
        }
    }
}
