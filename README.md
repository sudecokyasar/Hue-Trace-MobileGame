<div align="center"><img width="732" height="155" alt="image" src="https://github.com/user-attachments/assets/d49822d5-1773-44e9-b409-583aea8b8f59" /></div>

# Hue Trace - Color Connect Puzzle Game



![Unity](https://img.shields.io/badge/Unity-2022.3%20LTS-black?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=csharp&logoColor=white)
![Platform](https://img.shields.io/badge/Platform-Mobile-blue?style=for-the-badge)

**Renkleri birleştir, bulmacayı çöz, yıldızları topla.**

Flow-Free tarzı, grid tabanlı bir bağlantı bulmacası oyunu. Klasik mekaniğin üzerine kilitli hücreler, buz blokları, köprüler ve renk karıştırma sistemleriyle katmanlı bir zorluk eğrisi eklendi.

[Özellikler](#-özellikler) •
[Ekran Görüntüleri](#-ekran-görüntüleri) •
[Oynanış](#-oynanış-mekanikleri) •
[Kurulum](#-kurulum) •
[Mimari](#-proje-mimarisi)



---

##  Hakkında

**Hue Trace**, aynı renkteki iki taşı grid üzerinde çizgi çekerek birbirine bağlamanı isteyen bir bulmaca oyunudur. Basit görünen kural setinin üzerine eklenen özel hücre tipleri (kilit, buz, köprü, renk karışımı) her zorluk modunda oyuncuyu farklı şekillerde düşünmeye zorlar.

Proje; **Easy / Normal / Hard** olmak üzere üç zorluk modu, mod başına 100 level, hamle bazlı yıldız derecelendirme sistemi ve kalıcı ilerleme kaydı (`PlayerPrefs`) ile tam bir mobil bulmaca oyunu döngüsü sunar.

---

##  Ekran Görüntüleri



|  Ana Menü | Zorluk Seçim Ekranı |
|:---:|:---:|
|<img width="389" height="690" alt="image" src="https://github.com/user-attachments/assets/97cb1e56-5225-4edc-8213-a84877ac0c14" />| <img width="391" height="688" alt="image" src="https://github.com/user-attachments/assets/364f645b-405d-441d-894e-c9f0e9510fb5" />|

| Ayarlar Ekranı | Level Seçim Ekranı |
|:---:|:---:|
|<img width="384" height="690" alt="image" src="https://github.com/user-attachments/assets/19fa92b8-8bd1-4f68-a57e-39900760720f" />| <img width="384" height="689" alt="image" src="https://github.com/user-attachments/assets/f4bbdbab-8bc4-481d-9f14-f683701a8e18" />| 

| Oyun Ekranı | Level Tamamlandı Ekranı |
|:---:|:---:|
| <img width="382" height="686" alt="image" src="https://github.com/user-attachments/assets/07e741b8-f346-4bba-8ca6-8d3396ad9b31" />| <img width="389" height="683" alt="image" src="https://github.com/user-attachments/assets/2d17f114-75ba-4990-8f6e-aeeec9dbce7f" />|


---

##  Özellikler

###  Temel Oynanış
- **Path-drawing (çizgi çekme) mekaniği** - aynı renkteki taşları grid üzerinde kesintisiz bir hat ile birleştir
- Anlık dokunmatik girişiyle çalışan responsive path line çizim sistemi (Unity Input System tabanlı)
- **Undo** ve **Retry** butonlarıyla anlık hamle düzeltme
- Hamle limiti ve optimal hamle sayısına göre performans takibi

###  Özel Hücre Tipleri
| Hücre Tipi | Davranış |
|---|---|
|  **Kilitli Hücre** | Sadece belirlenen renk bu hücreden geçebilir |
|  **Buz Hücresi** | Üzerinden bir kez geçildiğinde kırılır, tekrar kullanılamaz |
|  **Köprü Hücresi** | İki farklı rengin aynı hücrede kesişmeden geçmesine izin verir |
|  **Renk Karıştırma Hücresi** | İki farklı rengi birleştirip üçüncü bir renk üretir (yalnızca Hard mod) |

###  İlerleme ve Ödül Sistemi
- **3 Zorluk Modu:** Easy, Normal, Hard — her biri kendi 20 levellik seti ve grid boyutlarıyla
- **Yıldız derecelendirme sistemi:** Hamle sayısına göre level başına 1-3 yıldız
  - Optimal hamlede tamamlama → ⭐⭐⭐
  - Optimal ile limit arası → ⭐⭐
  - Limit sınırında tamamlama → ⭐

---

##  Teknoloji Yığını

| Katman | Teknoloji |
|---|---|
| Motor | Unity |
| Dil | C# |
| Veri Modeli | ScriptableObject tabanlı `LevelData` |
| Kalıcı Depolama | `PlayerPrefs` |
| Mimari Desen | Event-driven (static C# event'ler ile gevşek bağlı bileşenler) |

---
##  Geliştirici Ekip

Bu proje iki kişilik bağımsız bir ekip tarafından ortak emekle geliştirilmiştir:

* **Sude Çokyaşar** - Görsel Tasarım, 2D Çizimler & Oyun Geliştirme
* **Toprak Kaya** —-Oyun Mekaniği, Sistem Tasarımı & Oyun Geliştirme

---
