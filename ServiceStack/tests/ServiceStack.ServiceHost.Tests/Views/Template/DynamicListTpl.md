# @Model.FirstName Dynamic Markdown Template

Hello @Model.FirstName,

  * @Model.LastName
  * @Model.FirstName

# heading 1

@foreach (var link in Model.Links) {
  - @link.Name - @link.Href
}

## heading 2

This is a [servicestack.net link](http://www.servicestack.net)

### heading 3

