﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace BlitsMe.Communication.P2P.P2P.Tunnel
{
    public class PeerInfo
    {
        public List<IPEndPoint> InternalEndPoints;
        public IPEndPoint InternalEndPoint
        {
            get { return InternalEndPoints == null || InternalEndPoints.Count == 0 ? null : InternalEndPoints[0]; }
            set
            {
                if (InternalEndPoints == null)
                {
                    InternalEndPoints = new List<IPEndPoint>();
                }
                InternalEndPoints.Insert(0,value);
            }
        }
        public IPEndPoint ExternalEndPoint;
        public IPEndPoint FacilitatorRepeatedEndPoint;

        public PeerInfo()
        {
            InternalEndPoints = new List<IPEndPoint>();
        }

        public override String ToString()
        {
            String output = "Internal => ";
            if (InternalEndPoints.Count == 0)
            {
                output += "none, ";
            } else
            {
                output = InternalEndPoints.Aggregate(output, (current, internalEndPoint) => current + (internalEndPoint + ", "));
            }
                
            output += "External => " + ExternalEndPoint;
            output += ", Facilitator => " + FacilitatorRepeatedEndPoint;
            return output;
        }

        public List<IPEndPoint> EndPoints
        {
            get
            {
                var endpoints = new List<IPEndPoint>();
                if (ExternalEndPoint != null)
                {
                    endpoints.Add(ExternalEndPoint);
                }
                if (FacilitatorRepeatedEndPoint != null)
                {
                    endpoints.Add(FacilitatorRepeatedEndPoint);
                }
                if (InternalEndPoints != null)
                {
                    endpoints.AddRange(InternalEndPoints);
                }
                return endpoints;
            }
        }
    }
}
