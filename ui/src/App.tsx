import { Link, Route, Routes } from "react-router-dom";
import { useMemo, useState } from "react";
import { useTelemetry } from "./lib/useTelemetry";
import { TelemetryMessage } from "./lib/types";

export function App() {
  const t = useTelemetry(400);
  const [selectedSensorId, setSelectedSensorId] = useState("sensor-01");

  const sortedSensors = useMemo(() => {
    const ids = Object.keys(t.latestBySensor);
    return ids.sort();
  }, [t.latestBySensor]);

  const selectedLatest: TelemetryMessage | undefined = t.latestBySensor[selectedSensorId];
  const recentSelected = useMemo(
    () => t.recent.filter((m) => m.sensorId === selectedSensorId).slice(0, 50),
    [t.recent, selectedSensorId],
  );

  return (
    <div style={{ fontFamily: "system-ui, sans-serif", padding: 16 }}>
      <h2>Landa Industrial Real-Time Demo</h2>
      <div
        style={{
          display: "flex",
          gap: 16,
          alignItems: "baseline",
          marginBottom: 8,
          padding: 12,
          border: "1px solid #ddd",
          borderRadius: 8,
        }}
      >
        <div>
          <div style={{ fontSize: 12, opacity: 0.8 }}>SignalR</div>
          <div style={{ fontWeight: 600 }}>{t.status}</div>
        </div>
        <div>
          <div style={{ fontSize: 12, opacity: 0.8 }}>Msgs/sec</div>
          <div style={{ fontWeight: 600 }}>{t.messagesPerSecond}</div>
        </div>
        <div>
          <div style={{ fontSize: 12, opacity: 0.8 }}>Total msgs</div>
          <div style={{ fontWeight: 600 }}>{t.totalMessages}</div>
        </div>
        <div>
          <div style={{ fontSize: 12, opacity: 0.8 }}>Last msg</div>
          <div style={{ fontWeight: 600 }}>
            {t.lastMessageAt ? new Date(t.lastMessageAt).toLocaleTimeString() : "—"}
          </div>
        </div>
        <div style={{ flex: 1 }} />
        <div style={{ maxWidth: 520, color: "#b00020" }}>
          {t.lastError ? <span style={{ fontSize: 12 }}>Error: {t.lastError}</span> : null}
        </div>
      </div>

      <nav style={{ display: "flex", gap: 12, marginBottom: 16 }}>
        <Link to="/">Overview</Link>
        <Link to="/sensor">Sensor</Link>
        <Link to="/ops">Ops</Link>
      </nav>

      <Routes>
        <Route
          path="/"
          element={
            <div>
              <h3 style={{ marginTop: 0 }}>Overview (Page 1)</h3>
              <div style={{ fontSize: 13, opacity: 0.8, marginBottom: 8 }}>
                Live table is driven only by SignalR messages (no polling).
              </div>

              <div style={{ overflow: "auto", border: "1px solid #eee", borderRadius: 8 }}>
                <table style={{ width: "100%", borderCollapse: "collapse" }}>
                  <thead>
                    <tr style={{ textAlign: "left", background: "#fafafa" }}>
                      <th style={{ padding: 10, borderBottom: "1px solid #eee" }}>Sensor</th>
                      <th style={{ padding: 10, borderBottom: "1px solid #eee" }}>Value</th>
                      <th style={{ padding: 10, borderBottom: "1px solid #eee" }}>Timestamp (UTC)</th>
                    </tr>
                  </thead>
                  <tbody>
                    {sortedSensors.length === 0 ? (
                      <tr>
                        <td style={{ padding: 10 }} colSpan={3}>
                          Waiting for telemetry…
                        </td>
                      </tr>
                    ) : (
                      sortedSensors.map((id) => {
                        const m = t.latestBySensor[id];
                        return (
                          <tr key={id}>
                            <td style={{ padding: 10, borderBottom: "1px solid #f2f2f2" }}>
                              {id}
                            </td>
                            <td style={{ padding: 10, borderBottom: "1px solid #f2f2f2" }}>
                              {m.value.toFixed(3)}
                            </td>
                            <td style={{ padding: 10, borderBottom: "1px solid #f2f2f2" }}>
                              {m.timestampUtc}
                            </td>
                          </tr>
                        );
                      })
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          }
        />

        <Route
          path="/sensor"
          element={
            <div>
              <h3 style={{ marginTop: 0 }}>Sensor details (Page 2)</h3>

              <div style={{ display: "flex", gap: 12, alignItems: "center", marginBottom: 12 }}>
                <label style={{ fontSize: 13, opacity: 0.9 }}>Sensor</label>
                <select
                  value={selectedSensorId}
                  onChange={(e) => setSelectedSensorId(e.target.value)}
                  style={{ padding: "6px 8px" }}
                >
                  {Array.from({ length: 20 }).map((_, i) => {
                    const id = `sensor-${String(i + 1).padStart(2, "0")}`;
                    return (
                      <option key={id} value={id}>
                        {id}
                      </option>
                    );
                  })}
                </select>
              </div>

              <div style={{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: 12 }}>
                <div style={{ border: "1px solid #eee", borderRadius: 8, padding: 12 }}>
                  <div style={{ fontSize: 12, opacity: 0.8 }}>Latest value</div>
                  <div style={{ fontSize: 26, fontWeight: 700 }}>
                    {selectedLatest ? selectedLatest.value.toFixed(3) : "—"}
                  </div>
                  <div style={{ fontSize: 12, opacity: 0.8 }}>
                    {selectedLatest ? selectedLatest.timestampUtc : "Waiting for data…"}
                  </div>
                </div>

                <div style={{ border: "1px solid #eee", borderRadius: 8, padding: 12 }}>
                  <div style={{ fontSize: 12, opacity: 0.8, marginBottom: 8 }}>Recent (last 50)</div>
                  <div style={{ maxHeight: 260, overflow: "auto", fontFamily: "ui-monospace, SFMono-Regular, Menlo, monospace", fontSize: 12 }}>
                    {recentSelected.length === 0 ? (
                      <div>Waiting…</div>
                    ) : (
                      recentSelected.map((m, idx) => (
                        <div key={`${m.sensorId}-${m.timestampUtc}-${idx}`}>
                          {m.timestampUtc}  value={m.value.toFixed(3)}
                        </div>
                      ))
                    )}
                  </div>
                </div>
              </div>
            </div>
          }
        />

        <Route
          path="/ops"
          element={
            <div>
              <h3 style={{ marginTop: 0 }}>System/Ops (Page 3)</h3>
              <ul style={{ marginTop: 8 }}>
                <li>
                  <b>SignalR status</b>: {t.status}
                </li>
                <li>
                  <b>Msgs/sec (client)</b>: {t.messagesPerSecond}
                </li>
                <li>
                  <b>Known sensors</b>: {Object.keys(t.latestBySensor).length} / 20
                </li>
                <li>
                  <b>Last error</b>: {t.lastError ?? "—"}
                </li>
              </ul>

              <div style={{ fontSize: 13, opacity: 0.85, marginTop: 12 }}>
                This page is intentionally “ops-ish”: it helps validate the real-time pipeline
                (Redis → REST API → SignalR → UI) without adding any polling.
              </div>
            </div>
          }
        />
      </Routes>
    </div>
  );
}

