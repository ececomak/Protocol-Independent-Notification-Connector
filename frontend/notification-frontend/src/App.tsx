import { useEffect, useMemo, useState } from "react";
import "./App.css";

type NotificationEnvelope = {
  id: string;
  source: string;
  type: string;
  title: string;
  message: string;
  deduplicationKey: string;
  createdAt: string;
  receivedAt: string;
};

const API_BASE_URL = "http://localhost:5199";

function App() {
  const [notifications, setNotifications] = useState<NotificationEnvelope[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [lastUpdatedAt, setLastUpdatedAt] = useState<Date | null>(null);

  const fetchNotifications = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/notifications`);

      if (!response.ok) {
        throw new Error("Notifications could not be fetched.");
      }

      const data: NotificationEnvelope[] = await response.json();
      setNotifications(data);
      setError("");
      setLastUpdatedAt(new Date());
    } catch {
      setError("Backend API'ye ulaşılamadı. Backend servisinin çalıştığından emin olun.");
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchNotifications();

    const intervalId = window.setInterval(() => {
      fetchNotifications();
    }, 3000);

    return () => window.clearInterval(intervalId);
  }, []);

  const stats = useMemo(() => {
    return {
      total: notifications.length,
      info: notifications.filter((item) => item.type.toLowerCase() === "info").length,
      warning: notifications.filter((item) => item.type.toLowerCase() === "warning").length,
      error: notifications.filter((item) => item.type.toLowerCase() === "error").length,
    };
  }, [notifications]);

  return (
    <main className="page">
      <section className="hero">
        <div>
          <p className="eyebrow">Protocol Independent Notification Connector</p>
          <h1>Canlı Bildirim Listesi</h1>
          <p className="description">
            Backend API üzerinden alınan normalize edilmiş bildirimler bu ekranda listelenir.
            Geçerli mesajlar görüntülenirken duplicate ve bozuk mesajlar backend tarafından elenir.
          </p>

          <div className="connection-row">
            <span className={error ? "connection-dot disconnected" : "connection-dot"} />
            <span>{error ? "Backend bağlantısı yok" : "Backend bağlantısı aktif"}</span>
            {lastUpdatedAt && (
              <span className="last-update">
                Son güncelleme: {lastUpdatedAt.toLocaleTimeString("tr-TR")}
              </span>
            )}
          </div>
        </div>

        <button onClick={fetchNotifications}>Yenile</button>
      </section>

      <section className="stats-grid">
        <div className="stat-card">
          <span>Toplam Bildirim</span>
          <strong>{stats.total}</strong>
        </div>

        <div className="stat-card">
          <span>Info</span>
          <strong>{stats.info}</strong>
        </div>

        <div className="stat-card">
          <span>Warning</span>
          <strong>{stats.warning}</strong>
        </div>

        <div className="stat-card">
          <span>Error</span>
          <strong>{stats.error}</strong>
        </div>
      </section>

      {loading && <p className="info-box">Bildirimler yükleniyor...</p>}

      {error && <p className="error-box">{error}</p>}

      {!loading && !error && notifications.length === 0 && (
        <p className="info-box">
          Henüz bildirim bulunmuyor. Simulator çalıştırıldığında geçerli mesajlar burada listelenecek.
        </p>
      )}

      <section className="notification-list">
        {notifications.map((notification) => (
          <article className="notification-card" key={notification.id}>
            <div className="notification-header">
              <div>
                <span className="source">{notification.source}</span>
                <span className={`type ${notification.type.toLowerCase()}`}>
                  {notification.type}
                </span>
              </div>

              <span className="received-time">
                {new Date(notification.receivedAt).toLocaleString("tr-TR")}
              </span>
            </div>

            <h2>{notification.title}</h2>
            <p>{notification.message}</p>

            <div className="meta">
              <span>Dedup Key: {notification.deduplicationKey}</span>
              <span>ID: {notification.id.slice(0, 8)}</span>
            </div>
          </article>
        ))}
      </section>
    </main>
  );
}

export default App;