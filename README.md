# Protocol Independent Notification Connector

Bu proje, farklı protokollerden gelen bildirim mesajlarını ortak bir formata dönüştürerek backend üzerinden işleyen ve frontend arayüzünde canlı olarak listeleyen uçtan uca bir sistemdir.

## Amaç

RabbitMQ, WebSocket, Redis ve Webhook kaynaklarından gelen mesajların protokol bağımsız bir connector yapısı üzerinden normalize edilmesi hedeflenmektedir.

## Bileşenler

- Simulator
- Connector
- Backend
- Frontend

## Planlanan Proje Yapısı

```txt
protocol-independent-notification-connector/
├── simulator/      # .NET 10 console application that produces test messages
├── connector/      # .NET 10 worker service with protocol-independent connector core
├── backend/        # ASP.NET Core Minimal API for ingesting and listing notifications
├── frontend/       # React + Vite web interface for live notification list
├── docs/           # Weekly reports and project documentation
└── docker-compose.yml

## İlk Hafta Hedefi

İlk hafta kapsamında simulator, backend ve frontend arasında basit bir uçtan uca akış kurulacaktır.

Akış:

```txt
Simulator -> Backend API -> Frontend Liste
