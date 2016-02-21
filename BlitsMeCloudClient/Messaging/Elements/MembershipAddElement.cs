﻿using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Elements
{
    [DataContract]
    public class MembershipAddElement
    {
        public String uniqueHandle { get; set; }
        public bool player { get; set; }
        public bool admin { get; set; }

    }
}