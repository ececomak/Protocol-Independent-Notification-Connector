# Connector Design Notes

Bu doküman, protokol bağımsız bildirim connector bileşeninin temel tasarım kararlarını ve ilerleyen aşamalarda geliştirilecek yapıyı özetler.

## Amaç

Connector bileşeninin amacı, farklı protokollerden gelen ham mesajları protokol detaylarından bağımsız şekilde alıp ortak bir bildirim formatına dönüştürmektir.

Connector çekirdeği doğrudan RabbitMQ, WebSocket, Redis veya Webhook kütüphanelerine bağımlı olmayacaktır. Her kaynak kendi adapter sınıfı üzerinden sisteme dahil edilecektir.

## Temel Arayüzler

### IConnector

`IConnector`, kaynak adapterlarının çalışma zamanında sisteme eklenip çıkarılmasını sağlar.

Temel sorumlulukları:

- Kaynak adapterlarını register etmek
- Kaynak adapterlarını unregister etmek
- Kayıtlı adapterları başlatmak
- Kayıtlı adapterları durdurmak
- Normalize edilmiş mesajları dışarıya event olarak iletmek

### ISourceAdapter

`ISourceAdapter`, her protokol kaynağının uygulaması gereken ortak sözleşmedir.

Bu yapı sayesinde RabbitMQ, WebSocket, Redis ve Webhook gibi farklı kaynaklar connector çekirdeğine aynı arayüz üzerinden bağlanabilir.

Temel sorumlulukları:

- Kaynağa bağlanmak
- Kaynaktan ayrılmak
- Ham mesajları `RawMessage` olarak connector çekirdeğine iletmek

## Temel Modeller

### RawMessage

`RawMessage`, adapterlardan gelen ham mesajı temsil eder.

İçerdiği temel bilgiler:

- Adapter adı
- Ham payload
- Alınma zamanı

### NotificationEnvelope

`NotificationEnvelope`, connector tarafından normalize edilen ortak bildirim formatıdır.

İçerdiği temel bilgiler:

- Source
- Type
- Title
- Message
- DeduplicationKey
- CreatedAt

## Register / Unregister Mantığı

Connector çekirdeği, kaynakları doğrudan bilmez. Kaynaklar `Register` metodu ile sisteme eklenir ve `Unregister` metodu ile sistemden çıkarılır.

Bu yaklaşım sayesinde yeni bir protokol adapterı eklenirken connector çekirdeğinin değiştirilmesi gerekmez.

Örnek adapterlar:

- RabbitMqSourceAdapter
- WebSocketSourceAdapter
- RedisSourceAdapter
- WebhookSourceAdapter

## 2. Hafta İçin Plan

İkinci hafta kapsamında connector çekirdeğinin geliştirilmesi ve ilk adapterların eklenmesi hedeflenmektedir.

Öncelikli adımlar:

- Connector core yapısını genişletmek
- WebSocket adapter taslağını oluşturmak
- Webhook adapter taslağını oluşturmak
- Adapterlardan gelen ham mesajları ortak formata dönüştürmek
- Normalize edilen mesajları backend API’ye gönderecek yapı hazırlamak

## Final Hedef

Final mimaride simulator aynı olayları dört farklı protokolden üretecektir:

- RabbitMQ
- WebSocket
- Redis pub/sub
- Webhook

Connector ise bu kaynaklardan gelen mesajları adapterlar aracılığıyla alıp ortak formata dönüştürecek ve backend API’ye iletecektir.