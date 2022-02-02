using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.OpenApi.Tests.Services
{
    public class InfoUpdateResponseDto
    {
        public string Result { get; set; }
    }

    [Route("/UpdateInfo/{Id}", "PUT", Summary = "Updates info.")]
    public class UpdateInfoReq : IReturn<InfoUpdateResponseDto>
    {
        [ApiMember(IsRequired = true)]
        public Guid Id { get; set; }
    }


    public class AsyncService : Service
    {
        public async Task<InfoUpdateResponseDto> Update(UpdateInfoReq query)
        {
            return new InfoUpdateResponseDto {Result = "Hello"};
        }
    }
}
