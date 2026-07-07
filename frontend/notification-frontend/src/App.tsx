import { useEffect, useState } from "react";
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

  const fetchNotifications = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/notifications`);

      if (!response.ok) {
        throw new Error("Notifications could not be fetched.");
      }

      const data = await response.json();
      setNotifications(data);
      setError("");
    } catch {
      setError("Backend API'ye ulaşılamadı.");
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

  return (
    <main className="page">
      <section className="hero">
        <div>
          <p className="eyebrow">Protocol Independent Notification Connector</p>
          <h1>Canlı Bildirim Listesi</h1>
          <p className="description">
            Backend API üzerinden alınan normalize edilmiş bildirimler bu ekranda listelenir.
          </p>
        </div>

        <button onClick={fetchNotifications}>Yenile</button>
      </section>

      <section className="status-card">
        <div>
          <span>Toplam Bildirim</span>
          <strong>{notifications.length}</strong>
        </div>
        <div>
          <span>Backend Durumu</span>
          <strong>{error ? "Bağlantı Yok" : "Bağlı"}</strong>
        </div>
      </section>

      {loading && <p className="info">Bildirimler yükleniyor...</p>}

      {error && <p className="error">{error}</p>}

      {!loading && !error && notifications.length === 0 && (
        <p className="info">Henüz bildirim bulunmuyor.</p>
      )}

      <section className="notification-list">
        {notifications.map((notification) => (
          <article className="notification-card" key={notification.id}>
            <div className="notification-header">
              <span className="source">{notification.source}</span>
              <span className="type">{notification.type}</span>
            </div>

            <h2>{notification.title}</h2>
            <p>{notification.message}</p>

            <div className="meta">
              <span>Dedup Key: {notification.deduplicationKey}</span>
              <span>
                Received: {new Date(notification.receivedAt).toLocaleString("tr-TR")}
              </span>
            </div>
          </article>
        ))}
      </section>
    </main>
  );
}

export default App;