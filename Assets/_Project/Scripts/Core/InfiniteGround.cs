using UnityEngine;
using UnityEngine.Tilemaps;

namespace PixelSurvivor
{
    /// <summary>
    /// Kamera ilerledikçe etrafına zemin karoları döşer; böylece harita hiç bitmez.
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    public class InfiniteGround : MonoBehaviour
    {
        [SerializeField] private TileBase tile;
        [Tooltip("Ekran kenarının ötesine fazladan döşenen karo sayısı.")]
        [SerializeField, Min(0)] private int margin = 4;

        private Tilemap map;
        private Camera cam;
        private Vector3Int lastCenter = new(int.MinValue, int.MinValue, 0);

        private void Awake()
        {
            map = GetComponent<Tilemap>();
            cam = Camera.main;
        }

        private void LateUpdate()
        {
            if (tile == null || cam == null) return;

            Vector3Int center = map.WorldToCell(cam.transform.position);
            if (center == lastCenter) return;
            lastCenter = center;

            int halfH = Mathf.CeilToInt(cam.orthographicSize) + margin;
            int halfW = Mathf.CeilToInt(cam.orthographicSize * cam.aspect) + margin;

            for (int x = center.x - halfW; x <= center.x + halfW; x++)
            for (int y = center.y - halfH; y <= center.y + halfH; y++)
            {
                var cell = new Vector3Int(x, y, 0);
                if (!map.HasTile(cell)) map.SetTile(cell, tile);
            }
        }
    }
}
