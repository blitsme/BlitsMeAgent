﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gwupe.Cloud.Messaging.Response
{
    public class LogoutRs : API.Response
    {
        public override string type {
            get { return "Logout-RS"; }
            set { }
        }
    }
}
