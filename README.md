# Pixel Survivor

Unity 6 ile geliştirilmiş, tepeden bakışlı 2D pixel-art **hayatta kalma (horde survival / roguelite)** oyunu.
Oyuncu sürekli artan düşman dalgalarına karşı hayatta kalmaya çalışır; silahlar otomatik ateş eder,
oyuncu hareket eder, XP toplar, seviye atladıkça yükseltme seçer ve 8. dakikada gelen **son boss'u**
yenerek oyunu kazanır.

---

## Nasıl Oynanır

| Tuş | İşlev |
|---|---|
| **WASD / Ok tuşları** | Hareket |
| **Shift / Space** | Atılma (dash): kısa süre hasar almaz, düşmanların içinden geçersin |
| **1 / 2 / 3** veya fare | Seviye atlayınca yükseltme seç (oyun durmaz) |
| **Esc** | Duraklat (ses ayarları, ana menü) |
| **R** | Oyun bitince yeniden başla |

**Amaç:** Düşmanları öldür, düşen XP taşlarını topla, seviye atla ve yükseltme seç. Her dakika bir
boss gelir; yenince hazine sandığı düşer. **8:00'de gelen SON BOSS'u yenersen kazanırsın.**

Oyun içinde ana menüdeki **NASIL OYNANIR** ekranı da aynı bilgileri gösterir.

---

## Özellikler

- **4 oynanabilir karakter**: farklı can, hız ve başlangıç silahları. Üçü altınla ya da oyunu kazanarak açılır.
- **5 silah**: otomatik tüfek, her yöne ateş topu (çember), dönen kürekler, fırlatma baltası, alev bombası.
- **20 yükseltme**: silah güçlendirmeleri, pasif bonuslar ve **3 silah evrimi** (iki yükseltme son seviyeye gelince açılır).
- **14 düşman türü**: zamanla sırayla gelir. Yakın dövüşçüler, hızlı koşucular, tanklar ve **menzilli düşmanlar** var.
- **Elit düşmanlar**: altın renkli, güçlü, daha çok ödül verir.
- **8 boss**: her dakika bir tane. Son boss yenilince oyun kazanılır.
- **Kalıcı gelişim**: düşmanlardan altın düşer, ana menüdeki **Dükkan**'da kalıcı güçlendirme alınır (PlayerPrefs ile kaydedilir).
- **3 zorluk seviyesi**: Kolay / Normal / Zor (can, hasar, düşman yoğunluğu, altın çarpanı).
- **Sonsuz harita**: kamera ilerledikçe zemin ve engeller (mezar taşı, kaya, fıçı...) üretilir.
- **Oyun hissi**: hasar sayıları, vuruş parlaması, ekran sarsıntısı, patlama efektleri, ses efektleri ve müzik.
- **Menüler**: ana menü, karakter + zorluk seçimi, dükkan, nasıl oynanır, duraklatma (ses ayarları), oyun sonu ekranı.

---

## Gereksinimler ve Çalıştırma

- **Unity 6000.6.10f1** (Unity 6), **Universal Render Pipeline (2D)**, **Input System**
- Platform: Windows (PC)

**Editörde oynamak:**
1. Projeyi Unity Hub ile aç.
2. `Assets/_Project/Scenes/MainMenu.unity` sahnesini aç.
3. **Play**'e bas.

**Sahneleri yeniden üretmek:** Sahneler ve prefab'lar bir editör aracıyla koddan üretilir.
Menüden **PixelSurvivor → Build Prototype** çalıştırılınca hepsi baştan kurulur.

**Windows .exe almak:** **PixelSurvivor → Build Windows (.exe)**. Çıktı `Builds/Windows/` klasörüne yazılır.
Başka bir bilgisayarda çalıştırmak için klasörün **tamamı** kopyalanmalıdır.

---

## Proje Yapısı

```
Assets/
├── _Project/                 ← oyunun kendi dosyaları
│   ├── Scenes/               MainMenu.unity, Game.unity
│   ├── Scripts/
│   │   ├── Core/             oyun durumu, ayarlar, kayıt, kamera, harita, efekt, ses
│   │   ├── Player/           hareket, atılma, XP, karakter yükleme, animasyon
│   │   ├── Enemies/          düşman, spawner, boss yöneticisi, düşman mermileri
│   │   ├── Weapons/          5 silah ve mermi havuzu
│   │   ├── Pickups/          XP taşı, altın, iksir, mıknatıs, sandık
│   │   ├── Upgrades/         yükseltme tanımı ve seçim sistemi
│   │   ├── Characters/       karakter tanımı, seçilen karakter
│   │   ├── UI/               HUD, menüler, seviye atlama, dükkan, hasar sayıları
│   │   └── Editor/           sahne/prefab üretici, Windows build aracı
│   ├── Prefabs/              Player, Enemy, mermiler, pickup
│   └── Data/                 yükseltme ve karakter ScriptableObject'leri, düşman animasyonları
├── Settings/                 URP ve Input System ayarları
└── (asset paketleri)         sadece oyunda kullanılan dosyalar bırakıldı
```

---

## Teknik Notlar

- **Mimari:** Her sistem tek bir işten sorumlu küçük bileşenlerden oluşur (ör. `Health`, `EnemySpawner`,
  `UpgradeManager`). Sistemler birbirine olaylarla (`Enemy.Killed`, `Health.Died`, `LeveledUp`) bağlanır.
- **Veri odaklı tasarım:** Yükseltmeler ve karakterler `ScriptableObject`'tir; Inspector'dan kod değiştirmeden ayarlanabilir.
- **Performans:** Düşmanlar, mermiler, pickup'lar ve efektler **nesne havuzu** (`ObjectPool`) ile yeniden kullanılır.
  Yüzlerce düşmanda bile çöp üretimi düşük tutulur.
- **Fizik:** Düşmanlar dinamik `Rigidbody2D` ile kalabalık oluşturur. Katman matrisi sayesinde mermiler yalnızca
  hedeflerine çarpar.
- **Kayıt:** Altın, kalıcı yükseltmeler, açılan karakterler, zorluk ve ses ayarları `PlayerPrefs` ile saklanır.
- **Otomatik kurulum:** `PrototypeBuilder` editör aracı katmanları, prefab'ları, sahneleri ve UI'ı koddan üretir.
  Böylece proje her zaman tutarlı biçimde yeniden kurulabilir.

---

## Kullanılan Hazır Asset'ler

Tüm görsel ve ses dosyaları ücretsiz paketlerden alınmıştır. Kod tamamen bu proje için yazılmıştır.

| Paket | Kaynak | Kullanım |
|---|---|---|
| Undead Survivor | Goldmetal, Unity Asset Store | Karakterler, ilk düşmanlar, zemin, UI, müzik, sesler |
| Monsters Creatures Fantasy | Luiz Melo, Unity Asset Store | Goblin, Mantar, Uçan Göz, İskelet |
| 2D Mega Pack | Brackeys | Ek düşmanlar, balta, altın, dekor, efektler, sesler |
| Dragon Warrior | Graphictoon | Patlama ve atılma efektleri |
| Horror Atmosphere LITE | Unity Asset Store | Boss uyarısı ve son boss müziği |
| GUI Parts | Unity Asset Store | Panel, çerçeve ve buton görselleri |
| Neo둥근모 (neodgm) font | Undead Survivor paketiyle gelir (SIL Open Font License) | Oyundaki tüm yazılar |

---

## Geliştirme

Proje Unity 6 ve C# ile geliştirilmiştir. Geliştirme sürecinde kodlama asistanı olarak
Claude Code'dan  destek alınmıştır.
