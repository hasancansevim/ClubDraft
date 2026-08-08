import { useEffect, useRef, useState } from 'react';
import * as signalR from '@microsoft/signalr';

export interface UseSignalROptions {
  roomId: string;
  userId: string;
  onParticipantJoined?: (data: any) => void;
  onParticipantReady?: (data: any) => void;
  onDraftReady?: (data: any) => void;
  onDraftTurnAdvanced?: (data: any) => void;
  onPlayerClaimed?: (data: any) => void;
}

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5056';

export const useSignalR = ({ roomId, userId, onParticipantJoined, onParticipantReady, onDraftReady, onDraftTurnAdvanced, onPlayerClaimed }: UseSignalROptions) => {
  const [isConnected, setIsConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const connectionRef = useRef<signalR.HubConnection | null>(null);

  const callbacksRef = useRef({
    onParticipantJoined,
    onParticipantReady,
    onDraftReady,
    onDraftTurnAdvanced,
    onPlayerClaimed
  });

  useEffect(() => {
    callbacksRef.current = {
      onParticipantJoined,
      onParticipantReady,
      onDraftReady,
      onDraftTurnAdvanced,
      onPlayerClaimed
    };
  });

  useEffect(() => {
    if (!roomId || !userId) return;

    let isMounted = true;
    
    const connect = async () => {
      try {
        const connection = new signalR.HubConnectionBuilder()
          .withUrl(`${API_BASE_URL}/gameHub?userId=${userId}&roomId=${roomId}`, {
            skipNegotiation: true,
            transport: signalR.HttpTransportType.WebSockets
          })
          .withAutomaticReconnect()
          .build();

        connection.on('onParticipantJoined', (data) => callbacksRef.current.onParticipantJoined?.(data));
        connection.on('onParticipantReady', (data) => callbacksRef.current.onParticipantReady?.(data));
        connection.on('onDraftReady', (data) => callbacksRef.current.onDraftReady?.(data));
        connection.on('onDraftTurnAdvanced', (data) => callbacksRef.current.onDraftTurnAdvanced?.(data));
        connection.on('onPlayerClaimed', (data) => callbacksRef.current.onPlayerClaimed?.(data));

        await connection.start();
        if (isMounted) {
          setIsConnected(true);
          connectionRef.current = connection;
          console.log('SignalR Connected!');
        } else {
          connection.stop();
        }
      } catch (err: any) {
        console.error('SignalR Connection Error: ', err);
        if (isMounted) {
          setError(err.toString());
        }
      }
    };

    connect();

    return () => {
      isMounted = false;
      if (connectionRef.current) {
        connectionRef.current.stop();
        connectionRef.current = null;
        setIsConnected(false);
      }
    };
  }, [roomId, userId]);

  return { isConnected, error, connection: connectionRef.current };
};
