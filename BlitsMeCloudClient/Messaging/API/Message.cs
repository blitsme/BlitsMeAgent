﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Gwupe.Cloud.Messaging.Request;
using Gwupe.Cloud.Messaging.Response;

namespace Gwupe.Cloud.Messaging.API
{
    [DataContract]
    public abstract class Message
    {
        [DataMember(IsRequired = true,Order = 0)]
        public abstract String type { get; set; }
        [DataMember(IsRequired = true,Order = 1)]
        public String id { get; set; }
        [DataMember(IsRequired = true)]
        public DateTime date { get; set; }
    }
}
