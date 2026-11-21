using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Physics layer names used by the game and the collision rules between them.
    /// Layers are created by the PixelSurvivor/Build Prototype editor menu.
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
            // Player shots only touch enemies, enemy shots only touch the player.
            // (Pickups use distance checks, not physics.)
            CollideOnlyWith(LayerMask.NameToLayer(PlayerProjectile), LayerMask.NameToLayer(Enemy));
            CollideOnlyWith(LayerMask.NameToLayer(EnemyProjectile), LayerMask.NameToLayer(Player));

            // The dash turns this off temporarily; make sure every run starts with it on
            // (the setting would otherwise survive leaving Play mode mid-dash).
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
