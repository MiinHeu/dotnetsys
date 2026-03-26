import { useEffect, useRef } from "react";
import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";

export function useSignalR() {
  const qc = useQueryClient();
  const conn = useRef<signalR.HubConnection | null>(null);

  useEffect(() => {
    conn.current = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/vinh-khanh")
      .withAutomaticReconnect()
      .build();

    conn.current.on("PoiCreated", () => qc.invalidateQueries({ queryKey: ["pois"] }));
    conn.current.on("PoiUpdated", () => qc.invalidateQueries({ queryKey: ["pois"] }));
    conn.current.on("TourCreated", () => qc.invalidateQueries({ queryKey: ["tours"] }));
    conn.current.on("TourUpdated", () => qc.invalidateQueries({ queryKey: ["tours"] }));

    conn.current.start().catch(console.error);

    return () => {
      conn.current?.stop();
    };
  }, [qc]);

  return conn.current;
}
