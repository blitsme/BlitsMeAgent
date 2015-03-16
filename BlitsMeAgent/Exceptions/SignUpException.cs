﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gwupe.Cloud.Exceptions;
using Gwupe.Cloud.Messaging.Response;

namespace Gwupe.Agent.Exceptions
{
    class SignUpException : Exception
    {
        public List<String> errors;

        public SignUpException(MessageException<SignupRs> signupMessageException)
        {
            errors = signupMessageException.Response.signupErrors;
        }

        public SignUpException()
        {
        }
    }
}
