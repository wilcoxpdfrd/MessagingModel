namespace schemas.xmlsoap.org.ws._2001._10.referral {
    
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("ref", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class ref_t {
        
        private for_t forField;
        
        private if_t ifField;
        
        private go_t goField;
        
        private uri_t refIdField;
        
        private desc_t descField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        public for_t @for {
            get {
                return this.forField;
            }
            set {
                this.forField = value;
            }
        }
        
        /// <remarks/>
        public if_t @if {
            get {
                return this.ifField;
            }
            set {
                this.ifField = value;
            }
        }
        
        /// <remarks/>
        public go_t go {
            get {
                return this.goField;
            }
            set {
                this.goField = value;
            }
        }
        
        /// <remarks/>
        public uri_t refId {
            get {
                return this.refIdField;
            }
            set {
                this.refIdField = value;
            }
        }
        
        /// <remarks/>
        public desc_t desc {
            get {
                return this.descField;
            }
            set {
                this.descField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("for", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class for_t {
        
        private uri_t[] exactField;
        
        private uri_t[] prefixField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("exact")]
        public uri_t[] exact {
            get {
                return this.exactField;
            }
            set {
                this.exactField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("prefix")]
        public uri_t[] prefix {
            get {
                return this.prefixField;
            }
            set {
                this.prefixField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("refId", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class uri_t {
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        private string valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute(DataType="anyURI")]
        public string Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("desc", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class desc_t {
        
        private uri_t refAddrField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        public uri_t refAddr {
            get {
                return this.refAddrField;
            }
            set {
                this.refAddrField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("go", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class go_t {
        
        private uri_t[] viaField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("via")]
        public uri_t[] via {
            get {
                return this.viaField;
            }
            set {
                this.viaField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("invalidates", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class invalidates_t {
        
        private uri_t[] ridField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("rid")]
        public uri_t[] rid {
            get {
                return this.ridField;
            }
            set {
                this.ridField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("ttl", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class ttl_t {
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        private ulong valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public ulong Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("if", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class if_t {
        
        private ttl_t ttlField;
        
        private invalidates_t invalidatesField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        public ttl_t ttl {
            get {
                return this.ttlField;
            }
            set {
                this.ttlField = value;
            }
        }
        
        /// <remarks/>
        public invalidates_t invalidates {
            get {
                return this.invalidatesField;
            }
            set {
                this.invalidatesField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("referrals", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class referrals_t {
        
        private ref_t[] refField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ref")]
        public ref_t[] @ref {
            get {
                return this.refField;
            }
            set {
                this.refField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("query", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class query_t {
        
        private for_t forField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        public for_t @for {
            get {
                return this.forField;
            }
            set {
                this.forField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("queryResponse", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class queryResponse_t {
        
        private ref_t[] refField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("ref")]
        public ref_t[] @ref {
            get {
                return this.refField;
            }
            set {
                this.refField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("registration", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class registration_t {
        
        private ref_t refField;
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        public ref_t @ref {
            get {
                return this.refField;
            }
            set {
                this.refField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("registrationResponse", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class registrationResponse_t {
        
        private System.Xml.XmlElement[] anyField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyElementAttribute()]
        public System.Xml.XmlElement[] Any {
            get {
                return this.anyField;
            }
            set {
                this.anyField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("duplicate", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class duplicate_t {
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("notInvalidated", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class notInvalidated_t {
        
        private uri_t[] ridField;
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("rid")]
        public uri_t[] rid {
            get {
                return this.ridField;
            }
            set {
                this.ridField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
    }
    
    /// <remarks/>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("System.Xml", "4.8.9037.0")]
    [System.SerializableAttribute()]
    [System.Diagnostics.DebuggerStepThroughAttribute()]
    [System.ComponentModel.DesignerCategoryAttribute("code")]
    [System.Xml.Serialization.XmlTypeAttribute(Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral")]
    [System.Xml.Serialization.XmlRootAttribute("maxTtl", Namespace="http://schemas.xmlsoap.org/ws/2001/10/referral", IsNullable=false)]
    public partial class maxTtl_t {
        
        private System.Xml.XmlAttribute[] anyAttrField;
        
        private ulong valueField;
        
        /// <remarks/>
        [System.Xml.Serialization.XmlAnyAttributeAttribute()]
        public System.Xml.XmlAttribute[] AnyAttr {
            get {
                return this.anyAttrField;
            }
            set {
                this.anyAttrField = value;
            }
        }
        
        /// <remarks/>
        [System.Xml.Serialization.XmlTextAttribute()]
        public ulong Value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
}
