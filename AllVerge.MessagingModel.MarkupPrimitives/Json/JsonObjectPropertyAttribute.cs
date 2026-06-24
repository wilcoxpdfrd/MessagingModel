using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Json
{
    using Newtonsoft.Json;

    /// <summary>
    /// Indicates that a public property represents an Json property when the <see cref="Newtonsoft.Json.JsonSerializer"/>
    /// serializes or deserializes the object that contains it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = true)]
    public sealed class JsonObjectPropertyAttribute : Attribute
    {
        private String propertyName;
        private Type type;

        ///// <summary>
        ///// Initializes a new instance of the <see cref="JsonObjectPropertyAttribute"/> class.
        ///// </summary>
        //public JsonObjectPropertyAttribute() { this.propertyName = null; this.type = null; }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="JsonObjectPropertyAttribute"/> class 
        ///// and specifies the name of the Json property.
        ///// </summary>
        ///// <param name="propertyName">The Json property name of the serialized property.</param>
        //public JsonObjectPropertyAttribute(string propertyName) { this.propertyName = propertyName; this.type = null; }

        ///// <summary>
        ///// Initializes a new instance of the <see cref="JsonObjectPropertyAttribute"/> class 
        ///// and specifies a type for the property to which the <see cref="JsonObjectPropertyAttribute"/> 
        ///// is applied. This type is used by the <see cref="JsonSerializer"/> when serializing or 
        ///// deserializing object that contains it.
        ///// </summary>
        ///// <param name="type">The System.Type of an object derived from the member's type.</param>
        //public JsonObjectPropertyAttribute(Type type) { this.propertyName = null; this.type = type; }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonObjectPropertyAttribute"/> class 
        /// and specifies  the name of the Json property and a derived type for the property to which the 
        /// <see cref="JsonObjectPropertyAttribute"/> is applied. This property type is used when 
        /// the <see cref="Newtonsoft.Json.JsonSerializer"/> serializes the object that contains it.
        /// </summary>
        /// <param name="name">The Json property name of the serialized property</param>
        /// <param name="type">The System.Type of an object derived from the member's type.</param>
        public JsonObjectPropertyAttribute(string name, Type type)
        {
            if (name == null)

                throw new ArgumentNullException(nameof(name));

            if (type == null)

                throw new ArgumentNullException(nameof(type));

            this.propertyName = name; this.type = type;
        }

        /// <summary>
        ///  Gets or sets the name of the generated Json property.
        /// </summary>
        public string PropertyName
        {
            get { return this.propertyName; } 
            //set { this.propertyName = value; } 
        }

        /// <summary>
        /// Gets or sets the object type used to represent the Json property.
        /// </summary>
        public Type Type
        {
            get { return this.type; }
            //set { this.type = value; }
        }
    }
}
