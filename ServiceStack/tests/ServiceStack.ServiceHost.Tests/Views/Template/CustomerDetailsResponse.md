@model ServiceStack.ServiceHost.Tests.AppData.CustomerDetailsResponse
@helper Fmt: ServiceStack.ServiceHost.Tests.AppData.FormatHelpers
@helper Nwnd: ServiceStack.ServiceHost.Tests.AppData.NorthwindHelpers

@var customer = Model.Customer

# @customer.ContactName Customer Details (@customer.City, @customer.Country)
### @customer.ContactTitle 

  - **Company Name:** @customer.CompanyName
  - **Address:** @customer.Address
  - **Email:** @customer.Email

## Customer Orders

<table><thead>
  <tr><th>Id</th><th>Order Date</th><th>Freight Cost</th><th>Order Total</th></tr>
</thead>
<tbody>

@foreach (var customerOrder in Model.CustomerOrders) {
@var order = customerOrder.Order

<tr>
  <td>@order.Id</td>
  <td>@Fmt.ShortDate(order.OrderDate)</td>
  <td>@Fmt.Money(order.Freight)</td>
  <td>@Nwnd.OrderTotal(customerOrder.OrderDetails)</td>
</tr>
}

</tbody></table>

### Customer Orders Total: @Nwnd.CustomerOrderTotal(Model.CustomerOrders)
