# Protocol Independent Notification Connector

Bu proje, farklı protokollerden gelen bildirim mesajlarını ortak bir formata dönüştürerek backend üzerinden işleyen ve frontend arayüzünde listeleyen uçtan uca bir sistemdir.

## Amaç

Projenin amacı; RabbitMQ, WebSocket, Redis pub/sub ve Webhook gibi farklı kaynaklardan gelen mesajları protokol bağımsız bir connector yapısı üzerinden ortak bildirim formatına dönüştürmektir.

Connector tarafından normalize edilen mesajlar backend API’ye iletilir. Backend tarafında mesajlar doğrulanır, duplicate kayıtlar elenir ve frontend arayüzünde listelenir.

## Bileşenler

| Bileşen | Açıklama | Teknoloji |
|---|---|---|
| Simulator | Test bildirimleri üretir | .NET 10 Console |
| Connector | Protokol bağımsız çekirdek ve adapter yapısı | .NET 10 Worker Service |
| Backend | Bildirimleri alan ve listeleyen API | ASP.NET Core Minimal API |
| Frontend | Bildirimleri gösteren web arayüzü | React + Vite |
| RabbitMQ | Queue tabanlı mesaj kaynağı | Docker |
| Redis | Pub/sub tabanlı mesaj kaynağı | Docker |

## Çalışan Akışlar

```txt
Simulator -> Backend API -> Frontend

Simulator -> WebhookSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> WebSocketSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> RabbitMQ -> RabbitMQSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> Redis pub/sub -> RedisSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

Connector tarafında aktif adapter seçimi configuration üzerinden yapılır. Docker Compose içinde bu değerler environment variable ile yönetilir.

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

## Docker Compose ile Çalıştırma

Proje Docker Compose ile tek komutla ayağa kaldırılabilir.

```bash
docker compose up -d --build
```

Servislerin durumunu kontrol etmek için:

```bash
docker compose ps -a
```

Connector loglarını izlemek için:

```bash
docker compose logs -f connector
```

Frontend arayüzü:

```txt
http://localhost:3001
```

Backend API:

```txt
http://localhost:8080
```

Bildirim listesini kontrol etmek için:

```txt
http://localhost:8080/api/notifications
```

Sistemi kapatmak için:

```bash
docker compose down
```

## Varsayılan Docker Senaryosu

Docker Compose ile sistem ayağa kalktığında simulator varsayılan olarak RabbitMQ hedefiyle çalışır.

```txt
Simulator -> RabbitMQ -> Connector -> Backend -> Frontend
```

Bu senaryoda frontend ekranında `simulator-rabbitmq` kaynaklı geçerli bildirimlerin listelenmesi beklenir.

## Diğer Protokolleri Test Etme

Docker Compose ayaktayken simulator farklı hedeflerle tekrar çalıştırılabilir.

```bash
docker compose run --rm -e SIMULATOR_TARGET=redis simulator
```

```bash
docker compose run --rm -e SIMULATOR_TARGET=webhook simulator
```

```bash
docker compose run --rm -e SIMULATOR_TARGET=websocket simulator
```

```bash
docker compose run --rm -e SIMULATOR_TARGET=rabbitmq simulator
```

Her çalıştırmada geçerli mesajların frontend ekranında ilgili kaynak adıyla listelenmesi beklenir.

## Backend Endpointleri

| Metot | Endpoint | Açıklama |
|---|---|---|
| GET | `/api/health` | Backend durumunu döndürür |
| GET | `/api/notifications` | Bildirimleri listeler |
| POST | `/api/notifications` | Yeni bildirim alır |
| DELETE | `/api/notifications` | Bildirim listesini temizler |

Bildirim listesini temizlemek için:

```bash
curl -X DELETE http://localhost:8080/api/notifications
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
- Duplicate mesajlar `deduplicationKey` ile tekrar kaydedilmez.
- Backend ulaşılamazsa mesajlar connector içinde buffer’da bekletilir ve tekrar denenir.
- Frontend yalnızca backend’den aldığı geçerli bildirimleri listeler.