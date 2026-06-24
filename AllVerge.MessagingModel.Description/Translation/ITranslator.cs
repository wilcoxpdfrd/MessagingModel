using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.Description.Translation
{
    using AllVerge.MessagingModel.Description.Model;

    public interface ITranslator
    {
        string GroupUri { get; }
        string DispatchAction { get; }
        string MessageStyle { get; }
        Connection DispatchConnection { get; }
        Interaction DispatchInteraction { get; }
        Message ValidateAndFormatInputMessage(Message requestMessage, out string accepts);
        Message ValidateAndFormatOuputMessage(Message replyMessage, string accepts);
    }
}