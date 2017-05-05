using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.OpenApi.Tests.Services
{

    public class Pet
    {
        virtual public int? PetId { get; set; }
        virtual public string Name { get; set; }
    }

    [Route("/Pets", "POST")]
    public class PetsPOSTRequest : Pet, IReturn<Pet>
    {
        [System.Runtime.Serialization.IgnoreDataMember]
        public override int? PetId { get; set; }
    }


    public class InheritedDtoService : Service
    {
        public Pet POST(PetsPOSTRequest request)
        {
            return request;
        }
    }
}
