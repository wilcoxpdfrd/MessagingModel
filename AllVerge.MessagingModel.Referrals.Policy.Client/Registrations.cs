using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

using Newtonsoft.Json;

using schemas.xmlsoap.org.ws._2001._10.referral;

namespace AllVerge.MessagingModel.Referrals.Policy.Client
{
    public class Registrations
    {
        [JsonProperty("registrations")]
        [XmlArray("Registrations")]
        [XmlArrayItem("Registration")]
        public List<registration_t> registrations = new List<registration_t>();

        public int Count()
        {
            return registrations.Count;
        }

        public void Add(registration_t registration)
        {
            this.registrations.Add(registration);
        }

        public void AddRange(IEnumerable<registration_t> registrations)
        {
            this.registrations.AddRange(registrations);
        }

        public bool Contains(registration_t registration)
        {
            return this.registrations.Contains(registration);
        }
    }

}
