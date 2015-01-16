﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace TNL.NET.Network
{
    public enum NetError
    {
        NoError,
        InvalidPacketProtocol,
        WouldBlock,
        UnknownError
    }

    public class TNLSocket
    {
        public const UInt32 MaxPacketDataSize = 1490;

        private Boolean _needRun;
        private readonly UdpClient _listener;

        public Queue<Tuple<IPEndPoint, Byte[]>> PacketsToBeHandled = new Queue<Tuple<IPEndPoint, Byte[]>>(); 

        public TNLSocket(Int32 port)
        {
            _listener = new UdpClient(port);
            _listener.BeginReceive(OnEndReceive, null);

            _needRun = true;
        }

        private void OnEndReceive(IAsyncResult result)
        {
            try
            {
                var ep = new IPEndPoint(0, 0);

                var buff = _listener.EndReceive(result, ref ep);

                if (buff != null && buff.Length > 0)
                    PacketsToBeHandled.Enqueue(new Tuple<IPEndPoint, Byte[]>(ep, buff));
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Socket closed, stopping Receiving!");
                return;
            }
            catch (SocketException se)
            {
                if (se.SocketErrorCode != SocketError.ConnectionReset)
                {
                    Console.WriteLine("Valami hiba (fogadás)!");
                    Console.WriteLine(se);
                }
            }
            catch (Exception e)
            {
                
                Console.WriteLine("Valami hiba (fogadás)!");
                Console.WriteLine(e);
            }

            if (_needRun && _listener != null)
                _listener.BeginReceive(OnEndReceive, null);
        }

        public void Stop()
        {
            _needRun = false;
        }

        public NetError Send(IPEndPoint iep, Byte[] buffer, UInt32 bufferSize)
        {
            try
            {
                using (var sw = new StreamWriter(@"C:\ki-raw-uj-" + DateTime.Now.Minute + "-" + DateTime.Now.Second + "-" + DateTime.Now.Millisecond + ".txt"))
                    sw.Write(BitConverter.ToString(buffer, 0, (Int32) bufferSize));

                _listener.BeginSend(buffer, (Int32) bufferSize, iep, OnEndSend, null);

                return NetError.NoError;
            }
            catch
            {
                return NetError.UnknownError;
            }
        }

        public void OnEndSend(IAsyncResult result)
        {
            try
            {
                _listener.EndSend(result);
            }
            catch
            {
                Console.WriteLine("Valami hiba (küldés)!");
            }

        }
    }
}