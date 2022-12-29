using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceStack.Web;

public interface IConvertRequest
{
    T Convert<T>(T value);
}
