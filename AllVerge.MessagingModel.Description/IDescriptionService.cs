using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

using System.Net.Http.Headers;
using System.ServiceModel.Channels;

namespace AllVerge.MessagingModel.Description
{
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Actions.Attributes;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;

    [ResourceContract(Namespace = DescriptionConstants.Namespace, Name = DescriptionConstants.DescriptionServiceName)]
    public interface IDescriptionService
    {
        [GetResourceAction("/import/{documentType}@{documentUrl}")]
        Stream ImportDescription(string documentType, string documentUrl);

        [GetResourceAction("/document/{documentType}/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}?target={documentTarget}")]
        Stream ExportDescription(string documentType, string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string documentTarget);

        [GetResourceAction("/all?$top={pageSize}&$skip={skip}")]
        Stream GetDescriptions(int pageSize, int skip);

        [GetResourceAction("/{descriptionKey}")]
        Stream GetDescription(string descriptionKey);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}")]
        Stream GetConnector(string descriptionKey, string connectorNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}")]
        Stream GetConnection(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}")]
        Stream GetInteraction(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Inputs/{messageNameOrIndex}")]
        Stream GetInteractionInputMessage(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Inputs/{messageNameOrIndex}?form={formName}")]
        Stream GetInteractionInputMessageForm(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string formName);

        [PostResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Inputs/{messageNameOrIndex}")]
        Message InvokeInteractionInputMessageForm(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Inputs/{messageNameOrIndex}/{potentialPath}")]
        Stream GetInteractionInputBlock(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Inputs/{messageNameOrIndex}/{potentialPath}/{potentialName}")]
        Stream GetInteractionInputPotentialPartitionType(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath, string potentialName);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Outputs/{messageNameOrIndex}")]
        Stream GetInteractionOutputMessage(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Outputs/{messageNameOrIndex}/{potentialPath}")]
        Stream GetInteractionOutputBlock(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Outputs/{messageNameOrIndex}/{potentialPath}/{potentialName}")]
        Stream GetInteractionOutputPotentialPartitionType(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath, string potentialName);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Faults/{messageNameOrIndex}")]
        Stream GetInteractionFaultMessage(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Faults/{messageNameOrIndex}/{potentialPath}")]
        Stream GetInteractionFaultBlock(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath);

        [GetResourceAction("/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/Faults/{messageNameOrIndex}/{potentialPath}/{potentialName}")]
        Stream GetInteractionFaultPotentialPartitionType(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string messageNameOrIndex, string potentialPath, string potentialName);

        [PostResourceAction("")]
        Stream CreateDescription(Stream descriptionStream, MediaTypeHeaderValue mediaTypeHeaderValue);

        [PutResourceAction("/{descriptionKey}")]
        Stream SaveDescription(string descriptionKey, Stream descriptionStream, MediaTypeHeaderValue mediaTypeHeaderValue);

        [DeleteResourceAction("/{descriptionKey}")]
        void DeleteDescription(string descriptionKey);

        [GetResourceAction("/register/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}")]
        Stream GetConnectionInteractionReferrals(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex);

        [PostResourceAction("/register/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}?match={matchType}&invalidates={invalidates}&ttl={ttl}&concurrency={concurrency}&sla={sla}&timeout={timeout}&via={via}")]
        void RegisterConnectionInteractionReferral(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, String via);

        [PutResourceAction("/register/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}?refId={referralId}&match={matchType}&invalidates={invalidates}&ttl={ttl}&concurrency={concurrency}&sla={sla}&timeout={timeout}&via={via}")]
        void UpdateConnectionInteractionReferral(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string referralId, string matchType, bool invalidates, ulong ttl, int concurrency, int sla, int timeout, String via);

        [PostResourceAction("/register/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/{referralId}")]
        void InvalidateConnectionInteractionReferral(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string referralId);

        [DeleteResourceAction("/register/{descriptionKey}/{connectorNameOrIndex}/{connectionNameOrIndex}/{interactionNameOrIndex}/{referralId}")]
        void DeleteConnectionInteractionReferral(string descriptionKey, string connectorNameOrIndex, string connectionNameOrIndex, string interactionNameOrIndex, string referralId);
    }
}
