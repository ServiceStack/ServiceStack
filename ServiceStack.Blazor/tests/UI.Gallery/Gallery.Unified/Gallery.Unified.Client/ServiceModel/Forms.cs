using ServiceStack;
using System.Collections.Generic;

namespace MyApp.ServiceModel;

[Tag("Forms")]
public class ComboBoxExamples : IReturn<ComboBoxExamples>, IPost
{
    [Input(Type = "combobox", Options = "{ allowableValues:['Alpha','Bravo','Charlie'] }")]
    public string? SingleClientValues { get; set; }

    [Input(Type = "combobox", Options = "{ allowableValues:['Alpha','Bravo','Charlie'] }", Multiple = true)]
    public List<string>? MultipleClientValues { get; set; }

    [Input(Type = "combobox", EvalAllowableValues = "['Alpha','Bravo','Charlie']")]
    public string? SingleServerValues { get; set; }

    [Input(Type = "combobox", EvalAllowableValues = "AppData.AlphaValues", Multiple = true)]
    public List<string>? MultipleServerValues { get; set; }

    [Input(Type = "combobox", EvalAllowableEntries = "{ A:'Alpha', B:'Bravo', C:'Charlie' }")]
    public string? SingleServerEntries { get; set; }

    [Input(Type = "combobox", EvalAllowableEntries = "AppData.AlphaDictionary", Multiple = true)]
    public List<string>? MultipleServerEntries { get; set; }
}
