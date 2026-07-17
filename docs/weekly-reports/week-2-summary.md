# Week 2 Summary

## Genel Özet

Bu hafta proje kapsamında connector bileşeninin temel akışı geliştirildi. İlk hafta kurulan simulator, backend ve frontend hattının üzerine protokol bağımsız connector yapısı eklenerek Webhook ve WebSocket kaynaklarından gelen mesajların connector üzerinden backend API’ye aktarılması sağlandı.

Bu aşamada amaç, connector çekirdeğinin farklı kaynak adapterlarıyla çalışabildiğini göstermek ve gelen ham mesajların ortak bir bildirim formatına dönüştürülerek backend’e iletilebildiğini doğrulamaktı.

## Tamamlanan Çalışmalar

- Connector üzerinden backend API’ye mesaj göndermek için publisher yapısı oluşturuldu.
- `INotificationPublisher` arayüzü ve `BackendNotificationPublisher` sınıfı eklendi.
- Connector iç akışını test etmek için geçici `MockSourceAdapter` kullanıldı.
- `WebhookSourceAdapter` oluşturuldu.
- Simulator uygulamasına webhook hedef modu eklendi.
- `Simulator -> WebhookSourceAdapter -> ConnectorCore -> Backend API -> Frontend` akışı test edildi.
- `WebSocketSourceAdapter` oluşturuldu.
- Simulator uygulamasına websocket hedef modu eklendi.
- `Simulator -> WebSocketSourceAdapter -> ConnectorCore -> Backend API -> Frontend` akışı test edildi.
- Eksik veya bozuk mesajların connector tarafından backend’e gönderilmeden elenmesi sağlandı.
- Duplicate mesajların tekrar kaydedilmediği doğrulandı.
- Adapter seçimi `appsettings.json` üzerinden yapılabilir hale getirildi.
- Normal çalışma yapılandırmasında mock adapter devre dışı bırakıldı.
- Webhook ve WebSocket adapterlarının config üzerinden aktif edildiği test edildi.

## Test Edilen Akışlar

### Webhook Akışı

```txt
Simulator -> WebhookSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

Simulator webhook hedef modunda çalıştırıldığında mesajlar connector’ın webhook endpointine gönderildi. Connector bu mesajları ortak formata dönüştürerek backend API’ye aktardı. Geçerli mesajların frontend ekranında `simulator-webhook` kaynağıyla listelendiği doğrulandı.

### WebSocket Akışı

```txt
Simulator -> WebSocketSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

Simulator websocket hedef modunda çalıştırıldığında WebSocket endpointine bağlanarak mesajları connector’a gönderdi. Connector bu mesajları normalize ederek backend API’ye aktardı. Geçerli mesajların frontend ekranında `simulator-websocket` kaynağıyla listelendiği doğrulandı.

## Config Tabanlı Adapter Seçimi

Connector tarafında aktif kaynak adapterları `appsettings.json` dosyasından yönetilebilir hale getirildi.

Örnek yapı:

```json
{
  "Connector": {
    "EnabledAdapters": [
      "webhook",
      "websocket"
    ]
  }
}
```

Bu yapı sayesinde hangi adapterların çalışacağı kod değiştirilmeden belirlenebilmektedir. Testlerde yalnızca config içerisinde belirtilen adapterların register edildiği doğrulandı.

## Karşılaşılan ve Düzeltilen Noktalar

Testler sırasında eksik alan içeren bozuk mesajların connector üzerinden backend’e iletilebildiği fark edildi. Bunun üzerine `ConnectorCore` içerisinde validation kontrolü güçlendirildi. Artık `source`, `type`, `title` veya `message` alanlarından biri eksik olan mesajlar backend’e gönderilmeden elenmektedir.

Ayrıca duplicate mesaj senaryosu test edilerek aynı `deduplicationKey` değerine sahip mesajların backend tarafında tekrar kaydedilmediği doğrulandı.

## Sıradaki Adım

Bir sonraki haftada RabbitMQ ve Redis pub/sub adapterlarının geliştirilmesi planlanmaktadır. Bu kapsamda RabbitMQ ve Redis kaynaklarından gelen mesajların da aynı connector çekirdeği üzerinden işlenmesi hedeflenmektedir.