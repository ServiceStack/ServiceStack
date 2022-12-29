//
// ServiceStack.Redis: ECMA CLI Binding to the Redis key-value storage system
//
// Authors:
//   Demis Bellot (demis.bellot@gmail.com)
//
// Copyright 2013 Service Stack LLC. All Rights Reserved.
//
// Licensed under the same terms of reddis and ServiceStack: new BSD license.
//

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using ServiceStack.Common.Tests.Models;
using ServiceStack.Redis.Generic;
using ServiceStack.Text;

namespace ServiceStack.Redis.Tests
{
    [TestFixture, Category("Async")]
    public class ShippersExampleAsync
    {

        public class Shipper
        {
            public long Id { get; set; }
            public string CompanyName { get; set; }
            public DateTime DateCreated { get; set; }
            public ShipperType ShipperType { get; set; }
            public Guid UniqueRef { get; set; }
        }

        static void Dump<T>(string message, T entity)
        {
            var text = TypeSerializer.SerializeToString(entity);

            //make it a little easier on the eyes
            var prettyLines = text.Split(new[] { "[", "},{", "]" },
                StringSplitOptions.RemoveEmptyEntries)
                .ToList().ConvertAll(x => x.Replace("{", "").Replace("}", ""));

            Debug.WriteLine("\n" + message);
            foreach (var l in prettyLines) Debug.WriteLine(l);
        }

        [Test]
        public async Task Shippers_UseCase()
        {
            await using var redisClient = new RedisClient(TestConfig.SingleHost).ForAsyncOnly();
            //Create a 'strongly-typed' API that makes all Redis Value operations to apply against Shippers
            IRedisTypedClientAsync<Shipper> redis = redisClient.As<Shipper>();

            //Redis lists implement IList<T> while Redis sets implement ICollection<T>
            var currentShippers = redis.Lists["urn:shippers:current"];
            var prospectiveShippers = redis.Lists["urn:shippers:prospective"];

            await currentShippers.AddAsync(
                new Shipper
                {
                    Id = await redis.GetNextSequenceAsync(),
                    CompanyName = "Trains R Us",
                    DateCreated = DateTime.UtcNow,
                    ShipperType = ShipperType.Trains,
                    UniqueRef = Guid.NewGuid()
                });

            await currentShippers.AddAsync(
                new Shipper
                {
                    Id = await redis.GetNextSequenceAsync(),
                    CompanyName = "Planes R Us",
                    DateCreated = DateTime.UtcNow,
                    ShipperType = ShipperType.Planes,
                    UniqueRef = Guid.NewGuid()
                });

            var lameShipper = new Shipper
            {
                Id = await redis.GetNextSequenceAsync(),
                CompanyName = "We do everything!",
                DateCreated = DateTime.UtcNow,
                ShipperType = ShipperType.All,
                UniqueRef = Guid.NewGuid()
            };

            await currentShippers.AddAsync(lameShipper);

            Dump("ADDED 3 SHIPPERS:", await currentShippers.ToListAsync());

            await currentShippers.RemoveAsync(lameShipper);

            Dump("REMOVED 1:", await currentShippers.ToListAsync());

            await prospectiveShippers.AddAsync(
                new Shipper
                {
                    Id = await redis.GetNextSequenceAsync(),
                    CompanyName = "Trucks R Us",
                    DateCreated = DateTime.UtcNow,
                    ShipperType = ShipperType.Automobiles,
                    UniqueRef = Guid.NewGuid()
                });

            Dump("ADDED A PROSPECTIVE SHIPPER:", await prospectiveShippers.ToListAsync());

            await redis.PopAndPushItemBetweenListsAsync(prospectiveShippers, currentShippers);

            Dump("CURRENT SHIPPERS AFTER POP n' PUSH:", await currentShippers.ToListAsync());
            Dump("PROSPECTIVE SHIPPERS AFTER POP n' PUSH:", await prospectiveShippers.ToListAsync());

            var poppedShipper = await redis.PopItemFromListAsync(currentShippers);
            Dump("POPPED a SHIPPER:", poppedShipper);
            Dump("CURRENT SHIPPERS AFTER POP:", await currentShippers.ToListAsync());

            //reset sequence and delete all lists
            await redis.SetSequenceAsync(0);
            await redis.RemoveEntryAsync(new[] { currentShippers, prospectiveShippers });
            Dump("DELETING CURRENT AND PROSPECTIVE SHIPPERS:", await currentShippers.ToListAsync());

        }

    }
}
