﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Communication.P2P.RUDP.Packet.API;
using Gwupe.Communication.P2P.RUDP.Socket.API;

namespace Gwupe.Communication.P2P.RUDP.Tunnel.API
{
    public interface ITCPTransport : ITransport
    {
        void CloseConnection(byte connectionId);
        void SendData(ITcpPacket packet);
        ITcpOverUdptSocket OpenConnection(String endPoint);
        ITcpOverUdptSocket OpenConnection(String endPoint, byte protocolId);
        void Listen(string endPoint, Func<ITcpOverUdptSocket, bool> callback);
        void StopListen(string endPoint);
        void ProcessPacket(byte[] packetData);
    }
}
