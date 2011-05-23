# @model.FirstName Dynamic Markdown Template

Hello @model.FirstName,

  * @model.LastName
  * @model.FirstName

# heading 1

@foreach (var link in model.Links) {
  @if (link.Name == "AjaxStack") {
  - @link.Name - @link.Href
  }
}

@if (model.Links.Count == 2) {
## Haz 2 links
  @foreach (var link in model.Links) {
  - @link.Name - @link.Href 
    @foreach (var label in link.Labels) { 
	- @label 
	}
  }
}

## heading 2

This is a [servicestack.net link](http://www.servicestack.net)

### heading 3

