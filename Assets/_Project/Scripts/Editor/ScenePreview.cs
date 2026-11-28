using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace PixelSurvivor.EditorTools
{
    /// <summary>
    /// Zemin (<see cref="InfiniteGround"/>) ve dekorlar (<see cref="DecorScatter"/>) oyun sırasında koddan
    /// üretildiği için editörde Scene ekranı boş görünür. Bu araç, sahne açıkken (Play'e basılmadan) kameranın
    /// etrafına oyunun başlangıçta üreteceği zemin ve dekorların AYNISINI önizleme olarak çizer.
    ///
    /// Önizleme güvenlidir:
    ///  - Nesneler HideFlags.DontSave ile oluşturulur; sahne dosyasına kaydedilmez, build'e girmez.
    ///  - Play'e basılırken silinir, Play'den çıkınca yeniden çizilir; oyun bugünkü gibi çalışır.
    ///  - Oyunun kendi script'lerine dokunulmaz.
    /// Inspector'da ayar değiştirirsen menüden: PixelSurvivor → Sahne Önizlemesini Yenile.
    /// </summary>
    [InitializeOnLoad]
    internal static class ScenePreview
    {
        private const string RootName = "[ÖNİZLEME - sadece editörde, oyunda yok]";
        private const HideFlags Flags = HideFlags.DontSave | HideFlags.NotEditable;

        // Kameranın etrafında önizlenecek zemin genişliği (karo). Oyun ekranından biraz büyük.
        private const int GroundHalfWidth = 24;
        private const int GroundHalfHeight = 14;

        static ScenePreview()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            EditorSceneManager.sceneOpened += (_, _) => Schedule();
            EditorSceneManager.sceneClosing += (_, _) => Clear();
            Schedule();
        }

        [MenuItem("PixelSurvivor/Sahne Önizlemesini Yenile")]
        private static void RebuildMenu() => Rebuild();

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode) Clear();      // oyun önizlemesiz başlar
            else if (state == PlayModeStateChange.EnteredEditMode) Schedule();
        }

        private static void Schedule()
        {
            EditorApplication.delayCall -= Rebuild;
            EditorApplication.delayCall += Rebuild;
        }

        private static void Clear()
        {
            // Script derlemesinden sonra statik referanslar kaybolur; önizlemeleri adından buluruz.
            foreach (GameObject go in Resources.FindObjectsOfTypeAll<GameObject>())
                if (go != null && go.name == RootName && (go.hideFlags & HideFlags.DontSave) != 0)
                    Object.DestroyImmediate(go);
        }

        private static void Rebuild()
        {
            Clear();
            if (EditorApplication.isPlayingOrWillChangePlaymode || BuildPipeline.isBuildingPlayer) return;

            Camera cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
            Vector3 center = cam != null ? cam.transform.position : Vector3.zero;

            foreach (InfiniteGround ground in Object.FindObjectsByType<InfiniteGround>())
                PreviewGround(ground, center);
            foreach (DecorScatter decor in Object.FindObjectsByType<DecorScatter>())
                PreviewDecor(decor, center);
        }

        private static GameObject CreateRoot(Component owner)
        {
            var root = new GameObject(RootName) { hideFlags = Flags };
            SceneManager.MoveGameObjectToScene(root, owner.gameObject.scene);
            return root;
        }

        private static GameObject CreateChild(Transform parent, string name)
        {
            var go = new GameObject(name) { hideFlags = Flags };
            go.transform.SetParent(parent, false);
            return go;
        }

        // ---------- Zemin ----------

        private static void PreviewGround(InfiniteGround ground, Vector3 center)
        {
            var tile = new SerializedObject(ground).FindProperty("tile").objectReferenceValue as TileBase;
            if (tile == null) return;

            Tilemap source = ground.GetComponent<Tilemap>();
            var sourceRenderer = ground.GetComponent<TilemapRenderer>();
            Grid sourceGrid = ground.GetComponentInParent<Grid>();

            GameObject root = CreateRoot(ground);
            root.transform.SetPositionAndRotation(
                sourceGrid != null ? sourceGrid.transform.position : Vector3.zero,
                sourceGrid != null ? sourceGrid.transform.rotation : Quaternion.identity);
            var grid = root.AddComponent<Grid>();
            if (sourceGrid != null)
            {
                grid.cellSize = sourceGrid.cellSize;
                grid.cellGap = sourceGrid.cellGap;
                grid.cellLayout = sourceGrid.cellLayout;
            }

            GameObject mapGo = CreateChild(root.transform, "Zemin (önizleme)");
            mapGo.transform.localPosition = source.transform.localPosition;
            var map = mapGo.AddComponent<Tilemap>();
            map.tileAnchor = source.tileAnchor;
            var renderer = mapGo.AddComponent<TilemapRenderer>();
            if (sourceRenderer != null)
            {
                renderer.sortingLayerID = sourceRenderer.sortingLayerID;
                renderer.sortingOrder = sourceRenderer.sortingOrder;
                renderer.sharedMaterial = sourceRenderer.sharedMaterial;
            }

            Vector3Int c = map.WorldToCell(center);
            var size = new Vector3Int(GroundHalfWidth * 2 + 1, GroundHalfHeight * 2 + 1, 1);
            var tiles = new TileBase[size.x * size.y];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = tile;
            map.SetTilesBlock(new BoundsInt(c.x - GroundHalfWidth, c.y - GroundHalfHeight, 0, size.x, size.y, 1), tiles);
        }

        // ---------- Dekor (DecorScatter.Populate ile aynı yerleşim) ----------

        private struct DecorItem
        {
            public Sprite Sprite;
            public float Weight;
            public bool Flat;
        }

        private static void PreviewDecor(DecorScatter decor, Vector3 center)
        {
            var so = new SerializedObject(decor);
            SerializedProperty itemsProp = so.FindProperty("items");
            float chunkSize = so.FindProperty("chunkSize").floatValue;
            int minPerChunk = so.FindProperty("minPerChunk").intValue;
            int maxPerChunk = so.FindProperty("maxPerChunk").intValue;
            int viewChunks = so.FindProperty("viewChunks").intValue;
            float referencePpu = so.FindProperty("referencePpu").floatValue;
            float clearRadius = so.FindProperty("clearRadiusAtStart").floatValue;
            int seed = so.FindProperty("seed").intValue;

            var items = new List<DecorItem>();
            float totalWeight = 0f;
            for (int i = 0; i < itemsProp.arraySize; i++)
            {
                SerializedProperty p = itemsProp.GetArrayElementAtIndex(i);
                var item = new DecorItem
                {
                    Sprite = p.FindPropertyRelative("sprite").objectReferenceValue as Sprite,
                    Weight = p.FindPropertyRelative("weight").floatValue,
                    Flat = p.FindPropertyRelative("flat").boolValue,
                };
                items.Add(item);
                totalWeight += item.Weight;
            }
            if (items.Count == 0 || totalWeight <= 0f || chunkSize <= 0f) return;

            GameObject root = CreateRoot(decor);
            var centerChunk = new Vector2Int(Mathf.FloorToInt(center.x / chunkSize), Mathf.FloorToInt(center.y / chunkSize));

            for (int x = -viewChunks; x <= viewChunks; x++)
            for (int y = -viewChunks; y <= viewChunks; y++)
            {
                var chunk = new Vector2Int(centerChunk.x + x, centerChunk.y + y);
                var rng = new Random(Hash(seed, chunk));
                int count = rng.Next(minPerChunk, maxPerChunk + 1);
                for (int i = 0; i < count; i++)
                {
                    DecorItem item = Pick(items, rng.NextDouble() * totalWeight);
                    var pos = new Vector2(
                        (chunk.x + (float)rng.NextDouble()) * chunkSize,
                        (chunk.y + (float)rng.NextDouble()) * chunkSize);
                    if (!item.Flat && pos.sqrMagnitude < clearRadius * clearRadius) continue;
                    if (item.Sprite == null) continue;

                    GameObject go = CreateChild(root.transform, item.Sprite.name);
                    go.transform.position = pos;
                    go.transform.localScale = Vector3.one * (item.Sprite.pixelsPerUnit / referencePpu);
                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.hideFlags = Flags;
                    sr.sprite = item.Sprite;
                    sr.sortingOrder = item.Flat ? -50 : 0;
                }
            }
        }

        private static DecorItem Pick(List<DecorItem> items, double roll)
        {
            foreach (DecorItem item in items)
            {
                roll -= item.Weight;
                if (roll <= 0) return item;
            }
            return items[^1];
        }

        // DecorScatter.Hash ile birebir aynı olmalı; yoksa önizleme oyundaki yerleşimden farklı olur.
        private static int Hash(int seed, Vector2Int c) => unchecked(seed * 73856093 ^ c.x * 19349663 ^ c.y * 83492791);
    }
}
