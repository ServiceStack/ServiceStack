namespace ServiceStack.OrmLite
{
    public struct XmlValue
    {
        public string Xml { get; }
        public XmlValue(string xml) => Xml = xml;
        public override string ToString() => Xml;

        public bool Equals(XmlValue other) => Xml == other.Xml;

        public override bool Equals(object obj) => obj is XmlValue other && Equals(other);

        public override int GetHashCode() => Xml != null ? Xml.GetHashCode() : 0;

        public static implicit operator XmlValue(string expandedName) => 
            expandedName != null ? new XmlValue(expandedName) : null;
    }
}