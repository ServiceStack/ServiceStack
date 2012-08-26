@model RockstarsResponse
@Layout HtmlReport

@var Title = Model.Aged.HasValue ? Model.Aged + "year old rockstars" : "All Rockstars"

^<div style="float:right">

**view this page in: **
[json](?format=json),
[xml](?format=xml),
[jsv](?format=jsv),
[csv](?format=csv)

### Other Pages

  - [/rockstars](/rockstars)
  - [/TypedModelNoController](/TypedModelNoController)
  - [/NoModelNoController](/NoModelNoController)

^</div>

### We have @Model.Total Rockstars, showing @Title

@foreach rockstar in Model.Results {
  - (@rockstar.Age.Value) @rockstar.FirstName @rockstar.LastName [delete](/rockstars/delete/@rockstar.Id)
}

<!--view:RockstarsMark.md-->