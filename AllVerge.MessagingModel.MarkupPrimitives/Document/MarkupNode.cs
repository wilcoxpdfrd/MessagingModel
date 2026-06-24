using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AllVerge.MessagingModel.MarkupPrimitives.Document
{
    public class MarkupNode
    {
        private MarkupNode parentNode;
        private String name;
        private string nodeType;
        private MarkupAttributes attributes = new MarkupAttributes();
        private List<String> childNames = new List<string>();
        private List<MarkupNode> childNodes = new List<MarkupNode>();
        private StringBuilder nodeTextBuilder = new StringBuilder();
        private bool nullText = false;

        public MarkupNode(MarkupNode parentNode, string name, string nodeType)
        {
            this.name = name;
            this.nodeType = nodeType;

            SetParentNode(parentNode);
        }

        public MarkupNode ParentNode { get { return this.parentNode; } }

        public string Name { get { return this.name; } }

        public string NodeType { get { return this.nodeType; } }

        public IEnumerable<string> ChildNames { get { return childNames; } }

        public IEnumerable<MarkupNode> Children { get { return this.childNodes; } }

        public String Text { get { return this.nullText ? null : this.nodeTextBuilder.ToString(); } }

        public MarkupAttributes Attributes { get { return this.attributes; } }

        public bool HasAttributes { get { return this.attributes.Count > 0; } }

        public bool IsName(string name)
        {
            return this.name == name;
        }

        public bool IsParentName(string name)
        {
            return this.parentNode != null && this.parentNode.IsName(name);
        }

        public bool AddChildName(string childName)
        {
            if (!String.IsNullOrWhiteSpace(childName))
            {
                this.childNames.Add(childName);

                return true;
            }

            return false;
        }

        public int AddAttributes(params MarkupAttribute[] childAttributes)
        {
            return this.AddAttributes((IEnumerable<MarkupAttribute>)childAttributes);
        }

        public int AddAttributes(IEnumerable<MarkupAttribute> childAttributes)
        {
            if (childAttributes != null)
            {
                foreach (MarkupAttribute childAttribute in childAttributes)

                    this.attributes.Add(childAttribute);

                return this.attributes.Count;
            }

            return 0;
        }

        public void SetNullText()
        {
            this.nullText = true;
        }

        public bool AddText(string text)
        {
            if (!String.IsNullOrEmpty(text))
            {
                this.nodeTextBuilder.Append(text);

                return true;
            }

            return false;
        }

        public bool ClearText()
        {
            return this.nodeTextBuilder.Clear() != null;
        }

        public bool RemoveChildNode(MarkupNode childNode)
        {
            if (this.childNames.Remove(childNode.name) && this.childNodes.Remove(childNode))
            {
                childNode.parentNode = null;

                return true;
            }

            return false;
        }

        public bool SetParentNode(MarkupNode parentNode)
        {
            if (parentNode != null)
            {
                this.parentNode = parentNode;

                if (!this.parentNode.childNames.Contains(this.Name))
                {
                    if (this.Name.StartsWith("$"))

                        this.parentNode.childNames.Insert(0, this.Name);

                    else

                        this.parentNode.childNames.Add(this.Name);
                }

                if (this.Name.StartsWith("$"))

                    this.parentNode.childNodes.Insert(0, this);

                else

                    this.parentNode.childNodes.Add(this);

                return true;
            }

            return false;
        }

        public MarkupNode Clone(MarkupNode parentNode, String nodeName = null)
        {
            MarkupNode clone;
            
            if (nodeName != null)

                clone = new MarkupNode(parentNode, nodeName, this.NodeType);

            else

                clone = new MarkupNode(parentNode, this.Name, this.NodeType);

            clone.attributes = this.Attributes.Clone();
            clone.childNames = new List<string>(this.childNames.Select(n => (String)n.Clone())); 
            clone.childNodes = new List<MarkupNode>(this.childNodes.Clone(clone));
            clone.nodeTextBuilder = new StringBuilder(this.nodeTextBuilder.ToString());

            return clone;
        }
    }
}
