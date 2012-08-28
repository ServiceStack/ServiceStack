@model ServiceStack.ServiceHost.Tests.AppData.Customer
@template alt-template
@helper Fmt: ServiceStack.ServiceHost.Tests.AppData.FormatHelpers
@helper Nwnd: ServiceStack.ServiceHost.Tests.AppData.NorthwindHelpers

@var customer = Model

# @customer.ContactName Customer Details (@customer.City, @customer.Country)
### @customer.ContactTitle 

  - **Company Name:** @customer.CompanyName
  - **Address:** @customer.Address
  - **Email:** @customer.Email
