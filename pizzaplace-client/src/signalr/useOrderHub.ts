import { useEffect, useRef } from 'react';
import * as signalR from '@microsoft/signalr';
import { startConnection } from './hub';

type EventHandlers = Record<string, (...args: unknown[]) => void>;

export function useOrderHub(group: string, handlers: EventHandlers, token?: string) {
  const connRef = useRef<signalR.HubConnection | null>(null);
  const handlersRef = useRef(handlers);

  // Keep ref current on every render so callbacks never go stale
  useEffect(() => {
    handlersRef.current = handlers;
  });

  useEffect(() => {
    let mounted = true;

    startConnection(token).then(conn => {
      if (!mounted) return;
      connRef.current = conn;

      const joinGroup = () => conn.invoke('JoinGroup', group).catch(console.error);
      joinGroup();

      // Re-join after API restart / reconnect
      conn.onreconnected(joinGroup);

      for (const event of Object.keys(handlers)) {
        conn.on(event, (...args) => handlersRef.current[event]?.(...args));
      }
    });

    return () => {
      mounted = false;
      if (connRef.current) {
        connRef.current.invoke('LeaveGroup', group).catch(() => {});
        for (const event of Object.keys(handlers)) {
          connRef.current.off(event);
        }
      }
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [group, token]);
}
