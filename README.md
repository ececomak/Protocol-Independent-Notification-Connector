# Protocol Independent Notification Connector

Bu proje, farklı protokollerden gelen bildirim mesajlarını ortak bir formata dönüştürerek backend üzerinden işleyen ve frontend arayüzünde listeleyen uçtan uca bir sistemdir.

## Amaç

Projenin amacı; RabbitMQ, WebSocket, Redis pub/sub ve Webhook gibi farklı kaynaklardan gelen mesajları protokol bağımsız bir connector yapısı üzerinden ortak bir bildirim formatına dönüştürmektir.

Connector tarafından normalize edilen mesajlar backend API’ye iletilir. Backend tarafında mesajlar doğrulanır, tekrar eden kayıtlar elenir ve frontend arayüzünde listelenir.

## Bileşenler

| Bileşen | Açıklama | Teknoloji |
|---|---|---|
| Simulator | Test bildirimleri üretir | .NET 10 Console |
| Connector | Protokol bağımsız çekirdek ve adapter yapısı | .NET 10 Worker Service |
| Backend | Bildirimleri alan ve listeleyen API | ASP.NET Core Minimal API |
| Frontend | Bildirimleri gösteren web arayüzü | React + Vite |

## Mevcut Çalışan Akışlar

```txt
Simulator -> Backend API -> Frontend

Simulator -> WebhookSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> WebSocketSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

Webhook ve WebSocket adapterları connector üzerinden çalışmaktadır. Aktif adapter seçimi `appsettings.json` dosyası üzerinden yapılır.

## Proje Yapısı

```txt
protocol-independent-notification-connector/
├── simulator/NotificationSimulator
├── connector/NotificationConnector
├── backend/NotificationBackend
├── frontend/notification-frontend
├── docs/
└── docker-compose.yml
```

## Local Portlar

| Servis | Adres |
|---|---|
| Backend | `http://localhost:5199` |
| Frontend | `http://localhost:5173` |
| Webhook Adapter | `http://localhost:7071/webhook/notifications` |
| WebSocket Adapter | `ws://localhost:7072/ws/notifications` |

## Environment Değerleri

| Bileşen | Değişken | Açıklama |
|---|---|---|
| Frontend | `VITE_API_BASE_URL` | Frontend’in bağlanacağı backend adresi |
| Simulator | `BACKEND_URL` | Backend hedef adresi |
| Simulator | `WEBHOOK_URL` | Webhook hedef adresi |
| Simulator | `WEBSOCKET_URL` | WebSocket hedef adresi |
| Simulator | `SIMULATOR_TARGET` | `backend`, `webhook` veya `websocket` |
| Backend | `FRONTEND_ORIGINS` | CORS için izin verilen frontend adresleri |

## Local Çalıştırma

Projeyi local ortamda çalıştırmak için backend, frontend, connector ve simulator ayrı terminallerde başlatılır.

### Backend

```bash
cd backend/NotificationBackend
dotnet run
```

### Frontend

```bash
cd frontend/notification-frontend
npm install
npm run dev
```

Frontend adresi:

```txt
http://localhost:5173
```

### Connector

```bash
cd connector/NotificationConnector
dotnet run
```

Beklenen durumda Webhook ve WebSocket adapterları aktif olur.

### Simulator - Backend Modu

```bash
cd simulator/NotificationSimulator
dotnet run
```

Bu modda simulator mesajları doğrudan backend API’ye gönderir.

### Simulator - Webhook Modu

Backend, frontend ve connector çalışırken:

```bash
cd simulator/NotificationSimulator
SIMULATOR_TARGET=webhook dotnet run
```

### Simulator - WebSocket Modu

Backend, frontend ve connector çalışırken:

```bash
cd simulator/NotificationSimulator
SIMULATOR_TARGET=websocket dotnet run
```

## Backend Endpointleri

| Metot | Endpoint | Açıklama |
|---|---|---|
| GET | `/api/health` | Backend durumunu döndürür |
| GET | `/api/notifications` | Bildirimleri listeler |
| POST | `/api/notifications` | Yeni bildirim alır |
| DELETE | `/api/notifications` | Bildirim listesini temizler |

Testler arasında bildirim listesini temizlemek için:

```bash
curl -X DELETE http://localhost:5199/api/notifications
```

## Temel Mesaj Formatı

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

## Mevcut Davranışlar

- Webhook ve WebSocket kaynaklarından gelen mesajlar connector tarafından alınır.
- Mesajlar `ConnectorCore` içinde ortak formata dönüştürülür.
- Eksik veya bozuk mesajlar backend’e gönderilmeden elenir.
- Geçerli mesajlar backend API’ye aktarılır.
- Duplicate mesajların tekrar kaydedilmemesi backend tarafında `deduplicationKey` ile kontrol edilir.
- Frontend, backend’den aldığı geçerli bildirimleri listeler.

## Sıradaki Adımlar

Bir sonraki aşamada RabbitMQ ve Redis pub/sub adapterları geliştirilecektir.

Planlanan çalışmalar:

- RabbitMQ adapter oluşturma
- Redis pub/sub adapter oluşturma
- RabbitMQ ve Redis için container yapılarının hazırlanması
- Config üzerinden RabbitMQ ve Redis kaynak seçiminin eklenmesi
- Reconnect ve hata toleransı kontrollerinin geliştirilmesi

## Docker Hedefi

Proje sonunda sistem Docker Compose ile tek komutla ayağa kaldırılabilir hale getirilecektir.

```bash
docker compose up -d --build
```

Docker Compose yapılandırması ilerleyen aşamada tamamlanacaktır.