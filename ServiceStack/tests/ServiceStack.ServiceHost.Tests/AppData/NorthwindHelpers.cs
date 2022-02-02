using System.Collections.Generic;
using System.Linq;

namespace ServiceStack.ServiceHost.Tests.AppData
{
    public class NorthwindHelpers
    {
        public string OrderTotal(List<OrderDetail> orderDetails)
        {
            var total = 0m;
            if (!orderDetails.IsEmpty())
                total += orderDetails.Sum(item => item.Quantity * item.UnitPrice);

            return FormatHelpers.Instance.Money(total);
        }

        public string CustomerOrderTotal(List<CustomerOrder> customerOrders)
        {
            var total = customerOrders
                .Sum(x =>
                    x.OrderDetails.Sum(item => item.Quantity * item.UnitPrice));

            return FormatHelpers.Instance.Money(total);
        }
    }
}