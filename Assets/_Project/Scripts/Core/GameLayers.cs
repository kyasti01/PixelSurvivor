using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Oyunun kullandığı fizik katmanlarının adları ve aralarındaki çarpışma kuralları.
    /// Katmanlar PixelSurvivor/Build Prototype editör menüsüyle oluşturulur.
    /// </summary>
    public static class GameLayers
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string PlayerProjectile = "PlayerProjectile";
        public const string EnemyProjectile = "EnemyProjectile";

        public static readonly string[] All = { Player, Enemy, PlayerProjectile, EnemyProjectile };

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ConfigureCollisionMatrix()
        {
            // Oyuncu mermileri sadece düşmanlara, düşman mermileri sadece oyuncuya çarpar.
            // (Pickup'lar fizik değil mesafe kontrolü kullanır.)
            CollideOnlyWith(LayerMask.NameToLayer(PlayerProjectile), LayerMask.NameToLayer(Enemy));
            CollideOnlyWith(LayerMask.NameToLayer(EnemyProjectile), LayerMask.NameToLayer(Player));

            // Atılma bunu geçici olarak kapatır; her oyunun açık başladığından emin ol
            // (yoksa atılmanın ortasında Play modundan çıkılırsa ayar kapalı kalabilir).
            SetPlayerEnemyCollision(true);
        }

        public static void SetPlayerEnemyCollision(bool collide)
        {
            int player = LayerMask.NameToLayer(Player);
            int enemy = LayerMask.NameToLayer(Enemy);
            if (player >= 0 && enemy >= 0) Physics2D.IgnoreLayerCollision(player, enemy, !collide);
        }

        private static void CollideOnlyWith(int layer, int allowed)
        {
            if (layer < 0 || allowed < 0)
            {
                Debug.LogWarning("[GameLayers] Layers missing. Run 'PixelSurvivor > Build Prototype' once.");
                return;
            }

            for (int other = 0; other < 32; other++)
                Physics2D.IgnoreLayerCollision(layer, other, other != allowed);
        }
    }
}
