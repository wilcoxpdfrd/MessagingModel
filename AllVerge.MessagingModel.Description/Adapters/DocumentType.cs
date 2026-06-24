using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.Description.Adapters
{
    public enum DocumentType
    {
        /// <summary>
        /// http://swagger.io/
        /// </summary>
        SWAGGER20,

        /// <summary>
        /// http://raml.org/
        /// http://forums.raml.org/t/how-is-this-different-from-swagger/24/6
        /// </summary>
        RAML10,
        
        /// <summary>
        /// https://apiblueprint.org/
        /// </summary>
        APIBlueprint,
        
        /// <summary>
        /// http://www.w3.org/TR/turtle/
        /// https://en.wikipedia.org/wiki/Resource_Description_Framework
        /// </summary>
        TURTLE,
        
        /// <summary>
        /// http://www.odata.org
        /// https://msdn.microsoft.com/en-us/library/dd541188.aspx
        /// </summary>
        ODATA,

        /// <summary>
        /// https://developers.google.com/gdata/docs/developers-guide
        /// </summary>
        GDATA,

        /// <summary>
        /// http://www.rfc-editor.org/rfc/rfc5023.txt
        /// </summary>
        ATOM,

        /// <summary>
        /// http://www.w3.org/Submission/wadl/wadl.xsd
        /// </summary>
        WADL200902,

        /// <summary>
        /// https://wadl.java.net/wadl20061109.xsd
        /// </summary>
        WADL200610,

        /// <summary>
        /// https://www.w3.org/TR/2001/NOTE-wsdl-20010315
        /// </summary>
        WSDL11,

        /// <summary>
        /// https://www.w3.org/TR/2007/REC-wsdl20-20070626/
        /// </summary>
        WSDL20,

        /// <summary>
        /// https://avro.apache.org/docs/current/spec.html
        /// </summary>
        Avro,

        /// <summary>
        /// https://spec.graphql.org/
        /// </summary>
        GraphQL,


        DEFAULT,
    }
}
