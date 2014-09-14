﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using BlitsMe.Cloud.Messaging.Elements;

namespace BlitsMe.Cloud.Messaging.Response
{
    [DataContract]
    public class LookupUserRs : API.Response
    {
        public override string type
        {
            get { return "LookupUser-RS"; }
            set { }
        }

        [DataMember]
        public string shortCode { get; set; }

        [DataMember]
        public UserElement userElement;
    }
}
