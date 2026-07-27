// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime;
using System.Threading;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation.Runtime
{
    internal class ExceptionFactory
    {
        private const string InvalidAsyncResult = "The asynchronous result object used to end this operation was not the object that was returned when the operation was initiated.";
        private const string AsyncResultAlreadyEnded = "End cannot be called twice on an AsyncResult.";

        public static Exception InvalidAsyncResultException(string paramName)
        {
            return new ArgumentNullException(paramName, ExceptionFactory.InvalidAsyncResult);
        }

        public static Exception AsyncResultAlreadyEndedException()
        {
            return new InvalidOperationException(ExceptionFactory.AsyncResultAlreadyEnded);
        }

    }
}
