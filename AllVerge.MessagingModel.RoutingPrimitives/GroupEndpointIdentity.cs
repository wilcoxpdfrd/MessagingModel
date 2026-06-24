using System;
using System.IdentityModel.Claims;
using System.ServiceModel;
using System.Reflection;

namespace AllVerge.MessagingModel.RoutingPrimitives
{
    using www.w3.org.ns.ws_policy;

    public class GroupEndpointIdentity : EndpointIdentity
    {
        private static readonly string GROUP_CLAIM_TYPE = $"{Policy.CLAIMS_NS}{Policy.GROUP_CLAIM_NAME}";
        //private PropertyInfo identityClaimPI;
        //private PropertyInfo claimTypePI;
        //private PropertyInfo resourcePI;
        //private PropertyInfo rightPI;

        public GroupEndpointIdentity(String groupId) : base()
        {
            Claim identityClaim = new Claim(GROUP_CLAIM_TYPE, groupId, Rights.PossessProperty);
            
            base.Initialize(identityClaim, Claim.DefaultComparer);

            //Type endpointIdentityType = this.GetType().BaseType;

            //Type claimType = endpointIdentityType.Assembly.GetType("System.IdentityModel.Claims.Claim");

            //ConstructorInfo ci = claimType.GetConstructor(new Type[] { typeof(String), typeof(Object), typeof(String) });

            //Object claim = ci.Invoke(new Object[] { GROUP_CLAIM_TYPE, groupId, "http://schemas.xmlsoap.org/ws/2005/05/identity/right/possessproperty" });

            //MethodInfo initializedInfo = endpointIdentityType.GetMethod("Initialize", BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { claimType }, null);

            //initializedInfo.Invoke(this, new Object[] { claim });

            //identityClaimPI = endpointIdentityType.GetProperty("IdentityClaim");

            //claim = endpointIdentityType.GetProperty("IdentityClaim").GetValue(this);

            //claimTypePI = claimType.GetProperty("ClaimType");

            //resourcePI = claimType.GetProperty("Resource");

            //rightPI = claimType.GetProperty("Right");
        }

        //public String IdentityClaimType
        //{
        //    get
        //    {
        //        Object claim = identityClaimPI.GetValue(this);

        //        return claimTypePI.GetValue(claim).ToString();
        //    }
        //}

        //public Object IdentityClaimResource
        //{
        //    get
        //    {
        //        //Object claim = identityClaimPI.GetValue(this);

        //        //return resourcePI.GetValue(claim).ToString();
        //    }
        //}

        //public String IdentityClaimRight
        //{
        //    get
        //    {
        //        Object claim = identityClaimPI.GetValue(this);

        //        return rightPI.GetValue(claim).ToString();
        //    }
        //}
    }
}
