# Protocol Independent Notification Connector

Bu proje, farklı protokollerden gelen bildirim mesajlarını ortak bir formata dönüştürerek backend üzerinden işleyen ve frontend arayüzünde canlı olarak listeleyen uçtan uca bir sistemdir.

## Amaç

Projenin temel amacı; RabbitMQ, WebSocket, Redis pub/sub ve Webhook gibi farklı kaynaklardan gelen mesajları protokol bağımsız bir connector yapısı üzerinden ortak bir bildirim formatına dönüştürmektir.

Normalize edilen mesajlar backend API tarafından alınır, doğrulanır, tekrar eden kayıtlar elenir ve frontend arayüzünde listelenir.

## Bileşenler

| Bileşen | Açıklama | Teknoloji |
|---|---|---|
| Simulator | Test bildirimleri ve senaryo mesajları üreten uygulama | .NET 10 Console |
| Connector | Protokol bağımsız çekirdek ve kaynak adapter yapısı | .NET 10 Worker |
| Backend | Bildirimleri alan, doğrulayan, tekilleştiren ve listeleyen API | ASP.NET Core Minimal API |
| Frontend | Bildirimleri listeleyen tek sayfalık web arayüzü | React + Vite |

## Mevcut Durum

İlk hafta kapsamında simulator, backend ve frontend arasında temel uçtan uca veri akışı kurulmuştur.

Mevcut çalışan akış:

```txt
Simulator -> Backend API -> Frontend Liste
```

Bu aşamada simulator, backend API’ye HTTP üzerinden senaryo bildirimleri göndermektedir.

İlerleyen aşamalarda simulator aynı olayları RabbitMQ, WebSocket, Redis pub/sub ve Webhook üzerinden üretecek; connector ise bu kaynaklardan gelen mesajları adapter yapısı ile okuyup ortak formata dönüştürecektir.

## Planlanan Final Mimari

```txt
Simulator
 ├── RabbitMQ
 ├── WebSocket
 ├── Redis pub/sub
 └── Webhook
      │
      ▼
Connector
 ├── Protocol-independent core
 ├── RabbitMQ adapter
 ├── WebSocket adapter
 ├── Redis adapter
 └── Webhook adapter
      │
      ▼
Backend API
      │
      ▼
Frontend
```

## Proje Yapısı

```txt
protocol-independent-notification-connector/
├── simulator/
│   └── NotificationSimulator/
├── connector/
├── backend/
│   └── NotificationBackend/
├── frontend/
│   └── notification-frontend/
├── docs/
└── docker-compose.yml
```

> Not: Connector adapterları, RabbitMQ, Redis, WebSocket, Webhook entegrasyonları ve Docker Compose yapısı ilerleyen aşamalarda tamamlanacaktır.

## Gereksinimler

Local geliştirme için:

- .NET 10 SDK
- Node.js
- npm
- Git

Final teslim aşamasında sistem Docker Compose ile tek komutla ayağa kaldırılacak şekilde düzenlenecektir.

## Kullanılan Local Portlar

| Servis | Local Adres | Açıklama |
|---|---|---|
| Backend | `http://localhost:5199` | ASP.NET Core Minimal API |
| Frontend | `http://localhost:5173` | React + Vite geliştirme sunucusu |

Port değerleri local geliştirme ortamına aittir. Final Docker Compose aşamasında kullanılacak portlar README içerisinde ayrıca güncellenecektir.

## Environment Değerleri

Projede servis adreslerinin doğrudan koda bağımlı kalmaması için environment variable desteği eklenmiştir.

| Bileşen | Değişken | Varsayılan Değer | Açıklama |
|---|---|---|---|
| Frontend | `VITE_API_BASE_URL` | `http://localhost:5199` | Frontend’in istek atacağı backend API adresi |
| Simulator | `BACKEND_URL` | `http://localhost:5199` | Simulator’ın bildirim göndereceği backend adresi |
| Backend | `FRONTEND_ORIGINS` | `http://localhost:3000,http://localhost:5173` | CORS için izin verilen frontend adresleri |


## Local Çalıştırma Adımları

Projeyi local ortamda çalıştırmak için backend, frontend ve simulator ayrı terminallerde başlatılır.

### 1. Backend’i Çalıştırma

```bash
cd backend/NotificationBackend
dotnet run
```

Backend çalıştıktan sonra aşağıdaki adresten kontrol edilebilir:

```txt
http://localhost:5199/api/health
```

### 2. Frontend’i Çalıştırma

Yeni bir terminal açarak:

```bash
cd frontend/notification-frontend
npm install
npm run dev
```

Frontend aşağıdaki adreste çalışır:

```txt
http://localhost:5173
```

### 3. Simulator’ı Çalıştırma

Backend ve frontend çalışırken yeni bir terminal açarak:

```bash
cd simulator/NotificationSimulator
dotnet run
```

Simulator çalıştırıldığında backend API’ye senaryo bildirimleri gönderilir. Geçerli bildirimler frontend ekranında listelenir.

Simulator’ı farklı bir backend adresiyle çalıştırmak için:

```bash
BACKEND_URL=http://localhost:5199 dotnet run
```

Frontend’i farklı bir backend API adresiyle çalıştırmak için:

```bash
VITE_API_BASE_URL=http://localhost:5199 npm run dev
```

## Backend Endpointleri

| Metot | Endpoint | Açıklama |
|---|---|---|
| GET | `/` | Servis bilgisi ve endpoint listesini döndürür |
| GET | `/api/health` | Backend servis durumunu döndürür |
| GET | `/api/notifications` | Kayıtlı bildirimleri listeler |
| GET | `/api/notifications/{id}` | Belirli bir bildirimi döndürür |
| POST | `/api/notifications` | Yeni bildirim alır |
| DELETE | `/api/notifications` | Bellekteki bildirim listesini temizler |

## Notification Formatı

Backend’e gönderilen temel bildirim formatı:

```json
{
  "source": "simulator",
  "type": "info",
  "title": "Bilgilendirme bildirimi",
  "message": "Simulator tarafından oluşturulan normal bilgilendirme mesajıdır.",
  "deduplicationKey": "info-20260708104016",
  "createdAt": "2026-07-08T10:40:16Z"
}
```

Backend geçerli mesajları ortak bir envelope formatına dönüştürerek saklar ve listeler.

## Backend Davranışları

Backend tarafında şu temel davranışlar uygulanmıştır:

- Zorunlu alan kontrolü yapılır.
- Eksik veya bozuk mesajlar `400 Bad Request` ile reddedilir.
- Aynı `deduplicationKey` değerine sahip mesajlar tekrar kaydedilmez.
- Duplicate mesajlar `409 Conflict` ile reddedilir.
- Geçerli mesajlar bellekte tutulur ve liste endpointinden döndürülür.

## Simulator Senaryoları

Mevcut simulator aşağıdaki test senaryolarını üretmektedir:

| Senaryo | Beklenen Sonuç |
|---|---|
| Normal info mesajı | Backend tarafından kabul edilir ve frontend’de listelenir |
| Warning mesajı | Backend tarafından kabul edilir ve frontend’de listelenir |
| Error mesajı | Backend tarafından kabul edilir ve frontend’de listelenir |
| Duplicate mesaj ilk gönderim | Backend tarafından kabul edilir |
| Duplicate mesaj tekrar gönderim | Backend tarafından duplicate olarak reddedilir |
| Bozuk/eksik mesaj | Backend tarafından doğrulama hatasıyla reddedilir |

Frontend ekranında yalnızca geçerli mesajlar listelenir. Duplicate ve bozuk mesajlar backend tarafından elenir.

## Frontend Özellikleri

- Bildirim listesini backend API’den çeker.
- Belirli aralıklarla otomatik yenilenir.
- Manuel yenileme butonu içerir.
- Backend bağlantı durumunu gösterir.
- Son güncelleme zamanını gösterir.
- Toplam, info, warning ve error bildirim sayılarını gösterir.
- Bildirim tiplerini renkli etiketlerle ayırır.
- Duplicate ve bozuk mesajlar frontend’e düşmez; backend tarafından elenir.

## Docker Hedefi

Proje sonunda sistem aşağıdaki adımlarla herhangi bir makinede ayağa kaldırılabilir durumda olacaktır:

```bash
git clone <repo-adresi>
cd <repo-klasörü>
docker compose up -d --build
docker compose ps
docker compose logs -f connector
docker compose down
```

Kesin port bilgileri, servis adları ve container yapılandırmaları Docker Compose aşaması tamamlandığında README içerisinde güncellenecektir.