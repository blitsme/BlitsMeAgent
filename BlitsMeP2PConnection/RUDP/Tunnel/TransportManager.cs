﻿using System;
using System.Collections.Generic;
using System.Net;
using Gwupe.Communication.P2P.RUDP.Packet.API;
using Gwupe.Communication.P2P.RUDP.Tunnel.API;
using Gwupe.Communication.P2P.RUDP.Tunnel.Transport;
using log4net;

namespace Gwupe.Communication.P2P.RUDP.Tunnel
{
    /* By adding headers to the packets destined for the UDP endPointManager, we can run multiple end to end connections across
     * the single endPointManager.  This class manages that multiplexing allowing named connections incoming and outgoing, adding, stripping and
     * allocating data to the various upstream handlers.
     * 
     * At this stage, we have all the functionality for handling tcp connections, but we will need to add UDP support and potentially split the
     * functionality into other classes for clarity
     */

    public class TransportManager : ITransportManager
    {
        private const byte PACKET_TYPE_TCP = 1;
        private const byte PACKET_TYPE_UDP = 2;
        private static readonly ILog Logger = LogManager.GetLogger(typeof(TransportManager));
        private readonly Dictionary<String, TunnelContainer> _tunnels;
        private TunnelContainer _mruTunnel;

        public event EventHandler Active;
        public event EventHandler Inactive;

        public bool IsActive { get; private set; }

        public void OnInactive(EventArgs e)
        {
            EventHandler handler = Inactive;
            if (handler != null) handler(this, e);
        }

        public void OnActive(EventArgs e)
        {
            EventHandler handler = Active;
            if (handler != null) handler(this, e);
        }

        public ITCPTransport TCPTransport { get; private set; }
        public UDPTransport UDPTransport { get; private set; }

        public IPAddress RemoteIp
        {
            get
            {
                if (_mruTunnel != null)
                {
                    return _mruTunnel.Tunnel.RemoteIp;
                }
                return null;
            }
        }


        public TransportManager()
        {
            _tunnels = new Dictionary<string, TunnelContainer>();
            TCPTransport = new TCPTransport(this);
            UDPTransport = new UDPTransport(this);
        }

        #region Data Incoming and Outgoing

        private void ProcessTunnelData(byte[] bytes, String id)
        {
            // Get the transport packet type out before
            byte[] packetData = new byte[bytes.Length - 1];
            Array.Copy(bytes, 1, packetData, 0, bytes.Length - 1);
            // Set this tunnel as MRU
            if (id != null && (_mruTunnel == null || !_mruTunnel.Tunnel.Id.Equals(id)) && _tunnels.ContainsKey(id))
            {
#if DEBUG
                Logger.Debug("Received data on tunnel " + id + ", this tunnels is now MRU");
#endif

                _mruTunnel = _tunnels[id];
            }
            if (bytes[0] == PACKET_TYPE_TCP)
            {
                TCPTransport.ProcessPacket(packetData);
            }
            else
            {
                Logger.Error("Error processing tunnel data, packet type not supported " + bytes[0]);
            }
        }

        public void SendData(IPacket packet)
        {
            if (packet is ITcpPacket)
            {
                SendData(packet, PACKET_TYPE_TCP);
            }
            else if (packet is IUdpPacket)
            {
                SendData(packet, PACKET_TYPE_UDP);
            }
            else
            {
                throw new Exception("Cannot send packet, unknown packet type [" + packet.GetType() + "]");
            }
        }

        private void SendData(IPacket packet, byte packetType)
        {
            byte[] data = packet.GetBytes();
            byte[] typedPacketData = new byte[data.Length + 1];
            typedPacketData[0] = packetType;
            Array.Copy(data, 0, typedPacketData, 1, data.Length);
            // Pick a tunnel to send
            if (_mruTunnel == null || !_mruTunnel.Tunnel.IsTunnelEstablished || _mruTunnel.Tunnel.Degraded)
            {
#if DEBUG
                Logger.Debug("Choosing new tunnel for transmission");
#endif
                _mruTunnel = PickTunnel();
            }
#if DEBUG
            Logger.Debug("Using tunnel " + _mruTunnel.Tunnel.Id + " for transmission");
#endif
            _mruTunnel.Tunnel.SendData(typedPacketData);
        }

        #endregion

        public void AddTunnel(IUDPTunnel tunnel, int priority)
        {
            tunnel.ProcessData = ProcessTunnelData;
            tunnel.Disconnected += TunnelOnDisconnected;
            _tunnels.Remove(tunnel.Id);
            _tunnels.Add(tunnel.Id, new TunnelContainer() { Priority = priority, Tunnel = tunnel });
            if (_tunnels.Count == 1)
            {
                Logger.Debug("TransportManager has become active.");
                IsActive = true;
                OnActive(EventArgs.Empty);
            }
        }

        private void TunnelOnDisconnected(object sender, EventArgs args)
        {
            var tunnel = (IUDPTunnel)sender;
            if (_tunnels.ContainsKey(tunnel.Id))
            {
                Logger.Info("Tunnel " + tunnel.Id + " has closed, removing it from transport.");
                _tunnels.Remove(tunnel.Id);
                if (_tunnels.Count == 0)
                {
                    Logger.Debug("All tunnels have disconnected, transport manager is inactive.");
                    IsActive = false;
                    OnInactive(EventArgs.Empty);
                }
            }
        }

        private TunnelContainer PickTunnel()
        {
            var tunnels = new List<TunnelContainer>(_tunnels.Values);
            var degradedTunnels = new List<TunnelContainer>();
            tunnels.Sort();
            foreach (TunnelContainer tunnelContainer in tunnels)
            {
                if (tunnelContainer.Tunnel.IsTunnelEstablished)
                {
                    if (!tunnelContainer.Tunnel.Degraded)
                    {
                        return tunnelContainer;
                    }
                    // Add degraded tunnels to a list for potential use if we can't find a nice tunnel
                    degradedTunnels.Add(tunnelContainer);
                }
            }
            if (degradedTunnels.Count > 0)
            {
                // pick the first of the degraded tunnels if we found no non-degraded ones
                return degradedTunnels[0];
            }
            throw new Exception("Failed to find a suitable tunnel for transmission");
        }

        public void Close()
        {
            Logger.Debug("Closing transports");
            if (TCPTransport != null) TCPTransport.Close();
            if (UDPTransport != null) UDPTransport.Close();
            foreach (var tunnelContainer in _tunnels.Values)
            {
                Logger.Debug("Closing tunnel " + tunnelContainer.Tunnel.Id);
                try
                {
                    tunnelContainer.Tunnel.Close();
                }
                catch (Exception e)
                {
                    Logger.Warn("Closing tunnel " + tunnelContainer.Tunnel.Id + " failed : " + e.Message);
                }
            }
            _tunnels.Clear();
        }

        public void CloseTunnel(IUDPTunnel outgoingTunnel)
        {
            if (_tunnels.ContainsKey(outgoingTunnel.Id))
            {
                Logger.Debug("Removing tunnel " + outgoingTunnel.Id + " from transportManager");
                _tunnels[outgoingTunnel.Id].Tunnel.Close();
                _tunnels.Remove(outgoingTunnel.Id);
            }
        }
    }

    internal class TunnelContainer : IComparable<TunnelContainer>
    {
        internal int Priority;
        internal IUDPTunnel Tunnel;

        public int CompareTo(TunnelContainer other)
        {
            return this.Priority - other.Priority;
        }
    }

}
