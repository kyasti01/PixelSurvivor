using UnityEngine;
using UnityEngine.UI;

namespace PixelSurvivor
{
    /// <summary>Health, experience, level, kills and survival time.</summary>
    public class HudView : MonoBehaviour
    {
        [SerializeField] private Image healthFill;
        [SerializeField] private Text healthText;
        [SerializeField] private Image xpFill;
        [SerializeField] private Text levelText;
        [SerializeField] private Text killText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text goldText;
        [SerializeField] private Image dashFill;
        [SerializeField] private Color dashReadyColor = new(0.45f, 0.85f, 1f);
        [SerializeField] private Color dashChargingColor = new(0.35f, 0.4f, 0.5f);

        [Header("Boss")]
        [SerializeField] private GameObject bossBar;
        [SerializeField] private Image bossFill;
        [SerializeField] private Text announcementText;

        // Last shown values, so strings are only rebuilt when something changes.
        private int shownHp = -1, shownMaxHp = -1, shownLevel = -1, shownKills = -1, shownSeconds = -1, shownGold = -1;

        private void LateUpdate()
        {
            PlayerController player = PlayerController.Instance;
            GameManager gm = GameManager.Instance;

            if (player != null)
            {
                healthFill.fillAmount = player.Health.Normalized;
                int hp = Mathf.CeilToInt(player.Health.Current);
                int maxHp = Mathf.CeilToInt(player.Health.Max);
                if (hp != shownHp || maxHp != shownMaxHp)
                {
                    shownHp = hp;
                    shownMaxHp = maxHp;
                    healthText.text = $"{hp} / {maxHp}";
                }

                if (dashFill != null)
                {
                    float ready = player.DashReadiness;
                    dashFill.fillAmount = ready;
                    dashFill.color = ready >= 1f ? dashReadyColor : dashChargingColor;
                }

                xpFill.fillAmount = player.Experience.Progress;
                if (player.Experience.Level != shownLevel)
                {
                    shownLevel = player.Experience.Level;
                    levelText.text = $"Lv.{shownLevel}";
                }
            }

            if (gm != null)
            {
                if (gm.Kills != shownKills)
                {
                    shownKills = gm.Kills;
                    killText.text = shownKills.ToString();
                }

                if (goldText != null && gm.RunGold != shownGold)
                {
                    shownGold = gm.RunGold;
                    goldText.text = shownGold.ToString();
                }

                // Count down to the final boss; once it's out, tell the player what to do.
                float final = BossDirector.FinalBossTime;
                int seconds = gm.Elapsed < final ? Mathf.CeilToInt(final - gm.Elapsed) : -1;
                if (float.IsInfinity(final)) seconds = (int)gm.Elapsed;
                if (seconds != shownSeconds)
                {
                    shownSeconds = seconds;
                    timerText.text = float.IsInfinity(final) ? $"{seconds / 60:00}:{seconds % 60:00}"
                        : seconds >= 0 ? $"SON BOSS  {seconds / 60:00}:{seconds % 60:00}"
                        : "SON BOSS'U YEN!";
                }
            }

            UpdateBoss();
        }

        private void UpdateBoss()
        {
            // Scenes built before the boss feature have no boss UI; rebuild to get it.
            if (bossBar == null || bossFill == null || announcementText == null) return;

            Enemy boss = BossDirector.CurrentBoss;
            bool showBar = boss != null && boss.IsAlive;
            if (bossBar.activeSelf != showBar) bossBar.SetActive(showBar);
            if (showBar) bossFill.fillAmount = boss.Health.Normalized;

            bool announce = Time.time < BossDirector.AnnouncementUntil;
            announcementText.enabled = announce;
            if (announce)
            {
                announcementText.text = BossDirector.Announcement;
                Color c = announcementText.color;
                c.a = 0.4f + 0.6f * Mathf.PingPong(Time.time * 3f, 1f); // blink
                announcementText.color = c;
            }
        }
    }
}
