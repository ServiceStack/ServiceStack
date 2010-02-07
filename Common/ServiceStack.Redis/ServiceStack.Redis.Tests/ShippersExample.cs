//
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2010 Liquidbit Ltd.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Linq;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Common.Text;
using ServiceStack.Redis.Generic;

namespace ServiceStack.Redis.Tests
{
	[TestFixture]
	public class ShippersExample
	{

		static void Dump<T>(string message, T entity)
		{
			var text = TypeSerializer.SerializeToString(entity);

			//make it a little easier on the eyes
			var prettyLines = text.Split(new[] { "[", "},{", "]" },
				StringSplitOptions.RemoveEmptyEntries)
				.ToList().ConvertAll(x => x.Replace("{", "").Replace("}", ""));

			Console.WriteLine("\n" + message);
			prettyLines.ForEach(Console.WriteLine);
		}

		[Test]
		public void Shippers_UseCase()
		{
			using (var redisClient = new RedisClient())
			{
				//Create a 'strongly-typed' API that makes all Redis Value operations to apply against Shippers
				IRedisTypedClient<Shipper> redis = redisClient.GetTypedClient<Shipper>();

				//Redis lists implement IList<T> while Redis sets implement ICollection<T>
				var currentShippers = redis.Lists["urn:shippers:current"];
				var prospectiveShippers = redis.Lists["urn:shippers:prospective"];

				currentShippers.Add(
					new Shipper {
						Id = redis.GetNextSequence(),
						CompanyName = "Trains R Us",
						DateCreated = DateTime.UtcNow,
						ShipperType = ShipperType.Trains,
						UniqueRef = Guid.NewGuid()
					});

				currentShippers.Add(
					new Shipper {
						Id = redis.GetNextSequence(),
						CompanyName = "Planes R Us",
						DateCreated = DateTime.UtcNow,
						ShipperType = ShipperType.Planes,
						UniqueRef = Guid.NewGuid()
					});

				var lameShipper = new Shipper {
					Id = redis.GetNextSequence(),
					CompanyName = "We do everything!",
					DateCreated = DateTime.UtcNow,
					ShipperType = ShipperType.All,
					UniqueRef = Guid.NewGuid()
				};

				currentShippers.Add(lameShipper);

				Dump("ADDED 3 SHIPPERS:", currentShippers);

				currentShippers.Remove(lameShipper);

				Dump("REMOVED 1:", currentShippers);

				prospectiveShippers.Add(
					new Shipper {
						Id = redis.GetNextSequence(),
						CompanyName = "Trucks R Us",
						DateCreated = DateTime.UtcNow,
						ShipperType = ShipperType.Automobiles,
						UniqueRef = Guid.NewGuid()
					});

				Dump("ADDED A PROSPECTIVE SHIPPER:", prospectiveShippers);

				redis.PopAndPushBetweenLists(prospectiveShippers, currentShippers);

				Dump("CURRENT SHIPPERS AFTER POP n' PUSH:", currentShippers);
				Dump("PROSPECTIVE SHIPPERS AFTER POP n' PUSH:", prospectiveShippers);

				var poppedShipper = redis.PopFromList(currentShippers);
				Dump("POPPED a SHIPPER:", poppedShipper);
				Dump("CURRENT SHIPPERS AFTER POP:", currentShippers);

				//reset sequence and delete all lists
				redis.SetSequence(0);
				redis.Remove(currentShippers, prospectiveShippers);
				Dump("DELETING CURRENT AND PROSPECTIVE SHIPPERS:", currentShippers);
			}

		}

	}
}