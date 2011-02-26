using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack;
using ServiceStack.Common;
using ServiceStack.Common.Extensions;
using ServiceStack.Common.Utils;
using ServiceStack.Redis;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ConsoleApplication1
{
	class Program
	{
		static void Main(string[] args)
		{
			//load 
			//string redisConnectIp = "192.168.2.33";
			string redisConnectIp = "127.0.0.1";

			IList<ProductCatalog> products1;
			Shop shop1;
			IList<ProductCatalog> products2;
			Shop shop2;

			var productsToGet = new List<string> { "Product 1", "Product 2" };
			int shopId = 1;

			var products = productsToGet.ConvertAll(x => new ProductCatalog { Name = x });
			var shop = new Shop { Id = shopId, Name = "Shop 1" };

			using (var redisClient = new RedisClient(redisConnectIp))
			{
				//SERIAL CALLS TO REDIS:: Notice the shorter alias of '.As<T>' == '.GetTypedClient<T>'
				IRedisTypedClient<ProductCatalog> catalogdata1 = redisClient.As<ProductCatalog>();
				IRedisTypedClient<Shop> shops1 = redisClient.As<Shop>();

				//serial, works OK ==> Well no, actually it wont. 
				//Whenever you call GetById, you either need to have an 'Id' property or specify what the Id property is with:
				ModelConfig<ProductCatalog>.Id(x => x.Name); //Setting Name as the Id property. Only need to do so once, statically i.e. on app load.

				//Add some data, you can use these generic methods off redisClient instead of 'redisClient.As<T>'
				redisClient.StoreAll(products);
				redisClient.Store(shop);

				products1 = catalogdata1.GetByIds(productsToGet);
				shop1 = shops1.GetById(shopId); //No Id configuration for <Shop> since it has Id property

				Console.WriteLine("products1: " + products1.Dump());
				Console.WriteLine("shop1: " + shop1.Dump());

				//now rewrite serial to parallel
				IEnumerable<string> jsonProducts = null;
				string jsonShop = null;

				//PARALLEL SINGLE CALL TO REDIS
				using (IRedisTransaction trans = redisClient.CreateTransaction())
				{
					IRedisTypedClient<ProductCatalog> catalogdata2 = redisClient.As<ProductCatalog>();
					IRedisTypedClient<Shop> shops2 = redisClient.As<Shop>();


					//Note: as you've quite rightly noticed there is a lack of a generic overload 
					//On redisClient.CreateTransaction() so you need to access it as a raw json string
					var productKeys = productsToGet.ConvertAll(IdUtils.CreateUrn<ProductCatalog>);
					var shopKey = IdUtils.CreateUrn<Shop>(shopId);

					trans.QueueCommand(r => r.GetValues(productKeys), x => jsonProducts = x);
					trans.QueueCommand(r => r.GetValue(shopKey), x => jsonShop = x);

					trans.Commit();
				}

				products2 = jsonProducts.ConvertAll(x => JsonSerializer.DeserializeFromString<ProductCatalog>(x));
				shop2 = JsonSerializer.DeserializeFromString<Shop>(jsonShop);

				Console.WriteLine("products2: " + products2.Dump());
				Console.WriteLine("shop2: " + shop2.Dump());
			}

			Console.ReadKey();
		}

		public class Shop
		{
			public int Id { get; set; }

			public string Name { get; set; }
		}

		public class ProductCatalog
		{
			public string Name { get; set; }
		}
	}
}