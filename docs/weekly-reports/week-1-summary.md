# Week 1 Summary

Bu hafta protokol bağımsız bildirim connector projesinin ilk haftası kapsamında temel uçtan uca akış kurulmuştur. Amaç, ilerleyen haftalarda geliştirilecek connector mimarisine geçmeden önce simulator, backend ve frontend bileşenlerinin birlikte çalıştığını doğrulamaktır.

## Yapılan Çalışmalar

İlk olarak proje gereksinimleri incelenmiş ve proje yapısı planlanmıştır. Projenin RabbitMQ, WebSocket, Redis pub/sub ve Webhook gibi farklı kaynaklardan gelen mesajları ortak bir formata dönüştüren uçtan uca bir sistem olacağı analiz edilmiştir. Geliştirme ortamı kontrol edilmiş, proje gereksinimi doğrultusunda .NET 10 SDK kurulmuş ve GitHub üzerinde proje reposu oluşturulmuştur.

Backend tarafında ASP.NET Core Minimal API kullanılarak bildirimlerin alınmasını ve listelenmesini sağlayan temel API yapısı geliştirilmiştir. `POST /api/notifications` endpointi ile bildirim alma, `GET /api/notifications` endpointi ile kayıtlı bildirimleri listeleme işlemleri yapılmıştır. Ayrıca backend tarafında zorunlu alan kontrolleri, duplicate mesaj kontrolü ve bozuk mesajların reddedilmesi gibi temel doğrulama davranışları eklenmiştir.

Frontend tarafında React ve Vite kullanılarak canlı bildirim listesi arayüzü oluşturulmuştur. Frontend, backend API’den bildirimleri çekerek ekranda listeleyebilecek hale getirilmiştir. Arayüzde backend bağlantı durumu, son güncelleme zamanı, toplam bildirim sayısı ve info/warning/error türlerine göre sayaçlar gösterilmiştir. Bildirim türleri renkli etiketlerle ayrılmış ve arayüz daha okunabilir hale getirilmiştir.

Simulator tarafında .NET 10 console uygulaması geliştirilmiştir. Önce manuel olarak curl komutlarıyla gönderilen test mesajları daha sonra simulator üzerinden otomatik gönderilecek hale getirilmiştir. Simulator içinde normal info mesajı, warning mesajı, error mesajı, duplicate mesaj ve bozuk/eksik mesaj senaryoları oluşturulmuştur. Bu senaryolarla backend’in geçerli mesajları kaydettiği, duplicate mesajları reddettiği ve bozuk mesajları kabul etmediği test edilmiştir.

Servis adreslerinin doğrudan koda bağlı kalmaması için environment variable desteği eklenmiştir. Frontend tarafında `VITE_API_BASE_URL`, simulator tarafında `BACKEND_URL`, backend tarafında ise `FRONTEND_ORIGINS` değişkenleriyle yapılandırma yapılabilecek hale getirilmiştir. README dosyası local çalıştırma adımları, kullanılan portlar, backend endpointleri, environment değerleri ve simulator senaryoları ile güncellenmiştir.

Hafta sonunda ikinci haftaya hazırlık olarak connector bileşeni için .NET 10 Worker Service projesi oluşturulmuştur. Connector çekirdeğinin temelini oluşturmak amacıyla `IConnector`, `ISourceAdapter`, `RawMessage`, `NotificationEnvelope` ve `ConnectorCore` yapıları hazırlanmıştır. Böylece ilerleyen haftalarda WebSocket, Webhook, RabbitMQ ve Redis adapterlarının eklenebilmesi için temel mimari oluşturulmuştur.

## Mevcut Çalışan Akış

```txt
Simulator -> Backend API -> Frontend Liste