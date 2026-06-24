using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;
using System.Text;
using System.Xml;

namespace AllVerge.MessagingModel.MessagingFoundation
{
    using AllVerge.MessagingModel.MessagingFoundation.Description;
    using AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes;
    using AllVerge.MessagingModel;

    public class ResourceDispatcherHost : ServiceHostBase
    {
        private Type resourceType;
        private IDisposable disposableInstance;
        private ReflectedContractCollection reflectedContracts;

        public ResourceDispatcherHost(Type resourceType, params Uri[] baseAddresses) : base()
        {
            InitializeDescription(resourceType, new UriSchemeKeyedCollection(baseAddresses));
        }

        public object SingletonInstance { get; private set; }

        protected override void OnFaulted()
        {
            base.OnFaulted();
        }
        private void InitializeDescription(Type resourceType, UriSchemeKeyedCollection baseAddresses)
        {
            if (resourceType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentNullException(nameof(resourceType)));
            }

            this.resourceType = resourceType;

            base.InitializeDescription(baseAddresses);
        }

        protected override ServiceDescription CreateDescription(out IDictionary<string, ContractDescription> implementedContracts)
        {
            if (this.resourceType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.SFxServiceHostCannotCreateDescriptionWithoutServiceType));
            }

            ServiceDescription description;
            if (this.SingletonInstance != null)
            {
                description = ServiceDescription.GetService(this.SingletonInstance);
            }
            else
            {
                description = ServiceDescription.GetService(this.resourceType);
            }
            ServiceBehaviorAttribute serviceBehavior = description.Behaviors.Find<ServiceBehaviorAttribute>();

            object serviceInstanceUsedAsABehavior = serviceBehavior?.GetWellKnownSingleton();
            if (serviceInstanceUsedAsABehavior == null)
            {
                serviceInstanceUsedAsABehavior = serviceBehavior?.GetHiddenSingleton();

                if (serviceInstanceUsedAsABehavior != null)

                    this.disposableInstance = serviceInstanceUsedAsABehavior as IDisposable;
            }

            if ((typeof(IServiceBehavior).IsAssignableFrom(this.resourceType) || typeof(IContractBehavior).IsAssignableFrom(this.resourceType))
                && serviceInstanceUsedAsABehavior == null)
            {
                serviceInstanceUsedAsABehavior = ServiceDescription.CreateImplementation(this.resourceType);
                this.disposableInstance = serviceInstanceUsedAsABehavior as IDisposable;
            }

            if (this.SingletonInstance == null)
            {
                if (serviceInstanceUsedAsABehavior is IServiceBehavior)
                {
                    description.Behaviors.Add((IServiceBehavior)serviceInstanceUsedAsABehavior);
                }
            }

            ReflectedContractCollection reflectedContracts = new ReflectedContractCollection();
            List<Type> interfaceTypes = ServiceReflector.GetInterfaces<ResourceContractAttribute, ResourceActionAttribute, IResourceMethodAttributeProvider>(this.resourceType);
            for (int i = 0; i < interfaceTypes.Count; i++)
            {
                Type interfaceType = interfaceTypes[i];
                if (!reflectedContracts.Contains(interfaceType))
                {
                    ContractDescription contract = null;
                    if (serviceInstanceUsedAsABehavior != null)
                    {
                        contract = GetContract(interfaceType, serviceInstanceUsedAsABehavior);
                    }
                    else
                    {
                        contract = GetContract(interfaceType, this.resourceType);
                    }

                    reflectedContracts.Add(contract);
                    Collection<ContractDescription> inheritedContracts = contract.GetInheritedContracts();
                    for (int j = 0; j < inheritedContracts.Count; j++)
                    {
                        ContractDescription inheritedContract = inheritedContracts[j];
                        if (!reflectedContracts.Contains(inheritedContract.ContractType))
                        {
                            reflectedContracts.Add(inheritedContract);
                        }
                    }
                }
            }
            this.reflectedContracts = reflectedContracts;

            implementedContracts = reflectedContracts.ToImplementedContracts();

            return description;
        }

        private ContractDescription GetContract(Type contractType, object serviceImplementation)
        {
            if (contractType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(contractType));
            }

            if (serviceImplementation == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(serviceImplementation));
            }

            ResourceTypeLoader typeLoader = new ResourceTypeLoader();
            Type serviceType = serviceImplementation.GetType();
            ContractDescription description = typeLoader.LoadContractDescription(contractType, serviceType, serviceImplementation);
            return description;
        }

        private ContractDescription GetContract(Type interfaceType, Type resourceType)
        {
            if (interfaceType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(interfaceType));
            }

            if (resourceType == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull(nameof(resourceType));
            }

            ResourceTypeLoader typeLoader = new ResourceTypeLoader();

            ContractDescription description = typeLoader.LoadContractDescription(interfaceType, resourceType);
            
            return description;
        }

        protected override void ApplyConfiguration()
        {
            if (this.Description == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.SFxServiceHostBaseCannotApplyConfigurationWithoutDescription));
            }

            EnsureAuthenticationAuthorizationDebug(this.Description);
        }

        internal new void AddBaseAddress(Uri baseAddress)
        {
            base.AddBaseAddress(baseAddress);
        }

        internal new IEnumerable<Uri> BaseAddresses => base.BaseAddresses;

        public override ReadOnlyCollection<ServiceEndpoint> AddDefaultEndpoints()
        {
            throw new NotSupportedException($"Adding default endpoints is not supported.  Please add endpoints using an ${nameof(AddServiceEndpoint)} overload.");
        }

        protected override void InitializeRuntime()
        {
            if (this.Description == null)
            {
                throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new InvalidOperationException(PublicSR.SFxServiceHostBaseCannotInitializeRuntimeWithoutDescription));
            }

            if (this.Description.Endpoints.Count == 0)
            {
                this.AddDefaultEndpoints();
            }

            this.EnsureAuthenticationSchemesDual();

            ResourceDispatcherBuilder dispatcherBuilder = new ResourceDispatcherBuilder();

            dispatcherBuilder.InitializeServiceHost(this.Description, this);

            SecurityValidationBehavior.Instance.AfterBuildTimeValidation(this.Description);
        }

        class ReflectedContractCollection : KeyedCollection<Type, ContractDescription>
        {
            public ReflectedContractCollection()
                : base(null, 4)
            {
            }

            protected override Type GetKeyForItem(ContractDescription item)
            {
                if (item == null)
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("item");

                return item.ContractType;
            }

            public IDictionary<string, ContractDescription> ToImplementedContracts()
            {
                Dictionary<string, ContractDescription> implementedContracts = new Dictionary<string, ContractDescription>();
                foreach (ContractDescription contract in this.Items)
                {
                    implementedContracts.Add(GetConfigKey(contract), contract);
                }
                return implementedContracts;
            }

            internal static string GetConfigKey(ContractDescription contract)
            {
                return contract.ConfigurationName;
            }
        }
    }
}
