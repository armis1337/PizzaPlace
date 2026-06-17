import * as signalR from '@microsoft/signalr';
import { API_BASE } from '../api/client';

let connection: signalR.HubConnection | null = null;

export function getConnection(token?: string): signalR.HubConnection {
  if (connection) return connection;

  connection = new signalR.HubConnectionBuilder()
    .withUrl(`${API_BASE}/hubs/orders`, token ? { accessTokenFactory: () => token } : {})
    .withAutomaticReconnect()
    .build();

  return connection;
}

export async function startConnection(token?: string): Promise<signalR.HubConnection> {
  const conn = getConnection(token);

  if (conn.state === signalR.HubConnectionState.Disconnected) {
    await conn.start();
  }

  return conn;
}

export function stopConnection() {
  connection?.stop();
  connection = null;
}
