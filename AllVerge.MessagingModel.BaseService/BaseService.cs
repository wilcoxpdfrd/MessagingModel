using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

using System.ServiceModel;
using System.ServiceModel.Channels;

using www.allverge.com.core.servicemodel.ws_diagnostic_level_assertion._1._0._0;
using www.allverge.com.core.servicemodel.ws_logging_level_assertion._1._0._0;

using Microsoft.Extensions.DependencyInjection;

namespace AllVerge.MessagingModel.BaseService
{
    using AllVerge.MessagingModel.MessagingApplication;
    using AllVerge.MessagingModel.MessagingFoundation;
    using AllVerge.MessagingModel.MessagingFoundation.Dispatch;
    using AllVerge.MessagingModel.MessagingFoundation.Faults;
    using AllVerge.MessagingModel.RoutingPrimitives;
    using AllVerge.MessagingModel.ServicePrimitives;
    using AllVerge.PolicyPrimitives;
    using AllVerge.SystemPrimitives.Diagnostics;
    using AllVerge.SystemPrimitives.Encodings;
    using AllVerge.SystemPrimitives.Logging;
    using AllVerge.SystemPrimitives.Net;
    using AllVerge.SystemPrimitives.Threading;
    using www.w3.org.ns.ws_policy;

    /// <summary>
    /// The class from which to derive services.
    /// </summary>
    public abstract class BaseService : IBaseService
    {
        private const string DIAGNOSTICS = "diagnostics";
        private const string SERVICE_ACTION_DIAGNOSTICS_CATEGORY = "ServiceAction";

        private PolicyCollection policies;
        private Logger logger = null;
        private IPathEnvironment pathEnvironment;
        private Uri serviceAddress;
        private IServiceScope serviceScope = null;
        private IServiceProvider serviceProvider;

        /// <summary>
        /// Override to create a new instance of a class deriving from <see cref="BaseService"/>.
        /// </summary>
        protected BaseService()
        {
        }

        // Override to create a configured cloned instance of a class (singleton instance) deriving from <see cref="BaseService"/>
        protected BaseService(BaseService derivedSingletonInstance)
        {
            SetConfiguredEnvironment(derivedSingletonInstance.pathEnvironment, derivedSingletonInstance.serviceAddress, derivedSingletonInstance.policies, derivedSingletonInstance.serviceProvider);
        }

        private void SetConfiguredEnvironment(IPathEnvironment pathEnvironment, Uri serviceAddress, PolicyCollection policies, IServiceProvider serviceProvider)
        {
            this.pathEnvironment = pathEnvironment;
            this.serviceAddress = serviceAddress;
            this.policies = policies;
            this.serviceScope = serviceProvider.CreateScope();
            this.serviceProvider = serviceScope.ServiceProvider;
        }

        /// <summary>
        /// Clones the configured instance.
        /// </summary>
        /// <returns></returns>
        IBaseService IBaseService.CloneConfiguredInstance()
        {
            return this.CloneConfiguredInstance();
        }

        /// <summary>
        /// Override to clone the configured (singleton) instance.
        /// The default behavior returns an instance cloned with <see cref="BaseService"/> properties (<see cref="PathEnvironment"/>, <see cref="ServiceAddress", <see cref="Policies"/>, and (scoped) <see cref="ServiceProvider"/>).
        /// </summary>
        /// <returns></returns>
        protected virtual IBaseService CloneConfiguredInstance()
        {
            BaseService clone = (BaseService)Activator.CreateInstance(this.GetType());

            clone.SetConfiguredEnvironment(this.PathEnvironment, this.ServiceAddress, this.Policies, this.ServiceProvider);

            return clone;
        }

        /// <summary>
        /// Sets the the service host environment for thie singleton instance.
        /// </summary>
        /// <param name="pathEnvironment"></param>
        /// <param name="serviceAddress"></param>
        /// <param name="serviceProvider"></param>
        void IBaseService.SetEnvironmentAndConfigure(IPathEnvironment pathEnvironment, Uri serviceAddress, IServiceProvider serviceProvider)
        {
            this.pathEnvironment = pathEnvironment;
            this.serviceAddress = serviceAddress;
            this.serviceProvider = serviceProvider;
            this.OnEnvironmentSet();
            this.LoadPolicies();
        }

        /// <summary>
        /// Sets the service context scope.  Invoked once for each service invocation.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="contextItems"></param>
        void IBaseService.OnMessageHandlerContextAvailable()
        {
            MessagingScope messageHandlerContext = MessagingInteractionContext.Current.Services.GetService<MessagingScope>();

            this.OnMessagingHandlerContext(messageHandlerContext);
        }

        private void LoadPolicies()
        {
            Policies.AddPolicyHandler<DiagnosticLevelAssertion>(ItemsChoiceType.Item, new HandlePolicyOperatorElements<DiagnosticLevelAssertion>(HandleDiagnosticLevelAssertionElements));
            Policies.AddPolicyHandler<LoggingLevelAssertion>(ItemsChoiceType.Item, new HandlePolicyOperatorElements<LoggingLevelAssertion>(HandleLoggingLevelAssertionElements));
            this.OnLoadingPolicies();
            if (Policies.LoadConfigState != PolicyCollection.LoadConfigStates.Loaded)
            {
                if (!Policies.TryLoadPolicyCacheFromConfig(out int policyCount, out Exception loadException))

                    throw loadException;

                this.Logger.Log(LoggerType.Info, Severity.TRACE, 1, "Policy cache loaded from config.");
            }
            this.OnPoliciesLoaded();
        }

        /// <summary>
        /// Invoked after the service environment is set.
        /// Override to implement any behaviors that consume <see cref="PathEnvironment"/>, <see cref="ServiceAddress"/>, or <see cref="ServiceProvider"/>.
        /// </summary>
        protected virtual void OnEnvironmentSet()
        {
        }

        /// <summary>
        /// Invoked before <see cref="Policies"/> are loaded from configuration.  
        /// Set any policy listeners/handlers here.
        /// </summary>
        protected virtual void OnLoadingPolicies()
        {
        }

        /// <summary>
        /// Invoked after <see cref="Policies"/> are loaded from configuration.  
        /// Override to implement any behaviors the consume loaded <see cref="Policies"/>.
        /// </summary>
        protected virtual void OnPoliciesLoaded()
        {
        }


        /// <summary>
        /// Invoked once per service invocation, when message handler context is available.
        /// Override to implement any behaviors that consume <see cref="ServiceProvider"/> and/or <see cref="ContextItems"/>.  
        /// </summary>
        protected virtual void OnMessagingHandlerContext(MessagingScope messagingHandlerContext)
        {
        }

        IPathEnvironment IMessagingDispatcher.GetPathEnvironment()
        {
            return this.PathEnvironment;
        }

        protected IPathEnvironment PathEnvironment
        {
            get
            {
                return this.pathEnvironment;
            }
        }

        Uri IMessagingDispatcher.GetListenerAddress()
        {
            return this.serviceAddress;
        }

        protected Uri ServiceAddress
        {
            get
            {
                return this.serviceAddress;
            }
        }

        IServiceProvider IMessagingDispatcher.GetServiceProvider()
        {
            return MessagingInteractionContext.Current.Services;
        }

        bool IMessagingDispatcher.HasDuplexCallback<T>()
        {
            return this.dispatchOperation != null &&
                this.dispatchOperation.GetIsDuplexResponse(out Type callbackResourceContractType) &&
                typeof(T).IsAssignableFrom(callbackResourceContractType);
        }

        bool IMessagingDispatcher.IsIntermediateHandler => this.IsIntermediateHandler;

        protected virtual bool IsIntermediateHandler => false;

        protected IServiceProvider ServiceProvider
        {
            get
            {
                return this.serviceProvider;
            }
        }

        /// <summary>
        /// Invoked upon receiving an incoming message.
        /// </summary>
        /// <param name="source">The instance of the runtime processing the service request.</param>
        /// <param name="args">The arguments to the event.</param>
        public void OnReceivedIncomingMessage(Object source, IncomingMessageEventArgs args)
        {
            if (args.TryGetMessageFilterMatchedDispatchOperation(out DispatchOperationDescription dispatchOperation))
            {
                this.dispatchOperation = dispatchOperation;
            }
            
            OnAfterReceivedIncomingMessage(source, args);
        }

        /// <summary>
        /// Override to implement any behavior after receiving the incoming message.
        /// </summary>
        /// <param name="source">The object raising the event.</param>
        /// <param name="args">The arguments to the event.</param>
        protected virtual void OnAfterReceivedIncomingMessage(Object source, IncomingMessageEventArgs args)
        {
        }

        /// <summary>
        /// Invoked prior to dispatching an outgoing message..
        /// </summary>
        /// <param name="source">The instance of the runtime processing the service request.</param>
        /// <param name="args">The arguments to the event.</param>
        public void OnDispatchOutgoingMessage(Object source, OutgoingMessageEventArgs args)
        {
            OnBeforeDispatchingOutgoingMessage(source, args);

            this.Logger.Log(args);

            bool success;

            if (args.OutgoingMessage == null)
                success = true;
            else
                success = !args.OutgoingMessageIsFault;

            if (args.Duration.HasValue && RecordKPIsTest(args.ReceivedAction))
            {
                RecordWorkKPIs(success, args.Duration.Value);
            }
        }

        /// <summary>
        /// Override to determine whether to record metrics for the current request.
        /// </summary>
        /// <param name="receivedAction">The action associated with the current request headers.</param>
        /// <returns>Boolean indicating whether to record metrics.</returns>
        /// <remarks>The default return is false.</remarks>
        protected virtual bool RecordKPIsTest(string receivedAction)
        {
            return false;
        }

        private void RecordWorkKPIs(bool success, TimeSpan elapsedTime)
        {
            string appliesToUri = this.GetDiagnosticsAppliesToUri();

            using (LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>> workDiagnostics = 
                new LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>>(
                    DIAGNOSTICS, 1000, 1000, () => new Dictionary<string, RecurringWorkDiagnostics>()))
            {
                if (workDiagnostics.CanRead)
                {
                    if (workDiagnostics.Object.ContainsKey(appliesToUri))
                    {
                        if (workDiagnostics.CanWrite)
                        {
                            if (success)

                                workDiagnostics.Object[appliesToUri].AccumulateSuccesses(elapsedTime);

                            else

                                workDiagnostics.Object[appliesToUri].AccumulateFailures();
                        }
                        else

                            this.Logger.Log(LoggerType.Info, Severity.WARN, "Work diagnostics could not be written.");
                    }
                }
                else

                    this.Logger.Log(LoggerType.Info, Severity.WARN, "Work diagnostics could not be read.");
            }
        }

        /// <summary>
        /// Override to implement any behavior before dispatching the outgoing message.
        /// </summary>
        /// <param name="source">The object raising the event.</param>
        /// <param name="args">The arguments to the event.</param>
        protected virtual void OnBeforeDispatchingOutgoingMessage(Object source, OutgoingMessageEventArgs args)
        {
        }

        /// <summary>
        /// Override to specify the diagnostics applies to Uri.
        /// </summary>
        /// <returns>The diagnostics applies to Uri.</returns>
        protected virtual string GetDiagnosticsAppliesToUri()
        {
            return MessagingInteractionContext.Current.Action.ToString();
        }

        /// <summary>
        /// Gets an instance of the <see cref="Logging.Logger"/> with which to perform logging.
        /// </summary>
        protected static Logger GetLogger<Service>() where Service : IBaseService
        {
            return Logger.GetInstance<Service>();
        }

        /// <summary>
        /// Gets an instance of the <see cref="Logging.Logger"/> with which to perform logging.
        /// </summary>
        protected virtual Logger Logger
        {
            get
            {
                if (this.logger == null)
                {
                    this.logger = Logger.GetInstance(GetLoggerType());

                    this.logger.Log(LoggerType.Info, Severity.TRACE, 1, "Logger initialized.");
                }
                return this.logger;
            }
        }

        protected virtual Type GetLoggerType()
        {
            return this.GetType();
        }

        protected PolicyCollection Policies
        {
            get
            {
                if (policies == null)

                    policies = new PolicyCollection();

                return policies;
            }
        }

        private static void HandleDiagnosticLevelAssertionElements(Policy target, Policy policy, DiagnosticLevelAssertion[] elements)
        {
            foreach (DiagnosticLevelAssertion diagnosticLevelAssertion in elements)
            {
                if (!TrySetDiagnosticLevel(diagnosticLevelAssertion.AppliesTo, diagnosticLevelAssertion.DiagnosticLevel))

                    FaultCodes.ServerErrorCode.ServiceTimeout.WrapFaultCode().CreateFaultException(
                        FaultCodes.CreateFaultReason("Diagnostic level could not be set for action {0}.", diagnosticLevelAssertion.AppliesTo),
                        MessagingInteractionContextAccessor.Current?.InteractionContext?.Dispatcher?.GetType());
            }
        }

        /// <summary>
        /// Sets the diagnostic level for the service.
        /// </summary>
        /// <param name="appliesToUri">A unique identifier for the service.</param>
        /// <param name="diagnosticLevel">The <see cref="DiagnosticLevels"/> enumeration value to set.</param>
        /// <returns>true if attempt to confirm or set diagnostic level to <paramref name="diagnosticLevel"/> could be done safely, otherwise false.</returns>
        /// <remarks>
        /// Setting the diagnostic level is a concurrent operation and can timeout waiting for a read or write lock.  
        /// In the case of a timeout the operation will fail, in which case the diagnostic level for the service will be unchanged.
        /// </remarks>
        protected static bool TrySetDiagnosticLevel(string appliesToUri, DiagnosticLevels diagnosticLevel)
        {
            using (LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>> workDiagnostics = new LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>>(DIAGNOSTICS, 1000, 1000, () => new Dictionary<string, RecurringWorkDiagnostics>()))
            {
                if (workDiagnostics.CanRead)
                {
                    RecurringWorkDiagnostics currentWorkDiagnostics;

                    if (workDiagnostics.Object.ContainsKey(appliesToUri))
                    {
                        currentWorkDiagnostics = workDiagnostics.Object[appliesToUri];

                        switch (diagnosticLevel)
                        {
                            case DiagnosticLevels.ALL:

                                if (currentWorkDiagnostics.Enabled)

                                    return true;

                                else
                                {
                                    currentWorkDiagnostics.Enabled = true;

                                    return true;
                                }

                            case DiagnosticLevels.NONE:

                                if (currentWorkDiagnostics.Enabled)
                                {
                                    currentWorkDiagnostics.Enabled = false;

                                    return true;
                                }
                                else

                                    return true;

                            default:

                                return false;
                        }
                    }
                    else

                        currentWorkDiagnostics = null;

                    if (currentWorkDiagnostics == null)
                    {
                        if (workDiagnostics.CanWrite)
                        {
                            currentWorkDiagnostics = new RecurringWorkDiagnostics(SERVICE_ACTION_DIAGNOSTICS_CATEGORY, appliesToUri);

                            workDiagnostics.Object.Add(appliesToUri, currentWorkDiagnostics);

                            switch (diagnosticLevel)
                            {
                                case DiagnosticLevels.ALL:

                                    currentWorkDiagnostics.Enabled = true;

                                    return true;

                                case DiagnosticLevels.NONE:

                                    currentWorkDiagnostics.Enabled = false;

                                    return true;

                                default:

                                    return false;
                            }
                        }
                    }
                }

                return false;
            }
        }

        private static void HandleLoggingLevelAssertionElements(Policy target, Policy policy, LoggingLevelAssertion[] elements)
        {
            foreach (LoggingLevelAssertion loggingLevelAssertion in elements)
            {
                if (!TrySetLogLevel(loggingLevelAssertion.AppliesTo, loggingLevelAssertion.LoggingLevel.ToLogLevel()))

                    throw 
                        FaultCodes.ServerErrorCode.ServiceTimeout.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("Logging level could not be set for action {0}.", loggingLevelAssertion.AppliesTo),
                            MessagingInteractionContextAccessor.Current.InteractionContext.Dispatcher?.GetType());
            }
        }

        /// <summary>
        /// Sets the logging level for the service.
        /// </summary>
        /// <param name="loggerName">The name of the logger instance.</param>
        /// <param name="logLevel">The <see cref="Threshold"/> enumeration level to set.</param>
        /// <returns>Returns true if attempt to confirm or set logging level to <paramref name="logLevel"/> succeeds, otherwise false.</returns>
        /// <remarks>
        /// Setting the logging level may be a concurrent operation and could timeout waiting for a read or write lock.  
        /// In the case of a timeout the operation will fail, in which case the logging level for the service will be unchanged.
        /// </remarks>
        protected static bool TrySetLogLevel(string loggerName, Threshold threshold)
        {
            Logger.SetLogger(loggerName, threshold);

            return true;
        }

        /// <summary>
        /// Gets the policies of the service.
        /// </summary>
        /// <returns></returns>
        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual Policy[] GetPolicies()
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, "Getting policies.");

                return Policies.ToArray();
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw e.CreateFaultException(this.GetType(), this.IsIntermediateHandler);
            }
        }

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri"></param>
        /// <param name="itemTypeName"></param>
        /// <returns></returns>
        public String[] GetPolicyNamesContainingItem(string itemTypeNamespaceUri, string itemTypeName)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"GetPolicyNamesContainingItem {itemTypeNamespaceUri}/{itemTypeName}.");

                List<String> policyNames = new List<string>();

                itemTypeNamespaceUri = itemTypeNamespaceUri.Base64UrlDecode();

                foreach (Policy policy in Policies)
                {
                    if (policy.ContainsItemOfType(itemTypeNamespaceUri, itemTypeName))

                        policyNames.Add(policy.Name);
                }

                return policyNames.ToArray();
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Gets the names of the service policies that contain items of <paramref name="itemTypeName" /> in <paramref name="itemNamespaceUri"/> for <paramref name="itemKeyUri"/>.
        /// </summary>
        /// <param name="itemTypeNamespaceUri">The type namespace Uri for the policy item.</param>
        /// <param name="itemTypeName">The type name for the policy item.</param>
        /// <param name="itemKeyTarget">The target property or attribute of the item.</param>
        /// <param name="itemKeyUri">A key that identifies a property or attribute of the item.</param>
        /// <returns>The result of executing the operation.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="FaultDetails" />.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        public String[] GetPolicyNamesContainingItemKey(string itemTypeNamespaceUri, string itemTypeName, string itemKeyTarget, string itemKeyUri)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"GetPolicyNamesContainingItemKey {itemTypeNamespaceUri}/{itemTypeName}/{itemKeyTarget}/{itemKeyUri}.");

                List<String> policyNames = new List<string>();

                itemTypeNamespaceUri = itemTypeNamespaceUri.Base64UrlDecode();
                itemKeyUri = itemKeyUri.Base64UrlDecode();

                string[] itemKeyTargetNodeNames = itemKeyTarget.Split('-');

                foreach (Policy policy in Policies)
                {
                    if (policy.TryGetItemOfType(itemTypeNamespaceUri, itemTypeName, out Object item) &&
                        this.IsPolicyItemTarget(item, new XmlQualifiedName(itemTypeName, itemTypeNamespaceUri), itemKeyTargetNodeNames, itemKeyUri))
                    {
                        policyNames.Add(policy.Name);
                    }
                }

                return policyNames.ToArray();
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Override this method to test whether a Policy Item is a tree of root and branch (identified by "<paramref name="itemKeyTargetRootName"/>" and "<paramref name="itemKeyTargetBranchNames"/>" with leaf value given by "<paramref name="itemKeyTargetLeafValue"/>". (See <see cref="GetPolicyNamesContainingItemKey"/>)
        /// </summary>
        /// <param name="item"></param>
        /// <param name="itemKeyTargetRootName"></param>
        /// <param name="itemKeyTargetBranchNames"></param>
        /// <param name="itemKeyTargetLeafValue"></param>
        /// <returns>true if the item is determined to be a target;  the default is false;</returns>
        /// <seealso cref="GetPolicyNamesContainingItemKey"/>
        protected virtual bool IsPolicyItemTarget(object item, XmlQualifiedName itemTypeName, IEnumerable<string> itemKeyTargetBranchNames, string itemKeyTargetLeafValue)
        {
            return false;
        }

        /// <summary>
        /// Gets the named policy of the service.
        /// </summary>
        /// <param name="name">The Uniform Resource Name for the policy.</param>
        /// <returns></returns>
        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual Policy GetPolicy(String name)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"GetPolicy {name}.");

                if (Policies.Contains(name))

                    return Policies[name];

                return null;
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Sets a policy of the service.
        /// </summary>
        /// <param name="policy"></param>
        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual void SetPolicy(Policy policy)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"Setting policy {policy.Name}.");

                if (String.IsNullOrWhiteSpace(policy.Name))

                    policy.Name = Guid.NewGuid().ToString("D");

                if (Policies.Contains(policy.Name))

                    Policies[Policies.IndexOf(Policies.First(p => p.Name == policy.Name))] = policy;

                else

                    Policies.Add(policy);
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual Policy[] GetPolicies(string action)
        {
            throw new NotImplementedException();
        }

        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual Policy GetPolicy(string action, string policyName)
        {
            throw new NotImplementedException();
        }

        [XmlAttributeOverridesSerializerFormat(XmlAttributeOverridesType = typeof(PolicyReferralsXmlAttributeOverrides))]
        public virtual void SetPolicy(string action, Policy policy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves current values of all diagnostic indicators for the service.
        /// </summary>
        /// <returns></returns>
        public IIndicator[] GetIndicators()
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, "GetIndicators.");

                IIndicator[] indicators;

                if (this.TryReadDiagnosticIndicators(this.GetDiagnosticsAppliesToUri(), out indicators))

                    return indicators;

                else

                    throw
                        FaultCodes.ServerErrorCode.ServiceTimeout.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("Indicators could not be read for action {0}.", this.GetDiagnosticsAppliesToUri()),
                            this.GetType());
            }
            catch (FaultException e)
            {
                Logger.Log(e);

                throw e;
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Retrieves the current value of the diagnostic indicator named <paramref name="indicatorName"/> for the service.
        /// </summary>
        /// <param name="indicatorName"></param>
        /// <returns></returns>
        public IIndicator GetIndicator(string indicatorName)
        {
            Logger.Log(LoggerType.Info, Severity.TRACE, $"GetIndicators {indicatorName}.");

            return GetIndicators().FirstOrDefault(i => i.Name == indicatorName);
        }

        /// <summary>
        ///  Replies with the date and time the ping was received.
        /// </summary>
        public DateTime Ping()
        {
            Logger.Log(LoggerType.Info, Severity.TRACE, "Ping.");

            return DateTime.Now;
        }

        /// <summary>
        /// The trace of the service action.
        /// </summary>
        /// <param name="action">The service action receiving the trace request.</param>
        /// <returns>The echoed trace request of the service action.</returns>
        /// <exception cref="System.ServiceModel.FaultException{TDetail}">TDetail is <see cref="ServiceDetailedException"/>.  See also <see cref="FaultDetail"/> and <see cref="ServiceFaultCodes"/>.</exception>
        public String Trace(string action)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"Trace {action}.");

                if (IsServiceAction(action) != true)

                    throw
                        FaultCodes.ClientErrorCode.NotFound.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("Action {0} not found.", action),
                            this.GetType());

                MessagingInteractionContext messageContext = MessagingInteractionContext.Current;

                if (messageContext == null)

                    throw
                        FaultCodes.ServerErrorCode.ServiceFaulted.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("No message context found for action {0}.", action),
                            this.GetType());

                if (messageContext.IncomingMessageProperties.TryGetProperty(HttpRequestMessageProperty.Name, out HttpRequestMessageProperty httpRequestMessageProperty))
                {
                    int httpMaxForwards;

                    if (!int.TryParse(httpRequestMessageProperty.Headers[HttpRequestHeader.MaxForwards], out httpMaxForwards))

                        httpMaxForwards = 0;

                    IPHostEntry ipHostEntry;

                    if (httpMaxForwards > 0)

                        ipHostEntry = Dns.GetHostEntry(IPUtility.GetPublicEgressIPEndPoint().Address);

                    else

                        ipHostEntry = Dns.GetHostEntry(httpRequestMessageProperty.Headers[HttpRequestHeader.Host]);

                    String via = httpRequestMessageProperty.Headers[HttpRequestHeader.Via];

                    if (via == null)

                        via = "";

                    via += string.Format("HTTP/1.1 {0} ({1}/{2}.{3})", ipHostEntry.HostName, Environment.OSVersion.Platform, Environment.OSVersion.Version.Major, Environment.OSVersion.Version.Minor);

                    String trace;

                    if (httpMaxForwards > 0)

                        trace = this.ForwardTrace(action, httpMaxForwards - 1, ref via);

                    else

                        trace = this.ReplyTrace(via, httpRequestMessageProperty.Headers);

                    HttpResponseMessageProperty responseMessageProperty = messageContext.OutgoingMessageProperties.GetPropertyOrDefault<HttpResponseMessageProperty>(HttpResponseMessageProperty.Name);

                    responseMessageProperty.Headers.Add(HttpResponseHeader.Via, via);

                    return trace;
                }

                throw
                    FaultCodes.ServerErrorCode.ServiceFaulted.WrapFaultCode(messageContext.IncomingVersion.Envelope).CreateFaultException(
                        FaultCodes.CreateFaultReason("No Http header properties found for action {0}.", action),
                        this.GetType());
            }
            catch (FaultException e)
            {
                Logger.Log(e);

                throw e;
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actions"></param>
        public void SetActions(params String[] actions)
        {
            try
            {
                using (LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>> workDiagnostics = new LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>>(DIAGNOSTICS, 1000, () => new Dictionary<string, RecurringWorkDiagnostics>()))
                {
                    if (workDiagnostics.CanWrite)
                    {
                        foreach (String action in actions)
                        {
                            if (!workDiagnostics.Object.ContainsKey(action))

                                workDiagnostics.Object.Add(action, new RecurringWorkDiagnostics(SERVICE_ACTION_DIAGNOSTICS_CATEGORY, action));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Determines whether the requested <paramref name="action"/> is a valid action for the service.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        protected virtual bool? IsServiceAction(string action)
        {
            using (LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>> workDiagnostics = new LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>>(DIAGNOSTICS, 1000, () => new Dictionary<string, RecurringWorkDiagnostics>()))
            {
                if (workDiagnostics.CanRead)
                {
                    return workDiagnostics.Object.ContainsKey(action);
                }

                return null;
            }
        }

        /// <summary>
        /// Override to implement trace forwarding for the <paramref name="action"/>, 
        /// sending a Max-Forwards http header value of <paramref name="maxForwards"/>, and a Via http header value of <paramref name="via"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="maxForwards"></param>
        /// <param name="via"></param>
        /// <returns></returns>
        protected virtual string ForwardTrace(string action, int maxForwards, ref string via)
        {
            throw new NotImplementedException();
        }

        private string ReplyTrace(string via, WebHeaderCollection requestMessageHeaders)
        {
            StringBuilder sb = new StringBuilder();

            WebHeaderCollection responseMessageHeaders = new WebHeaderCollection();

            // ToDo: Copy request headers to response headers less sensitive headers and Via and Content-Type.

            responseMessageHeaders.Add(HttpResponseHeader.Via, via);
            responseMessageHeaders.Add(HttpResponseHeader.ContentType, "message/http");

            sb.AppendLine("HTTP/1.1 200 OK");
            sb.AppendLine();
            sb.Append(requestMessageHeaders.ToString());

            return sb.ToString();
        }

        /// <summary>
        /// Retrieves current values of all diagnostic indicators for the service action.
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public IIndicator[] GetIndicators(string action)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"GetIndicators {action}.");

                if (this.TryReadDiagnosticIndicators(action, out IIndicator[] indicators))

                    return indicators;

                else

                    throw
                        FaultCodes.ServerErrorCode.ServiceTimeout.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("Indicators could not be read for action {0}.", action),
                            this.GetType());
            }
            catch (FaultException e)
            {
                Logger.Log(e);

                throw e;
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        /// <summary>
        /// Retrieves the current value of the diagnostic indicator named <paramref name="indicatorName"/> for the service <paramref name="action"/>.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="indicatorName"></param>
        /// <returns></returns>
        public IIndicator GetIndicator(string action, string indicatorName)
        {
            try
            {
                Logger.Log(LoggerType.Info, Severity.TRACE, $"GetIndicator {action}/{indicatorName}.");

                if (this.TryReadDiagnosticIndicators(action, out IIndicator[] indicators))

                    return GetIndicators(action).FirstOrDefault(i => i.Name == indicatorName);

                else

                    throw
                        FaultCodes.ServerErrorCode.ServiceTimeout.WrapFaultCode().CreateFaultException(
                            FaultCodes.CreateFaultReason("Indicators could not be read for action {0}.", action),
                            this.GetType());
            }
            catch (FaultException e)
            {
                Logger.Log(e);

                throw e;
            }
            catch (Exception e)
            {
                Logger.Log(e);

                throw this.CreateFaultException(e);
            }
        }

        protected FaultException CreateFaultException(Exception e)
        {
            return e.CreateFaultException(this.GetType(), this.IsIntermediateHandler);
        }

        /// <summary>
        /// Attempts a thread-safe read of diagnostic indicators for <paramref name="instanceName"/>.
        /// </summary>
        /// <param name="instanceName">The name of the instance for which to read diagnostics.</param>
        /// <param name="indicators">After execution, will be null if diagnostics cannot be read safely, or diagnostics is not enabled for the <paramref name="instanceName"/>.  Otherwise all available <see cref="IIndicator"/> are populated in the array.</param>
        /// <returns>returns true if diagnostics can be read safely, and diagnostics is enabled for the <paramref name="instanceName"/>, otherwise false.</returns>
        private bool TryReadDiagnosticIndicators(string instanceName, out IIndicator[] indicators)
        {
            indicators = null;

            using (LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>> workDiagnostics = new LockSlimCriticalSection<Dictionary<string, RecurringWorkDiagnostics>>(DIAGNOSTICS, 1000, () => new Dictionary<string, RecurringWorkDiagnostics>()))
            {
                if (workDiagnostics.CanRead)
                {
                    if (workDiagnostics.Object.ContainsKey(instanceName))

                        indicators = workDiagnostics.Object[instanceName].TryReadIndicators();
                }
            }

            return indicators != null;
        }

        //internal void ConfigureSingletonDispatcher(object singletonDispatcher, ResourceDispatcherSelector resourceDispatcherSelector)
        //{
        //    if (singletonDispatcher is IDiagnosticServiceAction)

        //        (singletonDispatcher as IDiagnosticServiceAction).SetActions(resourceDispatcherSelector.EndpointDispatcher.EndpointAddress.Uri.OriginalString);

        //    if (singletonDispatcher is IBaseService)
        //    {
        //        IBaseService baseService = singletonDispatcher as IBaseService;

        //        baseService.SetEnvironmentAndConfigure(resourceDispatcherSelector.HostingEnvironment, resourceDispatcherSelector.EndpointDispatcher.EndpointAddress.Uri, serviceProvider);
        //    }
        //}

        //internal object GetDispatcherInstance(object singletonDispatcher)
        //{
        //    Object dispatcherInstance = null;

        //    if (singletonDispatcher is IBaseService)

        //        dispatcherInstance = (singletonDispatcher as IBaseService).CloneConfiguredInstance();

        //    else

        //        dispatcherInstance = Activator.CreateInstance(singletonDispatcher.GetType());

        //    if (dispatcherInstance is IBaseService)
        //    {
        //        (dispatcherInstance as IBaseService).MessageHandlerContextAvailable();
        //    }

        //    return dispatcherInstance;
        //}

        void IMessagingDispatcher.ConfigureSingletonDispatcher(object singletonInstance, IPathEnvironment pathEnvironment, IServiceProvider serviceProvider, Uri listenUri)
        {
            if (singletonInstance is IDiagnosticServiceAction)

                (singletonInstance as IDiagnosticServiceAction).SetActions(listenUri.OriginalString);

            if (singletonInstance is IBaseService)
            {
                IBaseService baseService = singletonInstance as IBaseService;

                baseService.SetEnvironmentAndConfigure(pathEnvironment, listenUri, serviceProvider);
            }
        }

        object IMessagingDispatcher.CloneDispatcherInstanceFromSingleton(object singletonInstance)
        {
            Object perCallInstance = null;

            if (singletonInstance is IBaseService)

                perCallInstance = (singletonInstance as IBaseService).CloneConfiguredInstance();

            else

                perCallInstance = Activator.CreateInstance(singletonInstance.GetType());

            if (perCallInstance is IBaseService)
            {
                (perCallInstance as IBaseService).OnMessageHandlerContextAvailable();
            }

            return perCallInstance;
        }

            #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls
        private DispatchOperationDescription dispatchOperation;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    this.dispatchOperation = null;
                    if (this.serviceScope != null)
                        this.serviceScope.Dispose();
                    OnDispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        /// <summary>
        /// Override to implement perform any disposal of the instance's managed resources.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~BaseService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        #endregion
    }
}
