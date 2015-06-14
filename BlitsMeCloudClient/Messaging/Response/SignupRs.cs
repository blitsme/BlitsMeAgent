﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Gwupe.Cloud.Messaging.Response
{
    [DataContract]
    public class SignupRs : API.Response
    {
        public const String SignupErrorPasswordComplexity = "PASSWORD_COMPLEXITY";
        public const String SignupErrorUserExists = "USER_EXISTS";
        public const String SignupErrorEmailAddressInUse = "EMAIL_EXISTS";
        public const String SignupErrorEmailAddressInvalid = "EMAIL_INVALID";

        public override string type { get { return "Signup-RS"; } set { } }

        [DataMember] public List<String> signupErrors;
    }
}
