using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    /// <summary>
    /// Indicates to the <see cref="JsonTextPropertyAttribute"/> that the property must be treated as Json text 
    /// when the class that contains it is serialized or deserialized.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class JsonTextPropertyAttribute : Attribute
    {
        private Type type;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTextPropertyAttribute"/> class.
        /// </summary>
        public JsonTextPropertyAttribute() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonTextPropertyAttribute"/> class.
        /// </summary>
        /// <param name="type">The <see cref="System.Type"/>  of the member to be serialized.</param>
        public JsonTextPropertyAttribute(Type type)
        {
            this.type = type;
        }

        /// <summary>
        /// Gets or sets the type of the member.
        /// </summary>
        public Type Type { get { return this.type; } set { this.type = value; } }
    }
}
