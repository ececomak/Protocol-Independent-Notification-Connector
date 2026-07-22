# Week 3 Summary

## Genel Özet

Bu hafta protokol bağımsız bildirim connector projesinde RabbitMQ ve Redis pub/sub entegrasyonları üzerinde çalışıldı. Önceki haftalarda hazırlanan connector çekirdeği ve Webhook/WebSocket adapter yapısına ek olarak RabbitMQ ve Redis kaynakları da sisteme dahil edildi.

Bu kapsamda RabbitMQ ve Redis servisleri Docker Compose ile local ortamda çalıştırıldı. Connector tarafında bu kaynaklardan gelen mesajları dinleyen adapter yapıları oluşturuldu. Simulator uygulaması da RabbitMQ ve Redis üzerinden test senaryoları gönderebilecek şekilde güncellendi.

## Tamamlanan Çalışmalar

- RabbitMQ servisi Docker Compose yapısına eklendi.
- RabbitMQ Management paneli üzerinden servis durumu kontrol edildi.
- RabbitMQ için `notifications` kuyruğu kullanıldı.
- Connector tarafında RabbitMQ adapter yapısı oluşturuldu.
- RabbitMQ üzerinden gelen mesajların connector tarafından alınıp backend API’ye aktarılması sağlandı.
- Simulator uygulamasına `rabbitmq` hedef modu eklendi.
- `Simulator -> RabbitMQ -> Connector -> Backend -> Frontend` akışı test edildi.

- Redis servisi Docker Compose yapısına eklendi.
- Redis container çalışması `redis-cli ping` komutu ile kontrol edildi.
- Redis pub/sub için `notifications` kanalı kullanıldı.
- Connector tarafında Redis adapter yapısı oluşturuldu.
- Redis pub/sub üzerinden gelen mesajların connector tarafından alınıp backend API’ye aktarılması sağlandı.
- Simulator uygulamasına `redis` hedef modu eklendi.
- `Simulator -> Redis pub/sub -> Connector -> Backend -> Frontend` akışı test edildi.

## Test Edilen Akışlar

```txt
Simulator -> RabbitMQ -> RabbitMQSourceAdapter -> ConnectorCore -> Backend API -> Frontend

Simulator -> Redis pub/sub -> RedisSourceAdapter -> ConnectorCore -> Backend API -> Frontend
```

RabbitMQ ve Redis testlerinde simulator tarafından info, warning, error, duplicate ve bozuk mesaj senaryoları gönderildi. Geçerli mesajların frontend ekranında listelendiği, duplicate mesajların tekrar kaydedilmediği ve bozuk mesajların frontend’e düşmediği doğrulandı.

## Config Durumu

Connector tarafında aktif adapter seçimi config üzerinden yönetilmeye devam etmektedir. Bu hafta `rabbitmq` ve `redis` adapterları da mevcut config yapısına dahil edilmiştir.

Mevcut aktif kaynaklar:

```txt
webhook
websocket
rabbitmq
redis
```

Bu yapı sayesinde connector çekirdeği protokol detaylarına bağımlı olmadan farklı kaynaklardan gelen mesajları aynı ortak akışta işleyebilmektedir.

## Sıradaki Adım

Bir sonraki aşamada proje dayanıklılık ve final teslim hazırlığına odaklanacaktır. Bu kapsamda backend’e ulaşılamadığında mesajların kaybolmaması, bağlantı kopmalarında yeniden bağlanma davranışları, tüm bileşenlerin Dockerfile dosyalarının hazırlanması ve sistemin tek `docker compose up` komutu ile ayağa kaldırılması hedeflenmektedir.