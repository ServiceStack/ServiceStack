using System.ComponentModel;
using ServiceStack;

namespace Check.ServiceModel
{
    public class Rockstar
    {
        [Description("Идентификатор")]
        public int Id { get; set; }
        [Description("Фамилия")]
        public string FirstName { get; set; }
        [Description("Имя")]
        public string LastName { get; set; }
        [Description("Возраст")]
        public int? Age { get; set; }
    }
}
