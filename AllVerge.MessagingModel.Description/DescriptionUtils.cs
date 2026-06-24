using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;

using System.Xml.Serialization;
using System.Xml.Schema;

namespace AllVerge.MessagingModel.Description
{
    using AllVerge.SystemPrimitives.Logging;
    using AllVerge.SystemPrimitives.Collections.Concurrent;
    using AllVerge.SystemPrimitives.Net;

    using AllVerge.MessagingModel.Description.Model;

    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;

    using AllVerge.MessagingModel.MarkupPrimitives.Xml;

    public class DescriptionUtils
    {
        private static readonly String MANAGED_DESCRIPTION_FILE_EXT = ".protocol.xml";
        private static readonly String MANAGED_DESCRIPTION_FILE_SPEC = $"*{MANAGED_DESCRIPTION_FILE_EXT}";
        private static readonly String MANAGED_DESCRIPTION_INDEX_FILE_NAME = @"ManagedDescriptions";
        private static readonly String MANAGED_DESCRIPTION_INDEX_FILE_EXT = "index";

        public class DescriptionIndexer
        {
            static int flag = 0;
            static InteractionDescriptionCollection indexedDescriptions = null;
            static AggregateException indexingExceptions = null;

            public static InteractionDescriptionCollection IndexedDescriptions
            {
                get
                {
                    if (indexedDescriptions == null)

                        throw new NullReferenceException($"{nameof(IndexedDescriptions)} is null.  Please call {nameof(IndexContractDescriptionActions)} before using this property.");

                    return indexedDescriptions;
                }
            }

            public static AggregateException IndexingExceptions
            {
                get
                {
                    return indexingExceptions;
                }
            }

            public static void IndexContractDescriptionActions(Type loggerType)
            {
                IndexContractDescriptionActions(loggerType.FullName);
            }

            public static void IndexContractDescriptionActions(String loggerType)
            {
                if (loggerType == null)

                    throw new ArgumentNullException("loggerType");

                if (indexedDescriptions == null)
                {
                    if (Interlocked.Exchange(ref flag, 1) == 0)
                    {
                        ThreadPool.QueueUserWorkItem(GetIndexContractDescriptionActionsCallback(), loggerType);
                    }
                }
            }

            private static WaitCallback GetIndexContractDescriptionActionsCallback()
            {
                return new WaitCallback((state) =>
                {
                    if (indexedDescriptions == null)
                        
                        indexedDescriptions = new InteractionDescriptionCollection();

                    else

                        indexedDescriptions.Clear();

                    List<Exception> indexingExceptions = new List<Exception>();

                    foreach (Uri groupUri in GetDescriptionCacheGroups())
                    {
                        IEnumerable<String> contractDescriptionfiles = ScanDescriptionFiles(groupUri);

                        foreach (string contractDescriptionfile in contractDescriptionfiles)
                        {
                            try
                            {
                                DescriptionIndexer.IndexContractDescriptionActions(groupUri, contractDescriptionfile, indexingExceptions);
                            }
                            catch (Exception e)
                            {
                                indexingExceptions.Add(e);
                            }
                        }
                    }

                    indexedDescriptions.Save();

                    if (indexingExceptions.Count > 0)
                    {
                        Logger logger = Logger.GetInstance(state.ToString());

                        logger.Log(LoggerType.Info, Severity.WARN, "Index Contract Description Actions faulted.  See exception log.");

                        logger.Log(new AggregateException(indexingExceptions));
                    }

                    Interlocked.Exchange(ref flag, 0);
                });
            }

            private static void IndexContractDescriptionActions(Uri groupUri, string contractDescriptionFilePath, List<Exception> indexingExceptions)
            {
                String targetNamespace;

                ProtocolDescription description = ProtocolDescription.Load(contractDescriptionFilePath, out targetNamespace);

                foreach (Connector connector in description.Connectors)
                {
                    foreach (Connection connection in connector.Connections)
                    {
                        Uri baseUri = connection.GetLocation();

                        foreach (Interaction interaction in connection.Interactions)
                        {
                            String dispatchAction = interaction.GetDispatchAction(baseUri);

                            if (dispatchAction != null)
                            {
                                InteractionMessageStyle messageStyle = interaction.GetInteractionMessageStyle();

                                if (messageStyle != null)

                                    indexedDescriptions.AddOrUpdate(
                                        new InteractionDescription(
                                            groupUri.AbsoluteUri,
                                            dispatchAction,
                                            messageStyle.ToString(),
                                            connector.QualifiedNameToken,
                                            connection.Name,
                                            interaction.Name,
                                            contractDescriptionFilePath));

                                else

                                    indexingExceptions.Add(
                                        new ArgumentException(
                                            string.Format("No message style found for '{0}::{1}' from '{2}'.",
                                                connector.Name,
                                                interaction.Name,
                                                contractDescriptionFilePath)));
                            }
                            else
                            {
                                indexingExceptions.Add(
                                    new ArgumentException(
                                        string.Format("No action found for '{0}::{1}' from '{2}'.",
                                            connector.Name,
                                            interaction.Name,
                                            contractDescriptionFilePath)));
                            }
                        }
                    }
                }
            }

            public static void UpdateContractDescriptionActions(string fullPath, WatcherChangeTypes changeType)
            {
                throw new NotImplementedException();
            }
        }

        public class InteractionDescriptionCollection : 
            ConcurrentKeyedCollection<String, InteractionDescription>, IXmlSerializable
        {
            public InteractionDescriptionCollection() : base(StringComparer.OrdinalIgnoreCase) { }

            protected override string GetKeyForItem(InteractionDescription item)
            {
                return item.ID;
            }

            public void Save()
            {
                using (FileStream fs = File.Open(DescriptionConstants.DESCRIPTIONS_CACHE_PATH + MANAGED_DESCRIPTION_INDEX_FILE_NAME + '.' + MANAGED_DESCRIPTION_INDEX_FILE_EXT, FileMode.Create))
                {
                    Exception e;

                    fs.WriteXml(this.Serialize(), out e);

                    if (e != null)

                        throw e;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <exception cref="FileNotFoundException"></exception>
            /// <returns></returns>
            public static InteractionDescriptionCollection Load()
            {
                String indexFile = DescriptionConstants.DESCRIPTIONS_CACHE_PATH + MANAGED_DESCRIPTION_INDEX_FILE_NAME +'.'+ MANAGED_DESCRIPTION_INDEX_FILE_EXT;

                if (File.Exists(indexFile))
                {
                    XmlElement element;

                    using (FileStream fs = File.OpenRead(indexFile))
                    {
                        Exception e;

                        fs.ReadXml(out element, out e);

                        if (e != null)

                            throw e;
                    }

                    return element.Deserialize<InteractionDescriptionCollection>();
                }

                throw new FileNotFoundException(null, indexFile);
            }

            public XmlSchema GetSchema()
            {
                return null;
            }

            public void ReadXml(XmlReader reader)
            {
                if (reader.LocalName == nameof(InteractionDescriptionCollection))
                {
                    if (reader.ReadToDescendant(nameof(InteractionDescription)))
                    {
                        while (reader.IsStartElement(nameof(InteractionDescription)))
                        {
                            this.AddOrUpdate(reader.ReadSubtree().Deserialize<InteractionDescription>());

                            reader.ReadEndElement();
                        }
                    }

                    reader.Read();
                }
            }

            public void WriteXml(XmlWriter writer)
            {
                foreach (InteractionDescription interactionDescription in this.Values)
                {
                    interactionDescription.Serialize(writer, null);
                }
            }
        }

        public class InteractionDescription
        {
            String groupUri;
            String dispatchAction;
            String messageStyle;
            String connectorQualifiedname;
            String connectionName;
            String interactionName;
            String contractDescriptionFilePath;

            public InteractionDescription()
            {
            }

            public InteractionDescription(String groupUri, String dispatchAction, String messageStyle, String connectorQualifiedname, String connectionName, String interactionName, String contractDescriptionFilePath)
            {
                this.groupUri = groupUri;
                this.dispatchAction = dispatchAction;
                this.messageStyle = messageStyle;
                this.connectorQualifiedname = connectorQualifiedname;
                this.connectionName = connectionName;
                this.interactionName = interactionName;
                this.contractDescriptionFilePath = contractDescriptionFilePath;
            }

            public String GroupUri { get { return this.groupUri; } set { this.groupUri = value; } }
            public String DispatchAction { get { return this.dispatchAction; } set { this.dispatchAction = value; } }
            public String MessageStyle { get { return this.messageStyle; } set { this.messageStyle = value; } }
            public String ConnectorQualifiedname { get { return this.connectorQualifiedname; } set { this.connectorQualifiedname = value; } }
            public String ConnectionName { get { return this.connectionName; } set { this.connectionName = value; } }
            public String InteractionName { get { return this.interactionName; } set { this.interactionName = value; } }
            public String ContractDescriptionFilePath { get { return this.contractDescriptionFilePath; } set { this.contractDescriptionFilePath = value; } }

            public String ID => GetID(this.groupUri, this.DispatchAction, this.MessageStyle);

            public static String GetID(String groupUri, String dispatchAction, String messageStyle)
            {
                return $"{groupUri}>{dispatchAction}>{messageStyle}";
            }
        }

        public static Uri[] GetDescriptionCacheGroups()
        {
            List<Uri> uri = new List<Uri>();

            foreach (String groupDirectory in Directory.EnumerateDirectories($"{DescriptionConstants.DESCRIPTIONS_CACHE_PATH}{Path.DirectorySeparatorChar}urn"))
            {
                DirectoryInfo groupInfo = new DirectoryInfo(groupDirectory);

                uri.Add(new Uri($"urn:{groupInfo.Name}"));
            }

            return uri.ToArray();
        }

        public static IEnumerable<string> ScanDescriptionFiles(Uri groupUri = null, bool returnCachePathUrls = false, bool trimManagedDescriptionFileExtension = false)
        {
            List<string> descriptionfiles = new List<string>();

            String descriptionsCachePath;

            if (groupUri != null)

                descriptionsCachePath = groupUri.GetCachePathUri(new Uri(DescriptionConstants.DESCRIPTIONS_CACHE_PATH)).LocalPath;

            else

                descriptionsCachePath = DescriptionConstants.DESCRIPTIONS_CACHE_PATH;

            FileSystemUtils.Scan(descriptionsCachePath, MANAGED_DESCRIPTION_FILE_SPEC, descriptionfiles);

            if (returnCachePathUrls)
            {
                return descriptionfiles.Select(descriptionFilePath =>
                {
                    if (trimManagedDescriptionFileExtension)

                        descriptionFilePath = descriptionFilePath.TrimEnd(MANAGED_DESCRIPTION_FILE_EXT);

                    return ResourceCacheExtensions.GetUrlFromCachePath(descriptionFilePath, descriptionsCachePath);
                });
            }

            if (trimManagedDescriptionFileExtension)

                return descriptionfiles.Select(descriptionFilePath => descriptionFilePath.TrimEnd(MANAGED_DESCRIPTION_FILE_EXT));

            return descriptionfiles;
        }

        public static bool HasInteractionDescriptionForDispatchAction(string groupUri, string dispatchAction, String messageStyle)
        {
            return DescriptionIndexer.IndexedDescriptions.Contains(InteractionDescription.GetID(groupUri, dispatchAction, messageStyle));
        }

        public static Boolean TryGetManagedDesriptionObjectsForDispatchAction(String groupUri, String dispatchAction, String messageStyle, out Connector connector, out Connection connection, out Interaction interaction, out String connectorTargetNamespace)
        {
            connector = null;
            connection = null;
            interaction = null;
            connectorTargetNamespace = null;

            if (HasInteractionDescriptionForDispatchAction(groupUri, dispatchAction, messageStyle))
            {
                InteractionDescription descriptionItem = DescriptionIndexer.IndexedDescriptions[dispatchAction];

                ProtocolDescription description = ProtocolDescription.Load(descriptionItem.ContractDescriptionFilePath, out connectorTargetNamespace);

                if (description.TryGetConnector(descriptionItem.ConnectorQualifiedname, out connector))
                {
                    if (connector.TryGetConnection(descriptionItem.ConnectionName, out connection))
                    {
                        if (!connection.TryGetInteraction(descriptionItem.InteractionName, out interaction))
                        {
                            connection = null;

                            connectorTargetNamespace = null;
                        }
                    }
                    else

                        connector = null;
                }
            }

            return connectorTargetNamespace != null;
        }
    }
}
