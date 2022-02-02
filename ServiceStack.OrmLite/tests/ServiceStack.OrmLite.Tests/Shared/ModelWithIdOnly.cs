namespace ServiceStack.Common.Tests.Models
{
    public class ModelWithIdOnly
    {
        public ModelWithIdOnly()
        {
        }

        public ModelWithIdOnly(long id)
        {
            Id = id;
        }

        // must be long as you cannot have a table with only an autoincrement field
        public long Id { get; set; }

    }
}