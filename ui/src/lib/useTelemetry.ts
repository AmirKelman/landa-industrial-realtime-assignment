import { HubConnection, HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { useEffect, useMemo, useRef, useState } from "react";
import { SIGNALR_TELEMETRY_HUB_URL } from "./config";
import { TelemetryMessage } from "./types";

type TelemetryState = {
  status: "disconnected" | "connecting" | "connected";
  lastError?: string;
  totalMessages: number;
  lastMessageAt?: string;
  latestBySensor: Record<string, TelemetryMessage>;
  recent: TelemetryMessage[];
  messagesPerSecond: number;
};

export function useTelemetry(maxRecent = 200): TelemetryState {
  const [status, setStatus] = useState<TelemetryState["status"]>("disconnected");
  const [lastError, setLastError] = useState<string | undefined>(undefined);
  const [totalMessages, setTotalMessages] = useState(0);
  const [lastMessageAt, setLastMessageAt] = useState<string | undefined>(undefined);
  const [latestBySensor, setLatestBySensor] = useState<Record<string, TelemetryMessage>>({});
  const [recent, setRecent] = useState<TelemetryMessage[]>([]);
  const [messagesPerSecond, setMessagesPerSecond] = useState(0);

  const connectionRef = useRef<HubConnection | null>(null);
  const mpsCounter = useRef(0);

  const hubUrl = useMemo(() => SIGNALR_TELEMETRY_HUB_URL, []);

  useEffect(() => {
    const conn = new HubConnectionBuilder()
      .withUrl(hubUrl, { withCredentials: false })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    connectionRef.current = conn;

    conn.onreconnecting((err) => {
      setStatus("connecting");
      if (err) setLastError(err.message);
    });

    conn.onreconnected(() => {
      setStatus("connected");
      setLastError(undefined);
    });

    conn.onclose((err) => {
      setStatus("disconnected");
      if (err) setLastError(err.message);
    });

    conn.on("telemetry", (msg: TelemetryMessage) => {
      mpsCounter.current += 1;
      setTotalMessages((x) => x + 1);
      setLastMessageAt(new Date().toISOString());

      setLatestBySensor((prev) => ({ ...prev, [msg.sensorId]: msg }));
      setRecent((prev) => {
        const next = [msg, ...prev];
        return next.length > maxRecent ? next.slice(0, maxRecent) : next;
      });
    });

    let mpsTimer: number | undefined;
    const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

    const startWithRetry = async () => {
      let attempt = 0;
      while (true) {
        try {
          setStatus("connecting");
          await conn.start();
          setStatus("connected");
          setLastError(undefined);

          mpsTimer = window.setInterval(() => {
            setMessagesPerSecond(mpsCounter.current);
            mpsCounter.current = 0;
          }, 1000);
          return;
        } catch (e) {
          // Start can fail transiently during negotiation even if the next attempt succeeds.
          setStatus("disconnected");
          setLastError(e instanceof Error ? e.message : String(e));
          attempt += 1;
          const backoffMs = Math.min(5000, 250 * 2 ** Math.min(attempt, 4));
          await sleep(backoffMs);
        }
      }
    };

    void startWithRetry();

    return () => {
      if (mpsTimer) window.clearInterval(mpsTimer);
      conn.stop().catch(() => {});
    };
  }, [hubUrl, maxRecent]);

  return {
    status,
    lastError,
    totalMessages,
    lastMessageAt,
    latestBySensor,
    recent,
    messagesPerSecond,
  };
}

