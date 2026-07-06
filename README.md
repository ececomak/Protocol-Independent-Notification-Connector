# Protocol Independent Notification Connector

Bu proje, farklı protokollerden gelen bildirim mesajlarını ortak bir formata dönüştürerek backend üzerinden işleyen ve frontend arayüzünde canlı olarak listeleyen uçtan uca bir sistemdir.

## Amaç

RabbitMQ, WebSocket, Redis ve Webhook kaynaklarından gelen mesajların protokol bağımsız bir connector yapısı üzerinden normalize edilmesi hedeflenmektedir.

## Bileşenler

- Simulator
- Connector
- Backend
- Frontend

## İlk Hafta Hedefi

İlk hafta kapsamında simulator, backend ve frontend arasında basit bir uçtan uca akış kurulacaktır.

Akış:

```txt
Simulator -> Backend API -> Frontend Liste