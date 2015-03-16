﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Gwupe.Communication.P2P.RUDP.Packet.API;
using Gwupe.Communication.P2P.RUDP.Socket.API;
using Gwupe.Communication.P2P.RUDP.Socket;
using Gwupe.Communication.P2P.RUDP.Tunnel.Transport;

namespace Gwupe.Communication.P2P.RUDP.Tunnel.API
{
    /* This interface describes a endpoint manager which can multiplex multiple connections over and IUDPTunnel using named end points (like ports, only named)
     * You can listen on named end points and connect to named end points at the other end of the endPointManager.
     */

    public interface ITransportManager
    {
        ITCPTransport TCPTransport { get; }
        UDPTransport UDPTransport { get; }

        bool IsActive { get; }
        event EventHandler Active;
        event EventHandler Inactive;

        // send data function
        void SendData(IPacket packet);

        // Remote IP
        IPAddress RemoteIp { get; }

        // Add a tunnel
        void AddTunnel(IUDPTunnel tunnel, int priority);

        // Close
        void Close();

        // Close a Tunnel
        void CloseTunnel(IUDPTunnel tunnel);
    }
}
