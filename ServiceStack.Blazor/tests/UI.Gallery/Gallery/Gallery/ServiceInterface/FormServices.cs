using ServiceStack;
using MyApp.ServiceModel;

namespace MyApp.ServiceInterface;

public class FormServices : Service
{
    public object Any(ComboBoxExamples request) => request;
}