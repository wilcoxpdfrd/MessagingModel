using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;
using System.Xml;

namespace AllVerge.MessagingModel.ZeroMQChannelMessaging.Channels
{
    using System.ServiceModel;
    using System.ServiceModel.Channels;
    using System.ServiceModel.Diagnostics;
    using System.ServiceModel.Diagnostics.Application;
    using System.ServiceModel.Dispatcher;
    using AllVerge.Core.Resource;
    using AllVerge.Core.ServiceModel.Channels;
    using AllVerge.Core.ServiceModel.Transfer;

    abstract class ZeroMQTransferTransportChannelListenerBase : TransportChannelListener,
        ITransferTransportFactorySettings
    {
        //AuthenticationSchemes authenticationScheme;
        //bool extractGroupsForWindowsAccounts;
        //EndpointIdentity identity;
        bool keepAliveEnabled;
        int maxBufferSize;
        readonly int maxPendingAccepts;
        string method;
        string realm;
        readonly TimeSpan requestInitializationTimeout;
        TransferMode transferMode;
        //bool unsafeConnectionNtlmAuthentication;
        //ISecurityCapabilities securityCapabilities;

        //SecurityCredentialsManager credentialProvider;
        //SecurityTokenAuthenticator userNameTokenAuthenticator;
        //SecurityTokenAuthenticator windowsTokenAuthenticator;
        //ExtendedProtectionPolicy extendedProtectionPolicy;
        //bool usingDefaultSpnList;
        HttpAnonymousUriPrefixMatcher anonymousUriPrefixMatcher;

        //HttpMessageSettings httpMessageSettings;
        //WebSocketTransportSettings webSocketSettings;

        static UriPrefixTable<ITransportManagerRegistration> transportManagerTable =
            new UriPrefixTable<ITransportManagerRegistration>(true);

        public ZeroMQTransferTransportChannelListenerBase(ZeroMQTransferTransportBindingElementBase bindingElement, BindingContext context) :
            base(bindingElement, context, ZeroMQTransferTransportDefaults.GetDefaultMessageEncoderFactory(), bindingElement.HostNameComparisonMode)
        {
            if (bindingElement.TransferMode == TransferMode.Buffered)
            {
                if (bindingElement.MaxReceivedMessageSize > int.MaxValue)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
                        new ArgumentOutOfRangeException("bindingElement.MaxReceivedMessageSize",
                        SSR.MaxReceivedMessageSizeMustBeInIntegerRange));
                }

                if (bindingElement.MaxBufferSize != bindingElement.MaxReceivedMessageSize)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement",
                        SSR.MaxBufferSizeMustMatchMaxReceivedMessageSize);
                }
            }
            else
            {
                if (bindingElement.MaxBufferSize > bindingElement.MaxReceivedMessageSize)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgument("bindingElement",
                        SSR.MaxBufferSizeMustNotExceedMaxReceivedMessageSize);
                }
            }

            //if (bindingElement.AuthenticationScheme.IsSet(AuthenticationSchemes.Basic) &&
            //    bindingElement.AuthenticationScheme.IsNotSet(AuthenticationSchemes.Digest | AuthenticationSchemes.Ntlm | AuthenticationSchemes.Negotiate) &&
            //    bindingElement.ExtendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Always)
            //{
            //    //Basic auth + PolicyEnforcement.Always doesn't make sense because basic auth can't support CBT.
            //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SSR.ExtendedProtectionPolicyBasicAuthNotSupported));
            //}

            //this.authenticationScheme = bindingElement.AuthenticationScheme;
            this.keepAliveEnabled = bindingElement.KeepAliveEnabled;
            //this.InheritBaseAddressSettings = bindingElement.InheritBaseAddressSettings;
            this.maxBufferSize = bindingElement.MaxBufferSize;
            this.maxPendingAccepts = ZeroMQTransferTransportDefaults.GetEffectiveMaxPendingAccepts(bindingElement.MaxPendingAccepts);
            this.method = bindingElement.Method;
            this.realm = bindingElement.Realm;
            this.requestInitializationTimeout = bindingElement.RequestInitializationTimeout;
            this.transferMode = bindingElement.TransferMode;
            //this.unsafeConnectionNtlmAuthentication = bindingElement.UnsafeConnectionNtlmAuthentication;
            //this.credentialProvider = context.BindingParameters.Find<SecurityCredentialsManager>();
            //this.securityCapabilities = bindingElement.GetProperty<ISecurityCapabilities>(context);
            //this.extendedProtectionPolicy = GetPolicyWithDefaultSpnCollection(bindingElement.ExtendedProtectionPolicy, this.authenticationScheme, this.HostNameComparisonModeInternal, base.Uri, out this.usingDefaultSpnList);

            //this.webSocketSettings = WebSocketHelper.GetRuntimeWebSocketSettings(bindingElement.WebSocketSettings);

            if (bindingElement.AnonymousUriPrefixMatcher != null)
            {
                this.anonymousUriPrefixMatcher = new HttpAnonymousUriPrefixMatcher(bindingElement.AnonymousUriPrefixMatcher);
            }

            //    this.httpMessageSettings = context.BindingParameters.Find<HttpMessageSettings>() ?? new HttpMessageSettings();

            //    if (this.httpMessageSettings.HttpMessagesSupported && this.MessageVersion != MessageVersion.None)
            //    {
            //        throw FxTrace.Exception.AsError(
            //            new NotSupportedException(SSR.Format(
            //                    SSR.MessageVersionNoneRequiredForHttpMessageSupport,
            //                    typeof(HttpRequestMessage).Name,
            //                    typeof(HttpResponseMessage).Name,
            //                    typeof(HttpMessageSettings).Name,
            //                    typeof(MessageVersion).Name,
            //                    typeof(MessageEncodingBindingElement).Name,
            //                    this.MessageVersion.ToString(),
            //                    MessageVersion.None.ToString())));
            //    }
        }

        //public WebSocketTransportSettings WebSocketSettings
        //{
        //    get { return this.webSocketSettings; }
        //}

        //public HttpMessageSettings HttpMessageSettings
        //{
        //    get { return this.httpMessageSettings; }
        //}

        //public ExtendedProtectionPolicy ExtendedProtectionPolicy
        //{
        //    get
        //    {
        //        return this.extendedProtectionPolicy;
        //    }
        //}

        public virtual bool IsChannelBindingSupportEnabled
        {
            get
            {
                return false;
            }
        }

        //public abstract bool UseWebSocketTransport { get; }

        internal HttpAnonymousUriPrefixMatcher AnonymousUriPrefixMatcher
        {
            get
            {
                return this.anonymousUriPrefixMatcher;
            }
        }

        //protected SecurityTokenAuthenticator UserNameTokenAuthenticator
        //{
        //    get { return this.userNameTokenAuthenticator; }
        //}

        //internal override void ApplyHostedContext(string virtualPath, bool isMetadataListener)
        //{
        //    base.ApplyHostedContext(virtualPath, isMetadataListener);
        //    AspNetEnvironment.Current.ValidateHttpSettings(virtualPath, isMetadataListener, this.usingDefaultSpnList, ref this.authenticationScheme, ref this.extendedProtectionPolicy, ref this.realm);
        //}

        //public AuthenticationSchemes AuthenticationScheme
        //{
        //    get
        //    {
        //        return this.authenticationScheme;
        //    }
        //}

        public bool KeepAliveEnabled
        {
            get
            {
                return this.keepAliveEnabled;
            }
        }

        //public bool ExtractGroupsForWindowsAccounts
        //{
        //    get
        //    {
        //        return this.extractGroupsForWindowsAccounts;
        //    }
        //}

        public HostNameComparisonMode HostNameComparisonMode
        {
            get
            {
                return this.HostNameComparisonModeInternal;
            }
        }

        //Returns true if one of the non-anonymous authentication schemes is set on this.AuthenticationScheme
        //protected bool IsAuthenticationSupported
        //{
        //    get
        //    {
        //        return this.authenticationScheme != AuthenticationSchemes.Anonymous;
        //    }
        //}

        //bool IsAuthenticationRequired
        //{
        //    get
        //    {
        //        return this.AuthenticationScheme.IsNotSet(AuthenticationSchemes.Anonymous);
        //    }
        //}

        public int MaxBufferSize
        {
            get
            {
                return this.maxBufferSize;
            }
        }

        public int MaxPendingAccepts
        {
            get { return this.maxPendingAccepts; }
        }

        public virtual string Method
        {
            get
            {
                return this.method;
            }
        }

        public TimeSpan RequestInitializationTimeout
        {
            get { return this.requestInitializationTimeout; }
        }

        public TransferMode TransferMode
        {
            get
            {
                return transferMode;
            }
        }

        public string Realm
        {
            get { return this.realm; }
        }

        int ITransferTransportFactorySettings.MaxBufferSize
        {
            get { return MaxBufferSize; }
        }

        TransferMode ITransferTransportFactorySettings.TransferMode
        {
            get { return TransferMode; }
        }

        //internal static UriPrefixTable<ITransportManagerRegistration> StaticTransportManagerTable
        //{
        //    get
        //    {
        //        return transportManagerTable;
        //    }
        //}

        //public bool UnsafeConnectionNtlmAuthentication
        //{
        //    get
        //    {
        //        return this.unsafeConnectionNtlmAuthentication;
        //    }
        //}

        internal static UriPrefixTable<ITransportManagerRegistration> StaticTransportManagerTable
        {
            get
            {
                return transportManagerTable;
            }
        }

        internal override UriPrefixTable<ITransportManagerRegistration> TransportManagerTable
        {
            get
            {
                return transportManagerTable;
            }
        }

        internal override ITransportManagerRegistration CreateTransportManagerRegistration(Uri listenUri)
        {
            return new ZeroMQSharedTransferTransportManager(listenUri, this);
        }

        //string GetAuthType(HttpListenerContext listenerContext)
        //{
        //    string authType = null;
        //    IPrincipal principal = listenerContext.User;
        //    if ((principal != null) && (principal.Identity != null))
        //    {
        //        authType = principal.Identity.AuthenticationType;
        //    }
        //    return authType;
        //}

        //protected string GetAuthType(IHttpAuthenticationContext authenticationContext)
        //{
        //    string authType = null;
        //    if (authenticationContext.LogonUserIdentity != null)
        //    {
        //        authType = authenticationContext.LogonUserIdentity.AuthenticationType;
        //    }
        //    return authType;
        //}

        //bool IsAuthSchemeValid(string authType)
        //{
        //    return AuthenticationSchemesHelper.DoesAuthTypeMatch(this.authenticationScheme, authType);
        //}

        internal override int GetMaxBufferSize()
        {
            return MaxBufferSize;
        }

        //public override T GetProperty<T>()
        //{
        //    if (typeof(T) == typeof(EndpointIdentity))
        //    {
        //        return (T)(object)(this.identity);
        //    }
        //    else if (typeof(T) == typeof(ILogonTokenCacheManager))
        //    {
        //        object cacheManager = (object)GetIdentityModelProperty<T>();
        //        if (cacheManager != null)
        //        {
        //            return (T)cacheManager;
        //        }
        //    }
        //    else if (typeof(T) == typeof(ISecurityCapabilities))
        //    {
        //        return (T)(object)this.securityCapabilities;
        //    }
        //    else if (typeof(T) == typeof(ExtendedProtectionPolicy))
        //    {
        //        return (T)(object)this.extendedProtectionPolicy;
        //    }

        //    return base.GetProperty<T>();
        //}

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //T GetIdentityModelProperty<T>()
        //{
        //    if (typeof(T) == typeof(EndpointIdentity))
        //    {
        //        if (this.identity == null)
        //        {
        //            if (this.authenticationScheme.IsSet(AuthenticationSchemes.Negotiate) ||
        //                this.authenticationScheme.IsSet(AuthenticationSchemes.Ntlm))
        //            {
        //                this.identity = SecurityUtils.CreateWindowsIdentity();
        //            }
        //        }

        //        return (T)(object)this.identity;
        //    }
        //    else if (typeof(T) == typeof(ILogonTokenCacheManager)
        //        && (this.userNameTokenAuthenticator != null))
        //    {
        //        ILogonTokenCacheManager retVal = this.userNameTokenAuthenticator as ILogonTokenCacheManager;

        //        if (retVal != null)
        //        {
        //            return (T)(object)retVal;
        //        }
        //    }

        //    return default(T);
        //}

        internal abstract IAsyncResult BeginZeroMQRequestContextReceived(
            ZeroMQRequestContext context,
            Action acceptorCallback,
            AsyncCallback callback,
            object state);

        internal abstract bool EndZeroMQRequestContextReceived(IAsyncResult result);

        //[MethodImpl(MethodImplOptions.NoInlining)]
        //void InitializeSecurityTokenAuthenticator()
        //{
        //    Fx.Assert(this.IsAuthenticationSupported, "SecurityTokenAuthenticator should only be initialized when authentication is supported.");
        //    ServiceCredentials serviceCredentials = this.credentialProvider as ServiceCredentials;

        //    if (serviceCredentials != null)
        //    {
        //        if (this.AuthenticationScheme == AuthenticationSchemes.Basic)
        //        {
        //            // when Basic authentiction is enabled - but Digest and Windows are disabled use the UsernameAuthenticationSetting
        //            this.extractGroupsForWindowsAccounts = serviceCredentials.UserNameAuthentication.IncludeWindowsGroups;
        //        }
        //        else
        //        {
        //            //if (this.AuthenticationScheme.IsSet(AuthenticationSchemes.Basic) &&
        //            //    serviceCredentials.UserNameAuthentication.IncludeWindowsGroups != serviceCredentials.WindowsAuthentication.IncludeWindowsGroups)
        //            //{
        //            //    // Ensure there are no inconsistencies when Basic and (Digest and/or Ntlm and/or Negotiate) are both enabled
        //            //    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new NotSupportedException(SSR.Format(SSR.SecurityTokenProviderIncludeWindowsGroupsInconsistent,
        //            //            (AuthenticationSchemes)authenticationScheme - AuthenticationSchemes.Basic,
        //            //            serviceCredentials.UserNameAuthentication.IncludeWindowsGroups,
        //            //            serviceCredentials.WindowsAuthentication.IncludeWindowsGroups)));
        //            //}

        //            if (this.AuthenticationScheme.IsSet(AuthenticationSchemes.Basic) &&
        //                serviceCredentials.UserNameAuthentication.IncludeWindowsGroups != false)
        //            {
        //                // Ensure there are no inconsistencies when Basic and (Digest and/or Ntlm and/or Negotiate) are both enabled
        //                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
        //                    new NotSupportedException(
        //                        SSR.Format(SSR.SecurityTokenProviderIncludeWindowsGroupsInconsistent,
        //                        (AuthenticationSchemes)authenticationScheme - AuthenticationSchemes.Basic,
        //                        serviceCredentials.UserNameAuthentication.IncludeWindowsGroups,
        //                        false)));
        //            }

        //            //this.extractGroupsForWindowsAccounts = serviceCredentials.WindowsAuthentication.IncludeWindowsGroups;
        //            this.extractGroupsForWindowsAccounts = false;
        //        }

        //        // THEN, NOT NOW - we will only support custom and windows validation modes, if anything else is specified, we'll fall back to windows user name.
        //        // NOW - we will only support custom and possibly membership claims
        //        if (serviceCredentials.UserNameAuthentication.UserNamePasswordValidationMode == UserNamePasswordValidationMode.Custom)
        //        {
        //            this.userNameTokenAuthenticator = new CustomUserNameSecurityTokenAuthenticator(serviceCredentials.UserNameAuthentication.GetUserNamePasswordValidator());
        //        }
        //        else if (serviceCredentials.UserNameAuthentication.UserNamePasswordValidationMode == UserNamePasswordValidationMode.MembershipProvider)
        //        {
        //            throw NotImplemented.ByDesignWithMessage($"{this.GetType()}::{nameof(InitializeSecurityTokenAuthenticator)} is not yet implemented for {nameof(UserNamePasswordValidationMode.MembershipProvider)}");
        //        }
        //        else
        //        {
        //            throw NotImplemented.ByDesignWithMessage($"{this.GetType()}::{nameof(InitializeSecurityTokenAuthenticator)} is not implemented for {nameof(UserNamePasswordValidationMode.Windows)}");
        //        }
        //        //else
        //        //{
        //        //    if (serviceCredentials.UserNameAuthentication.CacheLogonTokens)
        //        //    {
        //        //        this.userNameTokenAuthenticator = new WindowsUserNameCachingSecurityTokenAuthenticator(this.extractGroupsForWindowsAccounts,
        //        //            serviceCredentials.UserNameAuthentication.MaxCachedLogonTokens, serviceCredentials.UserNameAuthentication.CachedLogonTokenLifetime);
        //        //    }
        //        //    else
        //        //    {
        //        //        this.userNameTokenAuthenticator = new WindowsUserNameSecurityTokenAuthenticator(this.extractGroupsForWindowsAccounts);
        //        //    }
        //        //}
        //    }
        //    else
        //    {
        //        //this.extractGroupsForWindowsAccounts = TransportDefaults.ExtractGroupsForWindowsAccounts;
        //        //this.userNameTokenAuthenticator = new WindowsUserNameSecurityTokenAuthenticator(this.extractGroupsForWindowsAccounts);

        //        throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(
        //            new NotSupportedException(
        //                SSR.NoSecurityCredentialsManagersInServiceBindingParameters));
        //    }

        //    this.windowsTokenAuthenticator = new WindowsSecurityTokenAuthenticator(this.extractGroupsForWindowsAccounts);
        //}

        //protected override void OnOpened()
        //{
        //    base.OnOpened();

        //    if (this.IsAuthenticationSupported)
        //    {
        //        InitializeSecurityTokenAuthenticator();
        //        this.identity = GetIdentityModelProperty<EndpointIdentity>();
        //    }
        //}

//        [MethodImpl(MethodImplOptions.NoInlining)]
//        protected void CloseUserNameTokenAuthenticator(TimeSpan timeout)
//        {
//            SecurityUtils.CloseTokenAuthenticatorIfRequired(this.userNameTokenAuthenticator, timeout);
//        }

//        [MethodImpl(MethodImplOptions.NoInlining)]
//        protected void AbortUserNameTokenAuthenticator()
//        {
//            SecurityUtils.AbortTokenAuthenticatorIfRequired(this.userNameTokenAuthenticator);
//        }

//        bool ShouldProcessAuthentication(IHttpAuthenticationContext authenticationContext)
//        {
//            Fx.Assert(authenticationContext != null, "IsAuthenticated should only be called if authenticationContext != null");
//            Fx.Assert(authenticationContext.LogonUserIdentity != null, "IsAuthenticated should only be called if authenticationContext.LogonUserIdentity != null");
//            return this.IsAuthenticationRequired || (this.IsAuthenticationSupported && authenticationContext.LogonUserIdentity.IsAuthenticated);
//        }

//        bool ShouldProcessAuthentication(HttpListenerContext listenerContext)
//        {
//            Fx.Assert(listenerContext != null, "IsAuthenticated should only be called if listenerContext != null");
//            Fx.Assert(listenerContext.Request != null, "IsAuthenticated should only be called if listenerContext.Request != null");
//            return this.IsAuthenticationRequired || (this.IsAuthenticationSupported && listenerContext.Request.IsAuthenticated);
//        }

//        public virtual SecurityMessageProperty ProcessAuthentication(IHttpAuthenticationContext authenticationContext)
//        {
//            if (this.ShouldProcessAuthentication(authenticationContext))
//            {
//                SecurityMessageProperty retValue;
//                try
//                {
//                    retValue = this.ProcessAuthentication(authenticationContext.LogonUserIdentity, GetAuthType(authenticationContext));
//                }
//#pragma warning suppress 56500 // covered by FXCop
//                catch (Exception exception)
//                {
//                    if (Fx.IsFatal(exception))
//                        throw;

//                    // Audit Authentication failure
//                    if (AuditLevel.Failure == (this.AuditBehavior.MessageAuthenticationAuditLevel & AuditLevel.Failure))
//                        WriteAuditEvent(AuditLevel.Failure, (authenticationContext.LogonUserIdentity != null) ? authenticationContext.LogonUserIdentity.Name : String.Empty, exception);

//                    throw;
//                }

//                // Audit Authentication success
//                if (AuditLevel.Success == (this.AuditBehavior.MessageAuthenticationAuditLevel & AuditLevel.Success))
//                    WriteAuditEvent(AuditLevel.Success, (authenticationContext.LogonUserIdentity != null) ? authenticationContext.LogonUserIdentity.Name : String.Empty, null);

//                return retValue;
//            }
//            else
//            {
//                return null;
//            }
//        }

//        public virtual SecurityMessageProperty ProcessAuthentication(HttpListenerContext listenerContext)
//        {
//            if (this.ShouldProcessAuthentication(listenerContext))
//            {
//                return this.ProcessRequiredAuthentication(listenerContext);
//            }
//            else
//            {
//                return null;
//            }
//        }

//        SecurityMessageProperty ProcessRequiredAuthentication(HttpListenerContext listenerContext)
//        {
//            SecurityMessageProperty retValue;
//            HttpListenerBasicIdentity identity = null;
//            WindowsIdentity wid = null;
//            try
//            {
//                Fx.Assert(listenerContext.User != null, "HttpListener delivered authenticated request without an IPrincipal.");
//                wid = listenerContext.User.Identity as WindowsIdentity;

//                if (this.AuthenticationScheme.IsSet(AuthenticationSchemes.Basic)
//                    && wid == null)
//                {
//                    identity = listenerContext.User.Identity as HttpListenerBasicIdentity;
//                    Fx.Assert(identity != null, "HttpListener delivered Basic authenticated request with a non-Basic IIdentity.");
//                    retValue = this.ProcessAuthentication(identity);
//                }
//                else
//                {
//                    Fx.Assert(wid != null, "HttpListener delivered non-Basic authenticated request with a non-Windows IIdentity.");
//                    retValue = this.ProcessAuthentication(wid, GetAuthType(listenerContext));
//                }
//            }
//#pragma warning suppress 56500 // covered by FXCop
//            catch (Exception exception)
//            {
//                if (!Fx.IsFatal(exception))
//                {
//                    // Audit Authentication failure
//                    if (AuditLevel.Failure == (this.AuditBehavior.MessageAuthenticationAuditLevel & AuditLevel.Failure))
//                    {
//                        WriteAuditEvent(AuditLevel.Failure, (identity != null) ? identity.Name : ((wid != null) ? wid.Name : String.Empty), exception);
//                    }
//                }
//                throw;
//            }

//            // Audit Authentication success
//            if (AuditLevel.Success == (this.AuditBehavior.MessageAuthenticationAuditLevel & AuditLevel.Success))
//            {
//                WriteAuditEvent(AuditLevel.Success, (identity != null) ? identity.Name : ((wid != null) ? wid.Name : String.Empty), null);
//            }

//            return retValue;
//        }

//        protected override bool TryGetTransportManagerRegistration(HostNameComparisonMode hostNameComparisonMode,
//            out ITransportManagerRegistration registration)
//        {
//            if (this.TransportManagerTable.TryLookupUri(this.Uri, hostNameComparisonMode, out registration))
//            {
//                HttpTransportManager httpTransportManager = registration as HttpTransportManager;
//                if (httpTransportManager != null && httpTransportManager.IsHosted)
//                {
//                    return true;
//                }
//                // Due to HTTP.SYS behavior, we don't reuse registrations from a higher point in the URI hierarchy.
//                if (registration.ListenUri.Segments.Length >= this.BaseUri.Segments.Length)
//                {
//                    return true;
//                }
//            }
//            return false;
//        }

//        protected void WriteAuditEvent(AuditLevel auditLevel, string primaryIdentity, Exception exception)
//        {
//            try
//            {
//                if (auditLevel == AuditLevel.Success)
//                {
//                    SecurityAuditHelper.WriteTransportAuthenticationSuccessEvent(this.AuditBehavior.AuditLogLocation,
//                        this.AuditBehavior.SuppressAuditFailure, null, this.Uri, primaryIdentity);
//                }
//                else
//                {
//                    SecurityAuditHelper.WriteTransportAuthenticationFailureEvent(this.AuditBehavior.AuditLogLocation,
//                        this.AuditBehavior.SuppressAuditFailure, null, this.Uri, primaryIdentity, exception);
//                }
//            }
//#pragma warning suppress 56500
//            catch (Exception auditException)
//            {
//                if (Fx.IsFatal(auditException) || auditLevel == AuditLevel.Success)
//                    throw;

//                DiagnosticUtility.TraceHandledException(auditException, TraceEventType.Error);
//            }
//        }

//        SecurityMessageProperty ProcessAuthentication(HttpListenerBasicIdentity identity)
//        {
//            SecurityToken securityToken = new UserNameSecurityToken(identity.Name, identity.Password);
//            ReadOnlyCollection<IAuthorizationPolicy> authorizationPolicies = this.userNameTokenAuthenticator.ValidateToken(securityToken);
//            SecurityMessageProperty security = new SecurityMessageProperty();
//            security.TransportToken = new SecurityTokenSpecification(securityToken, authorizationPolicies);
//            security.ServiceSecurityContext = new ServiceSecurityContext(authorizationPolicies);
//            return security;
//        }

//        SecurityMessageProperty ProcessAuthentication(WindowsIdentity identity, string authenticationType)
//        {
//            SecurityUtils.ValidateAnonymityConstraint(identity, false);
//            SecurityToken securityToken = new WindowsSecurityToken(identity, SecurityUniqueId.Create().Value, authenticationType);
//            ReadOnlyCollection<IAuthorizationPolicy> authorizationPolicies = this.windowsTokenAuthenticator.ValidateToken(securityToken);
//            SecurityMessageProperty security = new SecurityMessageProperty();
//            security.TransportToken = new SecurityTokenSpecification(securityToken, authorizationPolicies);
//            security.ServiceSecurityContext = new ServiceSecurityContext(authorizationPolicies);
//            return security;
//        }

//        HttpStatusCode ValidateAuthentication(string authType)
//        {
//            if (this.IsAuthSchemeValid(authType))
//            {
//                return HttpStatusCode.OK;
//            }
//            else
//            {
//                // Audit Authentication failure
//                if (AuditLevel.Failure == (this.AuditBehavior.MessageAuthenticationAuditLevel & AuditLevel.Failure))
//                {
//                    string message = SSR.Format(SSR.HttpAuthenticationFailed, this.AuthenticationScheme, HttpStatusCode.Unauthorized);
//                    Exception exception = DiagnosticUtility.ExceptionUtility.ThrowHelperError(new MessageSecurityException(message));
//                    WriteAuditEvent(AuditLevel.Failure, String.Empty, exception);
//                }

//                return HttpStatusCode.Unauthorized;
//            }
//        }

//        public virtual HttpStatusCode ValidateAuthentication(IHttpAuthenticationContext authenticationContext)
//        {
//            HttpStatusCode result = HttpStatusCode.OK;

//            if (this.IsAuthenticationSupported)
//            {
//                string authType = GetAuthType(authenticationContext);
//                result = ValidateAuthentication(authType);
//            }

//            if (result == HttpStatusCode.OK &&
//                authenticationContext.LogonUserIdentity != null &&
//                authenticationContext.LogonUserIdentity.IsAuthenticated &&
//                this.ExtendedProtectionPolicy.PolicyEnforcement == PolicyEnforcement.Always &&
//                !authenticationContext.IISSupportsExtendedProtection)
//            {
//                Exception exception = DiagnosticUtility.ExceptionUtility.ThrowHelperError(
//                    new PlatformNotSupportedException(SSR.ExtendedProtectionNotSupported));
//                WriteAuditEvent(AuditLevel.Failure, String.Empty, exception);

//                result = HttpStatusCode.Unauthorized;
//            }

//            return result;
//        }

//        public virtual HttpStatusCode ValidateAuthentication(HttpListenerContext listenerContext)
//        {
//            HttpStatusCode result = HttpStatusCode.OK;

//            if (this.IsAuthenticationSupported)
//            {
//                string authType = GetAuthType(listenerContext);
//                result = ValidateAuthentication(authType);
//            }

//            return result;
//        }

//        static ExtendedProtectionPolicy GetPolicyWithDefaultSpnCollection(ExtendedProtectionPolicy policy, AuthenticationSchemes authenticationScheme, HostNameComparisonMode hostNameComparisonMode, Uri listenUri, out bool usingDefaultSpnList)
//        {
//            if (policy.PolicyEnforcement != PolicyEnforcement.Never &&
//                policy.CustomServiceNames == null && //null indicates "use default"
//                policy.CustomChannelBinding == null && //not needed if a channel binding is provided.
//                authenticationScheme != AuthenticationSchemes.Anonymous && //SPN list only needed with authentication (mixed mode uses own default list)
//                string.Equals(listenUri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))//SPN list not used for HTTPS (CBT is used instead).
//            {
//                usingDefaultSpnList = true;
//                return new ExtendedProtectionPolicy(policy.PolicyEnforcement, policy.ProtectionScenario, GetDefaultSpnList(hostNameComparisonMode, listenUri));
//            }

//            usingDefaultSpnList = false;
//            return policy;
//        }

//        static ServiceNameCollection GetDefaultSpnList(HostNameComparisonMode hostNameComparisonMode, Uri listenUri)
//        {
//            //In 3.5 SP1, we started sending the HOST/xyz format, so we have to accept it for compat reasons.
//            //with this change, we will be changing our client so that it lets System.Net pick the SPN by default
//            //which will usually mean they use the HTTP/xyz format, which is more likely to interop with
//            //other web service stacks that support windows auth...
//            const string hostSpnFormat = "HOST/{0}";
//            const string httpSpnFormat = "HTTP/{0}";
//            const string localhost = "localhost";

//            Dictionary<string, string> serviceNames = new Dictionary<string, string>();

//            string hostName = null;
//            string dnsSafeHostName = listenUri.DnsSafeHost;

//            switch (hostNameComparisonMode)
//            {
//                case HostNameComparisonMode.Exact:
//                    UriHostNameType hostNameType = listenUri.HostNameType;
//                    if (hostNameType == UriHostNameType.IPv4 || hostNameType == UriHostNameType.IPv6)
//                    {
//                        hostName = Dns.GetHostEntry(string.Empty).HostName;
//                        AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, hostName));
//                        AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, hostName));
//                    }
//                    else
//                    {
//                        if (listenUri.DnsSafeHost.Contains("."))
//                        {
//                            //since we are listening explicitly on the FQDN, we should add only the FQDN SPN
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, dnsSafeHostName));
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, dnsSafeHostName));
//                        }
//                        else
//                        {
//                            hostName = Dns.GetHostEntry(string.Empty).HostName;
//                            //add the short name (from the URI) and the FQDN (from Dns)
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, dnsSafeHostName));
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, dnsSafeHostName));
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, hostName));
//                            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, hostName));
//                        }
//                    }
//                    break;
//                case HostNameComparisonMode.StrongWildcard:
//                case HostNameComparisonMode.WeakWildcard:
//                    hostName = Dns.GetHostEntry(string.Empty).HostName;
//                    AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, hostName));
//                    AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, hostName));
//                    break;
//                default:
//                    Fx.Assert("Unhandled HostNameComparisonMode: " + hostNameComparisonMode);
//                    break;
//            }

//            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, hostSpnFormat, localhost));
//            AddSpn(serviceNames, string.Format(CultureInfo.InvariantCulture, httpSpnFormat, localhost));

//            return new ServiceNameCollection(serviceNames.Values);
//        }

//        static void AddSpn(Dictionary<string, string> list, string value)
//        {
//            string key = value.ToLowerInvariant();

//            if (!list.ContainsKey(key))
//            {
//                list.Add(key, value);
//            }
//        }

//        public abstract bool CreateWebSocketChannelAndEnqueue(HttpRequestContext httpRequestContext, HttpPipeline httpPipeline, HttpResponseMessage httpResponseMessage, string subProtocol, Action dequeuedCallback);

//        public abstract byte[] TakeWebSocketInternalBuffer();
//        public abstract void ReturnWebSocketInternalBuffer(byte[] buffer);

//        internal interface IHttpAuthenticationContext
//        {
//            WindowsIdentity LogonUserIdentity { get; }
//            X509Certificate2 GetClientCertificate(out bool isValidCertificate);
//            bool IISSupportsExtendedProtection { get; }
//            TraceRecord CreateTraceRecord();
//        }
    }

    internal abstract class ZeroMQRequestChannelListener<TChannel> :
        ZeroMQTransferTransportChannelListenerBase, IChannelListener<TChannel> where TChannel : class, IChannel
    {
        InputQueueChannelAcceptor<TChannel> acceptor;
        TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> transportIntegrationHandler;

        protected ZeroMQRequestChannelListener(ZeroMQTransferTransportBindingElementBase bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
            this.acceptor = (InputQueueChannelAcceptor<TChannel>)(object)(new TransportReplyChannelAcceptor(this));
            this.CreatePipeline(bindingElement.MessageHandlerFactory);
        }

        public InputQueueChannelAcceptor<TChannel> Acceptor
        {
            get { return this.acceptor; }
        }

        public TChannel AcceptChannel()
        {
            return this.AcceptChannel(this.DefaultReceiveTimeout);
        }

        public IAsyncResult BeginAcceptChannel(AsyncCallback callback, object state)
        {
            return this.BeginAcceptChannel(this.DefaultReceiveTimeout, callback, state);
        }

        public TChannel AcceptChannel(TimeSpan timeout)
        {
            base.ThrowIfNotOpened();
            return this.Acceptor.AcceptChannel(timeout);
        }

        public IAsyncResult BeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            base.ThrowIfNotOpened();
            return this.Acceptor.BeginAcceptChannel(timeout, callback, state);
        }

        public TChannel EndAcceptChannel(IAsyncResult result)
        {
            base.ThrowPending();
            return this.Acceptor.EndAcceptChannel(result);
        }

        protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return new ChainedOpenAsyncResult(timeout, callback, state, base.OnBeginOpen, base.OnEndOpen, this.Acceptor);
        }

        protected internal override Task OnOpenAsync(TimeSpan timeout)
        {
            return Task.Factory.FromAsync(OnBeginOpen, OnEndOpen, timeout, null);
        }

        protected internal override Task OnCloseAsync(TimeSpan timeout)
        {
            return Task.Factory.FromAsync(OnBeginClose, OnEndClose, timeout, null);
        }

        protected override void OnOpen(TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            base.OnOpen(timeoutHelper.RemainingTime());
            this.Acceptor.Open(timeoutHelper.RemainingTime());
        }

        protected override void OnEndOpen(IAsyncResult result)
        {
            ChainedOpenAsyncResult.End(result);
        }

        protected override void OnClose(TimeSpan timeout)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            this.Acceptor.Close(timeoutHelper.RemainingTime());
            //if (this.IsAuthenticationSupported)
            //{
            //    CloseUserNameTokenAuthenticator(timeoutHelper.RemainingTime());
            //}
            //if (this.useWebSocketTransport)
            //{
            //    //this.webSocketLifetimeManager.Close(timeoutHelper.RemainingTime());
            //}
            base.OnClose(timeoutHelper.RemainingTime());
        }

        protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
        {
            TimeoutHelper timeoutHelper = new TimeoutHelper(timeout);
            //ICommunicationObject[] communicationObjects;
            //ICommunicationObject communicationObject = this.UserNameTokenAuthenticator as ICommunicationObject;
            //if (communicationObject == null)
            //{
            //    if (this.IsAuthenticationSupported)
            //    {
            //        CloseUserNameTokenAuthenticator(timeoutHelper.RemainingTime());
            //    }
            //    communicationObjects = new ICommunicationObject[] { this.Acceptor };
            //}
            //else
            //{
            //    communicationObjects = new ICommunicationObject[] { this.Acceptor, communicationObject };
            //}

            //if (this.useWebSocketTransport)
            //{
            //    return Task.FromException(NotImplemented.ByDesign);
            //    //return new LifetimeWrappedCloseAsyncResult<ServerWebSocketTransportDuplexSessionChannel>(
            //    //    timeoutHelper.RemainingTime(),
            //    //    callback,
            //    //    state,
            //    //    this.webSocketLifetimeManager,
            //    //    base.OnBeginClose,
            //    //    base.OnEndClose,
            //    //    communicationObjects);
            //}
            //else
            {
                //return new ChainedCloseAsyncResult(timeoutHelper.RemainingTime(), callback, state, base.OnBeginClose, base.OnEndClose, communicationObjects);
                return new ChainedCloseAsyncResult(timeoutHelper.RemainingTime(), callback, state, base.OnBeginClose, base.OnEndClose, new ICommunicationObject[] { this.Acceptor });
            }
        }

        protected override void OnEndClose(IAsyncResult result)
        {
            //if (this.useWebSocketTransport)
            //{
            //    //LifetimeWrappedCloseAsyncResult<ServerWebSocketTransportDuplexSessionChannel>.End(result);
            //}
            //else
            {
                ChainedCloseAsyncResult.End(result);
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            //if (this.bufferPool != null)
            //{
            //    this.bufferPool.Close();
            //}

            if (this.transportIntegrationHandler != null)
            {
                this.transportIntegrationHandler.Dispose();
            }
        }

        protected override void OnAbort()
        {
            //if (this.IsAuthenticationSupported)
            //{
            //    AbortUserNameTokenAuthenticator();
            //}

            this.Acceptor.Abort();

            //if (this.useWebSocketTransport)
            //{
            //    //this.webSocketLifetimeManager.Abort();
            //}

            base.OnAbort();
        }

        protected override bool OnWaitForChannel(TimeSpan timeout)
        {
            return Acceptor.WaitForChannel(timeout);
        }

        protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
        {
            return Acceptor.BeginWaitForChannel(timeout, callback, state);
        }

        protected override bool OnEndWaitForChannel(IAsyncResult result)
        {
            return Acceptor.EndWaitForChannel(result);
        }

        internal override IAsyncResult BeginZeroMQRequestContextReceived(ZeroMQRequestContext context,
                                                        Action acceptorCallback,
                                                        AsyncCallback callback,
                                                        object state)
        {
            return new ZeroMQRequestContextReceivedAsyncResult<TChannel>(
                context,
                acceptorCallback,
                this,
                callback,
                state);
        }

        internal override bool EndZeroMQRequestContextReceived(IAsyncResult result)
        {
            return ZeroMQRequestContextReceivedAsyncResult<TChannel>.End(result);
        }

        void CreatePipeline(ZeroMQTransferMessagingHandlerFactory transferMessageHandlerFactory)
        {
            TransferMessagingHandler<ZeroMQRequestMessage, ZeroMQResponseMessage> innerPipeline;

            if (transferMessageHandlerFactory == null)
            {
                return;
            }

            innerPipeline = transferMessageHandlerFactory.Create(new TransferMessagingPipelineIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>(this));

            if (innerPipeline == null)
            {
                throw FxTrace.Exception.AsError(
                    new InvalidOperationException(SSR.Format(SSR.HttpMessageHandlerChannelFactoryNullPipeline,
                        transferMessageHandlerFactory.GetType().Name, typeof(HttpRequestContext).Name)));
            }

            this.transportIntegrationHandler = new TransferMessagingIntegrationHandler<ZeroMQRequestMessage, ZeroMQResponseMessage>(innerPipeline);
        }

        static void HandleProcessInboundException(Exception ex, ZeroMQRequestContext context)
        {
            if (Fx.IsFatal(ex))
            {
                return;
            }

            if (ex is ProtocolException)
            {
                ProtocolException protocolException = (ProtocolException)ex;
                //HttpStatusCode statusCode = HttpStatusCode.BadRequest;
                //string statusDescription = string.Empty;
                //if (protocolException.Data.Contains(HttpChannelUtilities.HttpStatusCodeExceptionKey))
                //{
                //    statusCode = (HttpStatusCode)protocolException.Data[HttpChannelUtilities.HttpStatusCodeExceptionKey];
                //    protocolException.Data.Remove(HttpChannelUtilities.HttpStatusCodeExceptionKey);
                //}
                //if (protocolException.Data.Contains(HttpChannelUtilities.HttpStatusDescriptionExceptionKey))
                //{
                //    statusDescription = (string)protocolException.Data[HttpChannelUtilities.HttpStatusDescriptionExceptionKey];
                //    protocolException.Data.Remove(HttpChannelUtilities.HttpStatusDescriptionExceptionKey);
                //}
                //context.SendResponseAndClose(statusCode, statusDescription);
                context.SendResponseAndClose(protocolException);
            }
            else
            {
                try
                {
                    //context.SendResponseAndClose(new ProtocolException( HttpStatusCode.BadRequest);

                    MessageFault fault = MessageFault.CreateFault(FaultCode.CreateSenderFaultCode(null), "BadRequest");

                    FaultReasonText reason = fault.Reason.GetMatchingTranslation(CultureInfo.CurrentCulture);

                    context.SendResponseAndClose(new ProtocolException(reason.Text));
                }
                catch (Exception closeException)
                {
                    if (Fx.IsFatal(closeException))
                    {
                        throw;
                    }

                    DiagnosticUtility.TraceHandledException(closeException, TraceEventType.Error);
                }
            }
        }

        static bool ContextReceiveExceptionHandled(Exception e)
        {
            if (Fx.IsFatal(e))
            {
                return false;
            }

            if (e is CommunicationException)
            {
                DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
            }
            else if (e is XmlException)
            {
                DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
            }
            else if (e is IOException)
            {
                DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
            }
            else if (e is TimeoutException)
            {
                if (TD.ReceiveTimeoutIsEnabled())
                {
                    TD.ReceiveTimeout(e.Message);
                }
                DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
            }
            else if (e is OperationCanceledException)
            {
                DiagnosticUtility.TraceHandledException(e, TraceEventType.Information);
            }
            else if (!ExceptionHandler.HandleTransportExceptionHelper(e))
            {
                return false;
            }

            return true;
        }

        class ZeroMQRequestContextReceivedAsyncResult<TListenerChannel> : TraceAsyncResult where TListenerChannel : class, IChannel
        {
            static AsyncCallback onProcessInboundRequest = Fx.ThunkCallback(OnProcessInboundRequest);
            bool enqueued;
            ZeroMQRequestContext context;
            Action acceptorCallback;
            ZeroMQRequestChannelListener<TListenerChannel> listener;

            public ZeroMQRequestContextReceivedAsyncResult(
                ZeroMQRequestContext requestContext,
                Action acceptorCallback,
                ZeroMQRequestChannelListener<TListenerChannel> listener,
                AsyncCallback callback,
                object state)
                : base(callback, state)
            {
                this.context = requestContext;
                this.acceptorCallback = acceptorCallback;
                this.listener = listener;

                if (this.ProcessZeroMQRequestContextAsync() == AsyncCompletionResult.Completed)
                {
                    base.Complete(true);
                }
            }

            public static bool End(IAsyncResult result)
            {
                return AsyncResult.End<ZeroMQRequestContextReceivedAsyncResult<TListenerChannel>>(result).enqueued;
            }

            static void OnProcessInboundRequest(IAsyncResult result)
            {
                if (result.CompletedSynchronously)
                {
                    return;
                }

                ZeroMQRequestContextReceivedAsyncResult<TListenerChannel> thisPtr = (ZeroMQRequestContextReceivedAsyncResult<TListenerChannel>)result.AsyncState;
                Exception completionException = null;

                try
                {
                    thisPtr.HandleProcessInboundRequest(result);
                }
                catch (Exception ex)
                {
                    if (Fx.IsFatal(ex))
                    {
                        throw;
                    }

                    completionException = ex;
                }

                thisPtr.Complete(false, completionException);
            }

            AsyncCompletionResult ProcessZeroMQRequestContextAsync()
            {
                bool abort = false;
                try
                {
                    this.context.InitializeRequestPipeline(this.listener.transportIntegrationHandler);
                    //if (!this.Authenticate())
                    //{
                    //    return AsyncCompletionResult.Completed;
                    //}

                    //if (listener.UseWebSocketTransport && !context.IsWebSocketRequest)
                    //{
                    //    this.context.SendResponseAndClose(HttpStatusCode.BadRequest, SSR.WebSocketEndpointOnlySupportWebSocketError);
                    //    return AsyncCompletionResult.Completed;
                    //}

                    //if (!listener.UseWebSocketTransport && context.IsWebSocketRequest)
                    //{
                    //    this.context.SendResponseAndClose(HttpStatusCode.BadRequest, SSR.WebSocketEndpointDoesNotSupportWebSocketError);
                    //    return AsyncCompletionResult.Completed;
                    //}

                    try
                    {
                        IAsyncResult result =
                            context.BeginProcessInboundRequest(
                                listener.Acceptor as ReplyChannelAcceptor,
                                this.acceptorCallback,
                                onProcessInboundRequest,
                                this);
                        if (result.CompletedSynchronously)
                        {
                            this.EndInboundProcessAndEnqueue(result);
                            return AsyncCompletionResult.Completed;
                        }
                    }
                    catch (Exception ex)
                    {
                        HandleProcessInboundException(ex, this.context);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // containment -- we abort the context in all error cases, no additional containment action needed                
                    abort = true;
                    if (!ContextReceiveExceptionHandled(ex))
                    {
                        throw;
                    }
                }
                finally
                {
                    if (abort)
                    {
                        context.Abort();
                    }
                }

                return abort ? AsyncCompletionResult.Completed : AsyncCompletionResult.Queued;
            }

            //bool Authenticate()
            //{
            //    if (!this.context.ProcessAuthentication())
            //    {
            //        if (TD.HttpAuthFailedIsEnabled())
            //        {
            //            TD.HttpAuthFailed(context.EventTraceActivity);
            //        }

            //        if (DiagnosticUtility.ShouldTraceInformation)
            //        {
            //            TraceUtility.TraceEvent(TraceEventType.Information, TraceCode.HttpAuthFailed, SSR.TraceCodeHttpAuthFailed, this);
            //        }

            //        return false;
            //    }

            //    return true;
            //}

            void HandleProcessInboundRequest(IAsyncResult result)
            {
                bool abort = true;
                try
                {
                    try
                    {
                        this.EndInboundProcessAndEnqueue(result);
                        abort = false;
                    }
                    catch (Exception ex)
                    {
                        HandleProcessInboundException(ex, this.context);
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    // containment -- we abort the context in all error cases, no additional containment action needed                                    
                    if (!ContextReceiveExceptionHandled(ex))
                    {
                        throw;
                    }
                }
                finally
                {
                    if (abort)
                    {
                        context.Abort();
                    }
                }
            }

            void EndInboundProcessAndEnqueue(IAsyncResult result)
            {
                Fx.Assert(result != null, "Trying to complete without issuing a BeginProcessInboundRequest.");
                context.EndProcessInboundRequest(result);

                //We have finally managed to enqueue the message.
                this.enqueued = true;
            }
        }
    }

    internal class ZeroMQIpcRequestChannelListener<TChannel> :
        ZeroMQRequestChannelListener<TChannel> where TChannel : class, IChannel
    {
        public ZeroMQIpcRequestChannelListener(ZeroMQIpcTransferTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_IPC;
    }

    internal class ZeroMQTcpRequestChannelListener<TChannel> :
        ZeroMQRequestChannelListener<TChannel> where TChannel : class, IChannel
    {
        public ZeroMQTcpRequestChannelListener(ZeroMQTcpTransferTransportBindingElement bindingElement, BindingContext context) :
            base(bindingElement, context)
        {
        }

        public override string Scheme => ResourceProtocolSchemes.ZEROMQ_TCP;
    }
}