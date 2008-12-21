namespace ServiceStack.Messaging
{
    public class Destination : IDestination
    {
        private DestinationType destinationType;
        private string uri;

        public Destination(DestinationType destinationType, string uri)
        {
            this.destinationType = destinationType;
            this.uri = uri;
        }

        public DestinationType DestinationType
        {
            get { return destinationType; }
            set { destinationType = value; }
        }

        public string Uri
        {
            get { return uri; }
            set { uri = value; }
        }

        public override string ToString()
        {
            return string.Format("[type='{0}' uri='{1}']", destinationType, uri);
        }
    }
}