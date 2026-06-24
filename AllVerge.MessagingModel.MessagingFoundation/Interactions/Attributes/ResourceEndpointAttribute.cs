using System;

using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    using AllVerge.MessagingModel.MessagingFoundation.Description;

    /// <summary>Indicates that the decorated method is associated with a resource endpoint in a service contract .</summary>
    [AttributeUsage(AttributeTargets.Method)]
    public abstract class ResourceEndpointAttribute : Attribute, IOperationContractAttributeProvider, IOperationEndpointBehavior
    {
        private string methodName;
        private bool asyncPattern;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionAttribute" /> class with the given <paramref name="methodName"/>.
        /// </summary>
        /// <param name="methodName"></param>
        protected ResourceEndpointAttribute(String methodName) : this()
        {
            this.methodName = methodName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceActionAttribute" /> class.
        /// </summary>
        protected ResourceEndpointAttribute() : base()
        {
        }

        /// <summary>Gets or sets the name of the operation.</summary>
        /// <returns>The name of the operation.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <see cref="Name" /> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">The value is an empty string.</exception>
        public string Name
        {
            get
            {
                return this.methodName;
            }
            
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                if (value == "")
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", PublicSR.SFxNameCannotBeEmpty));
                }
                this.methodName = value;
            }
        }

        /// <summary>
        /// Indicates that an operation is implemented asynchronously using a Begin&lt;methodName&gt; and End&lt;methodName&gt; method pair in a service contract.
        /// </summary>
        /// <returns>true if the Begin&lt;methodName&gt;method is matched by an End&lt;methodName&gt; method and can be treated by the infrastructure as an operation that is implemented as an asynchronous method pair on the service interface; otherwise, false. The default is false.</returns>
        public bool AsyncPattern
        {
            get
            {
                return this.asyncPattern;
            }

            set
            {
                this.asyncPattern = value;
            }
        }

        void IOperationEndpointBehavior.AddBindingParameters(ServiceEndpoint endpoint, OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
            this.AddBindingParameters(endpoint, operationDescription, bindingParameters);
        }

        protected virtual void AddBindingParameters(ServiceEndpoint endpoint, OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        void IOperationBehavior.AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
            this.AddBindingParameters(operationDescription, bindingParameters);
        }

        protected virtual void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters)
        {
        }

        void IOperationBehavior.ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
            this.ApplyClientBehavior(operationDescription, clientOperation);
        }

        protected virtual void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation)
        {
        }

        void IOperationBehavior.ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            this.ApplyDispatchBehavior(operationDescription, dispatchOperation);
        }

        protected virtual void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
        }

        void IOperationBehavior.Validate(OperationDescription operationDescription)
        {
            this.Validate(operationDescription);
        }

        protected virtual void Validate(OperationDescription operationDescription)
        {
        }

        OperationContractAttribute IAttributeProvider<OperationContractAttribute>.GetAttribute()
        {
            return new OperationContractAttribute();
        }

        OperationContractAttribute IOperationContractAttributeProvider.GetOperationContractAttribute()
        {
            return (this as IAttributeProvider<OperationContractAttribute>).GetAttribute();
        }
    }
}
