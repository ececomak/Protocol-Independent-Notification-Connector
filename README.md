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

Simulator -> RabbitMQ -> RabbitMQSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> Redis pub/sub -> RedisSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

Aktif adapter seçimi `connector/NotificationConnector/appsettings.json` dosyasındaki `EnabledAdapters` alanı üzerinden yapılır.

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
| RabbitMQ | `localhost:5672` |
| RabbitMQ Management | `http://localhost:15672` |
| Redis | `localhost:6379` |

## Environment Değerleri

| Bileşen | Değişken | Açıklama |
|---|---|---|
| Frontend | `VITE_API_BASE_URL` | Frontend’in bağlanacağı backend adresi |
| Simulator | `SIMULATOR_TARGET` | `backend`, `webhook`, `websocket`, `rabbitmq`, `redis` |
| Simulator | `BACKEND_URL` | Backend hedef adresi |
| Simulator | `WEBHOOK_URL` | Webhook hedef adresi |
| Simulator | `WEBSOCKET_URL` | WebSocket hedef adresi |
| RabbitMQ | `RABBITMQ_HOST`, `RABBITMQ_PORT`, `RABBITMQ_QUEUE` | RabbitMQ bağlantı ayarları |
| Redis | `REDIS_HOST`, `REDIS_PORT`, `REDIS_CHANNEL` | Redis pub/sub bağlantı ayarları |
| Backend | `FRONTEND_ORIGINS` | CORS için izin verilen frontend adresleri |

## Local Çalıştırma

RabbitMQ ve Redis servislerini başlatmak için:

```bash
docker compose up -d
```

Servisleri kontrol etmek için:

```bash
docker compose ps
```

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

Connector çalıştığında config içinde aktif olan adapterlar Register edilir.

### Simulator

Backend, frontend ve connector çalışırken simulator farklı hedeflerle çalıştırılabilir.

Backend modu:

```bash
cd simulator/NotificationSimulator
dotnet run
```

Webhook modu:

```bash
SIMULATOR_TARGET=webhook dotnet run
```

WebSocket modu:

```bash
SIMULATOR_TARGET=websocket dotnet run
```

RabbitMQ modu:

```bash
SIMULATOR_TARGET=rabbitmq dotnet run
```

Redis modu:

```bash
SIMULATOR_TARGET=redis dotnet run
```

Her modda geçerli mesajların frontend ekranında ilgili kaynak adıyla listelenmesi beklenir.

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

- Dört farklı protokolden gelen mesajlar connector adapterları tarafından alınır.
- Mesajlar `ConnectorCore` içinde ortak formata dönüştürülür.
- Eksik veya bozuk mesajlar backend’e gönderilmeden elenir.
- Geçerli mesajlar backend API’ye aktarılır.
- Duplicate mesajların tekrar kaydedilmemesi backend tarafında `deduplicationKey` ile kontrol edilir.
- Frontend, backend’den aldığı geçerli bildirimleri listeler.

## Sıradaki Adımlar

Bir sonraki aşamada proje dayanıklılık ve final Docker yapısına hazırlanacaktır.

Planlanan çalışmalar:

- Backend ulaşılamadığında mesajların kaybolmaması için buffer/retry yapısı
- Bağlantı kopmalarına karşı reconnect kontrolleri
- Her bileşen için Dockerfile hazırlanması
- Backend, frontend, connector, simulator, RabbitMQ ve Redis servislerinin tek docker-compose.yml ile ayağa kaldırılması
- Connector config değerlerinin Docker Compose environment variable ile ezilebilir hale getirilmesi
- README, final özet PDF’i ve demo video hazırlığı

## Docker Hedefi

Proje sonunda sistem tek komutla ayağa kaldırılabilir hale getirilecektir:

```bash
docker compose up -d --build
```

Final Docker Compose yapısında backend, frontend, connector, simulator, RabbitMQ ve Redis birlikte çalışacaktır.