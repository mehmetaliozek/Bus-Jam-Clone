# Projeyi Genişletme Rehberi

Bu rehber, Bus Jam Clone'a yeni seviye, yolcu türü, renk veya oyun kuralı eklerken mevcut kodun sunduğu uzatma noktalarını anlatır. Oyun akışı için [mimari belgesine](ARCHITECTURE.md), seviye editörünün kullanımı için [editör rehberine](LEVEL_EDITOR.md) bakın.

## Önce değişikliğin türünü seçin

| İhtiyaç | Başlangıç noktası |
| --- | --- |
| Yeni düzen, süre, yolcu veya otobüs sırası | `LevelDataSO` asseti ve seviye editörü. |
| Farklı koltuk ihtiyacı, ölçek, prefab veya aksesuar | Yeni bir temel `PassengerTypeSO` asseti; C# değişikliği gerekmez. |
| Yolcunun ne zaman hareket edebileceğini veya nasıl göründüğünü değiştirme | `PassengerTypeSO` alt sınıfı ve `PassengerMechanic`. |
| Yolcu ve otobüsler için yeni eşleşme rengi | `GameColor` enum'u ve renk eşlemesi. |
| Yeni bir genel oyun kuralı | Kuralın sahibi olan bileşen, gerekirse `GameEvents` ve UI; durum/yeniden başlatma/kayıt etkisini ayrıca değerlendirin. |

## Yeni seviyeyi veriyle ekleme

Tercih edilen yol, **Tools > Bus Jam > Level Editor** içindeki **New Level** düğmesidir. Editör `Assets/Resources/Levels/LevelN.asset` oluşturur. Bu konum önemlidir: hem editör seviye listesini bu klasörden toplar hem de `LevelManager` çalışma sırasında `Resources.LoadAll<LevelDataSO>("Levels")` kullanır. Yeni seviye için `Game.unity` sahnesini Build Settings'e yeniden eklemek gerekmez.

Seviye eklerken `LevelNumber` değerini benzersiz tutun. Oyun seviyeleri bu sayıya göre sıralar, kayıt dosyası da açılacak seviyeyi bu sayıyla bulur; editör çakışmayı engellemez. Hücre koordinatları tekrarlanmamalı ve her yolcu etkin bir hücreye yerleştirilmelidir. `y = 0` satırında çıkış hücreleri bırakın. Renk başına toplam koltuk talebi, o renkteki otobüslerin **üçer koltukluk toplamına tam eşit** olmalıdır: otobüsler yalnız dolunca kalkar, seviye ise kuyruk boşalmadan tamamlanmaz. Otobüs sırasını, erişilebilir yolları ve süreyi oynayarak ayrıca kontrol edin. Editörün **Balance & Validation** bölümü yalnız koltuk hesabı yapar. Ayrıntılı adımlar [seviye editörü rehberindedir](LEVEL_EDITOR.md).

## Kod yazmadan yeni yolcu türü ekleme

Bu yol, yeni bir davranış kuralı gerekmiyorsa uygundur. Unity'de **Assets > Create > Bus Jam > Passenger Types > Basic Type** ile `PassengerTypeSO` asseti oluşturun. Editör türleri yalnızca `Assets/Resources/PassengerTypes` klasöründe aradığı için bu asseti bu klasöre koymanız **zorunludur**.

Inspector'daki alanları amacına göre ayarlayın:

| Alan grubu | Etkisi |
| --- | --- |
| `Display Name`, `Editor Short Tag`, `Editor Tag Color` | Editör paletindeki ad, hücre etiketi ve etiket rengi. |
| `Seat Requirement` | Yolcunun istediği koltuk sayısı; çalışma sırasında **1–3** aralığına sınırlandırılır. |
| `Prefab Override` | Boş bırakılırsa sahnedeki varsayılan yolcu prefabı kullanılır. Verilirse `PassengerSpawner` o prefab için ayrı havuz oluşturur. |
| `Accessory Prefab`, `Accessory Local Position`, `Scale Multiplier` | Yolcunun görünümü; ölçek **0,5–2** aralığına sınırlandırılır. Yerel aksesuar konumu, prefabda `Accessory Anchor` atanmadığında kullanılır. |
| `Has Custom Parameter` ve ilgili alanlar | Editörde yolcuya bir sayı girme ve etiket gösterme imkânı sağlar. Parametreyi yorumlayan bir mekanik yazılmadıkça yeni oyun davranışı oluşturmaz. |

Asset oluşturulduğunda editör `PassengerTypeSO` assetlerini yeniden tarar. Palet güncellenmezse Level Editor penceresini yeniden açın. Önce **Color** paletiyle bir yolcu oluşturun, ardından **Type** paletinden yeni türü seçip aynı yolcuya uygulayın; tür boyama boş hücrede yolcu oluşturmaz. Değişikliğin kaydedilmesi için seviyeyi de **Save Level** ile kaydedin.

Örneğin iki koltuk isteyen bir yolcu için temel tür assetinde `Seat Requirement = 2` seçilebilir. Bu, özel bir `PassengerMechanic` gerektirmez; fakat otobüs kapasitesi üç olduğu için aynı renkte bir koltukluk başka yolcu da gerekebilir. Tek başına iki koltuk isteyen yolcu, üç koltuklu otobüsü doldurup kaldıramaz. Yeni prefab kullanıyorsanız `Passenger`, `PassengerMovement` ve `PassengerAnimatorController` bileşenleri ile gerekli görsel/animasyon referanslarını kontrol edin. Yolcunun tıklanabilmesi için `OnMouseDown()` olayını alacak bir Collider da gerekir.

## Yeni davranışlı yolcu türü ekleme

Yeni bir davranış için `Assets/Code/Gameplay/Passenger/Types` altında `PassengerTypeSO`'dan türeyen bir sınıf oluşturun. Unity Create menüsünden asset açılabilmesi için sınıfa `CreateAssetMenu` niteliği ekleyin. Sınıf `public override PassengerMechanic CreateMechanic()` içinde o yolcuya özgü yeni bir mekanik nesnesi döndürmelidir. Temel sınıfın varsayılan uygulaması `null` döner; yalnız alt sınıf oluşturmak davranışı etkinleştirmez.

`PassengerMechanic` içindeki uygun kancaları kullanın:

| Kanca | Kullanım zamanı |
| --- | --- |
| `OnInitialize()` / `OnReset()` | Yolcu oluşturulurken ve havuza dönerken mekanik durumunu kurma/sıfırlama. |
| `CanMoveFromGrid(bool canReachExit)` | Izgaradaki yolcunun seçilebilir olup olmadığını belirleme. Çıkışa erişim koşulunu koruyun. |
| `GetVisualColor(GameColor baseColor)` | Yolcunun gösterilen rengini hesaplama. |
| `OnReachabilityChanged(bool canReachExit)` | Yol bulunabilirliği yenilendiğinde tepki verme. |
| `OnAnyPassengerMovedFromGrid(Passenger movedPassenger)` | Başka bir yolcu ızgaradan bir yolculuğa başladığında tepki verme. |
| `OnTick(float deltaTime)` | Oyun `Playing` durumundayken, yolcu ızgarada veya bekleme yerindeyse zamanla değişen davranış. |
| `OnMoveStarted()`, `OnArrivedAtWaitingSlot()`, `OnBoarded(Transform seat)` | Yolcunun hareket ve biniş aşamalarına tepki verme. |

`FrozenPassengerTypeSO`, `Data.Parameter` değerini kullanma, hareketi geçici olarak engelleme, görsel durumu yenileme ve mekanik durumunu sıfırlama örneğidir. `MysteryPassengerTypeSO`, erişim açıldığında görünümü değiştirme örneğidir. Mekanik içinde yolcuya ait değişebilir durumu assetin kendisinde tutmayın: aynı tür asseti birçok yolcu kullanabilir, `CreateMechanic()` ise her yolcu için ayrı nesne sağlar.

Kod derlendikten sonra bu alt sınıftan `Assets/Resources/PassengerTypes` klasöründe bir asset oluşturun, Inspector alanlarını ayarlayın ve bir seviyedeki mevcut yolcuya editör paletinden uygulayın. Özel parametre gerekiyorsa assette `Has Custom Parameter` alanını etkinleştirin; mekanik tarafında `Data.Parameter` değerini okuyup tür assetinin `ClampParameter(Data.Parameter)` metoduyla sınırlandırın. Görsel veya seçilebilirlik durumu değiştiğinde `Passenger.RefreshVisuals()` kullanımını mevcut Frozen/Mystery örnekleriyle karşılaştırın.

## Yeni eşleşme rengi ekleme

Renk eklerken [`GameColor.cs`](../Assets/Code/Core/GameColor.cs) içindeki enum'a **sona yeni, ardışık bir değer** ekleyin ve `GameColorExtensions.CachedColors` dizisine aynı indekste Unity rengini ekleyin. Mevcut enum sayılarını değiştirmeyin: seviye assetlerinde renkler sayısal olarak serileştirilir. Renk dizisi eksik kalırsa `ToUnityColor` bilinmeyen değer için beyaz döner.

Seviye editörü paleti `GameColor` enum'undan üretir ve renk başına sayaçlarını enum indeksleriyle tutar. Bu nedenle değerlerin `0`'dan başlayan bitişik indeksler olması gerekir. Yeni renk eklendikten sonra bu rengi kullanacak yolcuları ve otobüsleri seviye verisinde açıkça seçin; mevcut seviyeler kendiliğinden değişmez. Editör paletini, yolcu ve otobüs görsellerini, koltuk dengesi satırlarını ve kaydedilip yeniden açılmış seviyeyi kontrol edin.

## Genel oyun kuralı veya kapasiteyi değiştirme

Yeni kuralın hangi bileşene ait olduğunu önce belirleyin: seviye yaşam döngüsü `LevelManager`'da, seçme ve sonuç kontrolü `PassengerController`'da, biniş yolculukları `BoardingCoordinator`'da, otobüs sırası `BusQueue`'dadır. Sistemler arası bildirim gerekiyorsa `GameEvents`'e olay ekleyin ve dinleyiciyi `OnEnable`/`OnDisable` çiftinde yönetin. Kuralın `Ready`, `Playing`, `Paused`, `Completed` ve `Failed` durumlarında ne yapacağını tanımlayın; yeniden başlatma ve havuza dönüşte geçici durumu temizleyin.

Otobüs kapasitesini değiştirmek yalnız [`GameConstants.cs`](../Assets/Code/Core/GameConstants.cs) içindeki sayıyı değiştirmek değildir. `Bus` prefabındaki koltuk referansları, `Bus.IsConfigured` denetimi, koltuk ayırma, yolcu türü `Seat Requirement` sınırı, editörün denge hesabı ve mevcut seviyelerin otobüs/yolcu sayıları birlikte gözden geçirilmelidir. Kayıt kapsamı değişecekse `SaveData` ve `SaveManager` da ayrıca güncellenmelidir; mevcut kayıt yalnız seviye numarası ve ses/titreşim tercihlerini tutar.

## Değişiklik sonrası kontrol listesi

1. Unity'de derleme hatası olmadığını ve gerekli assetin editör paletinde göründüğünü doğrulayın.
2. Yeni türü veya rengi en az bir seviye assetine uygulayıp asseti kaydedin.
3. Yolcu erişimini, renk eşleşmesini, koltuk sayısını ve bekleme alanına geçişi oynayın.
4. Duraklatma/devam, yeniden başlatma ve sonraki seviye akışlarını kontrol edin.
5. Prefab veya mekanik değiştirdiyseniz aynı seviyeyi yeniden yükleyerek havuzdan gelen nesnenin eski durum taşımadığını doğrulayın.
6. Kayıt davranışını etkilediyseniz uygulamayı kapatıp açma akışını deneyin.