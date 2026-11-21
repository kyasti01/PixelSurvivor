using UnityEngine;

namespace PixelSurvivor
{
    public enum UpgradeKind
    {
        GunDamage,        // value = oran, örn. 0.25 = +%25
        GunFireRate,      // value = aralığın kısalma oranı
        GunProjectiles,   // value = ek mermi sayısı
        GunPierce,        // value = ek delinen düşman sayısı
        OrbitCount,       // value = ek kürek sayısı
        OrbitPower,       // value = hız ve hasardaki artış oranı
        MoveSpeed,        // value = oran
        MaxHealth,        // value = sabit can (o kadar da iyileştirir)
        PickupRadius,     // value = oran
        Heal,             // value = sabit can
        // Değerler yükseltme asset'lerine sayı olarak kaydedilir: yeni türleri her zaman sadece sona ekle.
        RingCount,        // value = çember başına ek mermi
        RingFireRate,     // value = aralığın kısalma oranı
        RingDamage,       // value = oran
        AxeCount,         // value = atış başına ek balta (ilki silahı açar)
        AxePower,         // value = hasar artış oranı
        BombCount,        // value = atış başına ek bomba (ilki silahı açar)
        BombRadius,       // value = patlama alanı artış oranı
        EvolveGun,        // evrimler: tek seferlik büyük güçlendirmeler, bkz. UpgradeManager.Apply
        EvolveRing,
        EvolveOrbit,
    }

    /// <summary>Seviye atlama ekranında çıkabilecek bir seçenek.</summary>
    [CreateAssetMenu(menuName = "PixelSurvivor/Upgrade", fileName = "Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string displayName = "Upgrade";
        [TextArea(2, 4)] public string description;
        public Sprite icon;
        public UpgradeKind kind;
        public float value = 0.1f;
        [Tooltip("0 = sınırsız alınabilir.")]
        [Min(0)] public int maxLevel = 5;
        [Tooltip("Sadece bu yükseltme en az bir kez alındıktan sonra sunulur.")]
        public UpgradeDefinition requires;
        [Tooltip("Sadece bunların HEPSİ maks seviyedeyken sunulur (silah evrimleri).")]
        public UpgradeDefinition[] requiresMaxed = System.Array.Empty<UpgradeDefinition>();
        [Tooltip("Seviye atlama ekranında EVRİM rozetiyle gösterilir.")]
        public bool isEvolution;
        [Tooltip("Sunulma ihtimalinin göreli ağırlığı.")]
        [Min(0f)] public float weight = 1f;
    }
}
