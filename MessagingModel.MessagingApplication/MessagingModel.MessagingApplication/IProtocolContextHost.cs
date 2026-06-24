using System;
using System.Collections.Generic;
using System.Text;

namespace AllVerge.MessagingModel.MessagingApplication
{
    /// <summary>
    /// Defines an interface that Hosts a Protocol Context.
    /// </summary>
    /// <typeparam name="TProtocolContext"></typeparam>
    public interface IProtocolContextHost<TProtocolContext> { public TProtocolContext ProtocolContext { get; set; } }

}
