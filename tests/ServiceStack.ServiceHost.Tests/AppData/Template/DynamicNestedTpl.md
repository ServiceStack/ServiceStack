# @model.FirstName Dynamic Nested Markdown Template

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

### heading 3