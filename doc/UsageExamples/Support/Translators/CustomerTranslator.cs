using System.Xml.Linq;
using Sakila.DomainModel;
using ServiceStack.DesignPatterns.Translator;
using ServiceStack.ServiceModel.Extensions;

namespace ServiceStack.UsageExamples.Support.Translators
{
    public sealed class CustomerTranslator : ITranslator<Customer, XElement>
    {
        public static readonly CustomerTranslator Instance = new CustomerTranslator();

        public Customer Parse(XElement from)
        {
            if (from == null) return null;
            var to = new Customer
            {
                Id = from.GetInt("Id"),
            };
            return to;
        }
    }
}