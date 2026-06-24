using System;

using System.Net.Security;

using System.ServiceModel;


namespace AllVerge.MessagingModel.MessagingFoundation.Interactions.Attributes
{
    using AllVerge.SystemPrimitives.Net;
    using global::System.ServiceModel;
    using global::System.ServiceModel.Security;

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public sealed class ResourceContractAttribute : Attribute
    {
        private string configurationName;
        private string name;
        private string ns;
        private SessionMode sessionMode;
        private ProtectionLevel protectionLevel;
        private bool hasProtectionLevel;

        /// <summary>Initializes a new instance of the <see cref="System.ServiceModel.ServiceContractAttribute" /> class. </summary>
        public ResourceContractAttribute()
        {
        }

        /// <summary>Gets or sets the name used to locate the service in an application configuration file.</summary>
        /// <returns>The name used to locate the service element in an application configuration file. The default is the name of the service implementation class.</returns>
        /// <exception cref="System.ArgumentNullException">The value is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is an empty string.</exception>
        public string ConfigurationName
        {
            get
            {
                return this.configurationName;
            }
    
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                if (value == string.Empty)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", PublicSR.SFxConfigurationNameCannotBeEmpty));
                }
                this.configurationName = value;
            }
        }

        /// <summary>Gets or sets the name for the &lt;portType&gt; element in Web Services Description Language (WSDL). </summary>
        /// <returns>The default value is the name of the class or interface to which the <see cref="System.ServiceModel.ServiceContractAttribute" /> is applied. </returns>
        /// <exception cref="System.ArgumentNullException">The value is null.</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is an empty string.</exception>
        public string Name
        {
            get
            {
                return this.name;
            }
    
            set
            {
                if (value == null)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperArgumentNull("value");
                }
                if (value == string.Empty)
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value", PublicSR.SFxNameCannotBeEmpty));
                }
                this.name = value;
            }
        }

        /// <summary>Gets or sets the namespace of the &lt;portType&gt; element in Web Services Description Language (WSDL).</summary>
        /// <returns>The WSDL namespace of the &lt;portType&gt; element. The default value is "http://tempuri.org". </returns>
        public string Namespace
        {
            get
            {
                return this.ns;
            }
    
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    UriUtils.ValidateUriArgument(value, "Namespace");
                }
                this.ns = value;
            }
        }

        /// <summary>Specifies whether the binding for the contract must support the value of the <see cref="System.ServiceModel.ServiceContractAttribute.ProtectionLevel" /> property.</summary>
        /// <returns>One of the <see cref="System.Net.Security.ProtectionLevel" /> values. The default is <see cref="System.Net.Security.ProtectionLevel.None" />.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">The value is not one of the <see cref="System.Net.Security.ProtectionLevel" /> values.</exception>
        public ProtectionLevel ProtectionLevel
        {
            get
            {
                return this.protectionLevel;
            }
            set
            {
                if (!ProtectionLevelHelper.IsDefined(value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
                }
                this.protectionLevel = value;
                this.hasProtectionLevel = true;
            }
        }

        /// <summary>Gets a value that indicates whether the member has a protection level assigned.</summary>
        /// <returns>true if the <see cref="System.ServiceModel.ServiceContractAttribute.ProtectionLevel" /> property is not <see cref="System.Net.Security.ProtectionLevel.None" />; otherwise, false. The default is false.</returns>
        public bool HasProtectionLevel
        {
            get
            {
                return this.hasProtectionLevel;
            }
        }

        /// <summary>
        /// Gets or sets whether sessions are allowed, not allowed or required.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value is not one of the <see cref="SessionMode" /> values.</exception>
        /// <value>A <see cref="SessionMode" /> that indicates whether sessions are allowed, not allowed, or required.</value>
        public SessionMode SessionMode
        {
            get
            {
                return sessionMode;
            }
            set
            {
                if (!IsSessionDefined(value))
                {
                    throw DiagnosticUtility.ExceptionUtility.ThrowHelperError(new ArgumentOutOfRangeException("value"));
                }
                sessionMode = value;
            }
        }

        private bool IsSessionDefined(SessionMode sessionMode)
        {
            if (sessionMode != SessionMode.NotAllowed && sessionMode != 0)
            {
                return sessionMode == SessionMode.Required;
            }
            return true;
        }
    }
}
