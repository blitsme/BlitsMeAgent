﻿using System;
using System.Runtime.Serialization;

namespace Gwupe.Cloud.Messaging.Request
{
    public class FileSendRequestResponseRq : API.UserToUserRequest
    {
        public override string type { get { return "FileSendRequestResponse-RQ"; } set { } }

        [DataMember]
        public String fileSendId { get; set; }
        [DataMember]
        public bool accepted { get; set; }
    }
}
