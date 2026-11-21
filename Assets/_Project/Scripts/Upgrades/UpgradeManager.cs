using System.Collections.Generic;
using UnityEngine;

namespace PixelSurvivor
{
    /// <summary>
    /// Her seviye atlamada <see cref="LevelUpView"/> üzerinden rastgele yükseltmeler sunar ve oyuncunun
    /// seçtiğini uygular. Varsayılan olarak oyunu durdurmaz (bkz. pauseWhileChoosing).
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        [SerializeField] private UpgradeDefinition[] upgrades;
        [Tooltip("Her şey maks seviyeye gelince boş kalan yerleri doldurmak için sunulur.")]
        [SerializeField] private UpgradeDefinition fallback;
        [SerializeField] private LevelUpView view;
        [SerializeField, Min(1)] private int choicesPerLevel = 3;
        [SerializeField] private AudioClip selectSfx;
        [Tooltip("Bir önceki seviyede sunulan yükseltmelerin ağırlık çarpanı; seçenekler çeşitlensin diye.")]
        [SerializeField, Range(0f, 1f)] private float repeatPenalty = 0.3f;
        [SerializeField] private string levelUpTitle = "SEVİYE ATLADIN!";
        [SerializeField] private string chestTitle = "HAZİNE SANDIĞI!";
        [Tooltip("Kapalı: seçenekler oyun devam ederken ekranın altında çıkar.")]
        [SerializeField] private bool pauseWhileChoosing;

        private string currentTitle;

        public static UpgradeManager Instance { get; private set; }

        private readonly Dictionary<UpgradeDefinition, int> levels = new();
        private readonly List<UpgradeDefinition> candidates = new();
        private readonly List<float> weights = new();
        private readonly List<UpgradeDefinition> choices = new();
        private readonly HashSet<UpgradeDefinition> lastOffered = new();

        private PlayerController player;
        private AutoGun gun;
        private RingShooter ring;
        private OrbitWeapon orbit;
        private AxeThrower axe;
        private BombThrower bomb;
        private bool choosing;
        private int pendingChestPicks;

        public int LevelOf(UpgradeDefinition def) => def != null && levels.TryGetValue(def, out int l) ? l : 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        private void Awake() => Instance = this;

        private void Start()
        {
            player = PlayerController.Instance;
            if (player == null) return;
            // Kapalı/devre dışı silahları da dahil et: bir karakter bazılarına sahip olmadan başlayabilir.
            gun = player.GetComponentInChildren<AutoGun>(true);
            ring = player.GetComponentInChildren<RingShooter>(true);
            orbit = player.GetComponentInChildren<OrbitWeapon>(true);
            axe = player.GetComponentInChildren<AxeThrower>(true);
            bomb = player.GetComponentInChildren<BombThrower>(true);
            player.Experience.LeveledUp += OnLeveledUp;
        }

        private void OnDestroy()
        {
            if (player != null) player.Experience.LeveledUp -= OnLeveledUp;
            if (Instance == this) Instance = null;
        }

        /// <summary>Yükseltme ekranını <paramref name="picks"/> kez daha açar (hazine sandığı).</summary>
        public void GrantChest(int picks)
        {
            pendingChestPicks += Mathf.Max(1, picks);
            if (!choosing) ShowNext();
            else RefreshTitle();
        }

        private void OnLeveledUp()
        {
            if (!choosing) ShowNext();
            else RefreshTitle();
        }

        /// <summary>Başlık artı sırada kaç seçim beklediği, örn. "SEVİYE ATLADIN!  (+2)".</summary>
        private void RefreshTitle()
        {
            int waiting = player.Experience.PendingLevelUps + pendingChestPicks;
            view.SetTitle(waiting > 0 ? $"{currentTitle}  (+{waiting})" : currentTitle);
        }

        private void SetPaused(bool paused)
        {
            if (pauseWhileChoosing && GameManager.Instance != null) GameManager.Instance.SetPaused(paused);
        }

        private void ShowNext()
        {
            bool gameOver = GameManager.Instance != null && GameManager.Instance.IsGameOver;
            string title = null;
            if (!gameOver)
            {
                if (pendingChestPicks > 0)
                {
                    pendingChestPicks--;
                    title = chestTitle;
                }
                else if (player.Experience.TryConsumeLevelUp())
                {
                    title = levelUpTitle;
                }
            }

            if (title == null)
            {
                choosing = false;
                SetPaused(false);
                return;
            }

            choosing = true;
            SetPaused(true);
            RollChoices();
            currentTitle = title;
            view.Show(title, choices, LevelOf, OnPicked);
            RefreshTitle();
        }

        private void OnPicked(UpgradeDefinition def)
        {
            Apply(def);
            levels[def] = LevelOf(def) + 1;
            SfxPlayer.Play(selectSfx);
            ShowNext(); // aynı anda birden fazla seviye atlanmış olabilir
        }

        private void RollChoices()
        {
            candidates.Clear();
            weights.Clear();
            foreach (UpgradeDefinition u in upgrades)
            {
                if (u == null) continue;
                bool maxed = u.maxLevel > 0 && LevelOf(u) >= u.maxLevel;
                bool locked = (u.requires != null && !IsOwned(u.requires)) || !RequirementsMaxed(u);
                if (maxed || locked || u.weight <= 0f) continue;

                candidates.Add(u);
                weights.Add(lastOffered.Contains(u) ? u.weight * repeatPenalty : u.weight);
            }

            choices.Clear();
            while (choices.Count < choicesPerLevel && candidates.Count > 0)
            {
                float total = 0f;
                foreach (float w in weights) total += w;

                float roll = Random.value * total;
                int picked = candidates.Count - 1;
                for (int i = 0; i < candidates.Count; i++)
                {
                    roll -= weights[i];
                    if (roll <= 0f) { picked = i; break; }
                }

                choices.Add(candidates[picked]);
                candidates.RemoveAt(picked);
                weights.RemoveAt(picked);
            }

            if (choices.Count < choicesPerLevel && fallback != null) choices.Add(fallback);

            lastOffered.Clear();
            lastOffered.UnionWith(choices);
        }

        /// <summary>
        /// En az bir kez alınmışsa ya da verdiği silah zaten varsa true
        /// (örn. küreklerle başlayan bir karakter kürek yükseltmesine "sahip" sayılır).
        /// </summary>
        private bool IsOwned(UpgradeDefinition def)
        {
            if (LevelOf(def) > 0) return true;
            return def.kind switch
            {
                UpgradeKind.OrbitCount => orbit != null && orbit.Count > 0,
                UpgradeKind.AxeCount => axe != null && axe.Count > 0,
                UpgradeKind.BombCount => bomb != null && bomb.Count > 0,
                UpgradeKind.RingCount or UpgradeKind.RingFireRate => ring != null && ring.enabled,
                UpgradeKind.GunDamage or UpgradeKind.GunFireRate or UpgradeKind.GunProjectiles or UpgradeKind.GunPierce
                    => gun != null && gun.enabled,
                _ => false,
            };
        }

        /// <summary>requiresMaxed'teki her yükseltme maks seviyedeyse (ya da liste boşsa) true.</summary>
        private bool RequirementsMaxed(UpgradeDefinition def)
        {
            if (def.requiresMaxed == null) return true;
            foreach (UpgradeDefinition r in def.requiresMaxed)
                if (r != null && (r.maxLevel == 0 || LevelOf(r) < r.maxLevel)) return false;
            return true;
        }

        private void Apply(UpgradeDefinition def)
        {
            float v = def.value;

            // Bu silah olmadan başlayan bir karakter, silahı ilk yükseltmesiyle kazanır.
            switch (def.kind)
            {
                case UpgradeKind.GunDamage or UpgradeKind.GunFireRate or UpgradeKind.GunProjectiles or UpgradeKind.GunPierce:
                    gun.enabled = true;
                    break;
                case UpgradeKind.RingCount or UpgradeKind.RingFireRate or UpgradeKind.RingDamage:
                    ring.enabled = true;
                    break;
            }

            switch (def.kind)
            {
                case UpgradeKind.GunDamage: gun.Damage *= 1f + v; break;
                case UpgradeKind.GunFireRate: gun.FireInterval *= 1f - v; break;
                case UpgradeKind.GunProjectiles: gun.ProjectileCount += Mathf.RoundToInt(v); break;
                case UpgradeKind.GunPierce: gun.Pierce += Mathf.RoundToInt(v); break;
                case UpgradeKind.RingCount: ring.Count += Mathf.RoundToInt(v); break;
                case UpgradeKind.RingFireRate: ring.FireInterval *= 1f - v; break;
                case UpgradeKind.RingDamage: ring.Damage *= 1f + v; break;
                case UpgradeKind.AxeCount: axe.Count += Mathf.RoundToInt(v); break;
                case UpgradeKind.AxePower: axe.Damage *= 1f + v; break;
                case UpgradeKind.BombCount: bomb.Count += Mathf.RoundToInt(v); break;
                case UpgradeKind.BombRadius: bomb.Radius *= 1f + v; break;

                // Evrimler: bir silahın yükseltmelerinin hepsi maks olunca güçte büyük bir sıçrama.
                case UpgradeKind.EvolveGun:
                    gun.Damage *= 2f;
                    gun.Pierce += 5;
                    gun.FireInterval *= 0.7f;
                    break;
                case UpgradeKind.EvolveRing:
                    ring.Count += 8;
                    ring.Damage *= 1.5f;
                    ring.FireInterval *= 0.6f;
                    break;
                case UpgradeKind.EvolveOrbit:
                    orbit.Count += 2;
                    orbit.Radius *= 1.3f;
                    orbit.Damage *= 2f;
                    orbit.DegreesPerSecond *= 1.5f;
                    break;
                case UpgradeKind.OrbitCount: orbit.Count += Mathf.RoundToInt(v); break;
                case UpgradeKind.OrbitPower:
                    orbit.DegreesPerSecond *= 1f + v;
                    orbit.Damage *= 1f + v;
                    break;
                case UpgradeKind.MoveSpeed: player.MoveSpeed *= 1f + v; break;
                case UpgradeKind.MaxHealth:
                    player.Health.SetMax(player.Health.Max + v, refill: false);
                    player.Health.Heal(v);
                    break;
                case UpgradeKind.PickupRadius: player.Experience.PickupRadius *= 1f + v; break;
                case UpgradeKind.Heal: player.Health.Heal(v); break;
            }
        }
    }
}
