using Amazon.DynamoDBv2;
using Funq;
using ServiceStack.Aws.DynamoDb;

namespace ServiceStack.WebHost.Endpoints.Tests
{
    public class AutoQueryDataDynamoTests : AutoQueryDataTests
    {
        public override ServiceStackHost CreateAppHost()
        {
            return new AutoQueryDataDynamoAppHost();
        }
    }

    public class AutoQueryDataDynamoAppHost : AutoQueryDataAppHost
    {
        public override void Configure(Container container)
        {
            base.Configure(container);

            container.Register(c => new PocoDynamo(
                new AmazonDynamoDBClient("keyId", "key", new AmazonDynamoDBConfig
                {
                    ServiceURL = "http://localhost:8000",
                }))
                .RegisterTable<Rockstar>()
                .RegisterTable<RockstarAlbum>()
                .RegisterTable<Adhoc>()
                .RegisterTable<Movie>()
                .RegisterTable<AllFields>()
                .RegisterTable<PagingTest>()
            );

            var dynamo = container.Resolve<IPocoDynamo>();
            //dynamo.DeleteAllTables();
            dynamo.InitSchema();
            dynamo.PutItems(SeedRockstars);
            dynamo.PutItems(SeedAlbums);
            dynamo.PutItems(SeedAdhoc);
            dynamo.PutItems(SeedMovies);
            dynamo.PutItems(SeedAllFields);
            dynamo.PutItems(SeedPagingTest);

            var feature = this.GetPlugin<AutoQueryDataFeature>();
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Rockstar>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<RockstarAlbum>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Adhoc>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<Movie>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<AllFields>());
            feature.AddDataSource(ctx => ctx.DynamoDbSource<PagingTest>());
        }
    }
}