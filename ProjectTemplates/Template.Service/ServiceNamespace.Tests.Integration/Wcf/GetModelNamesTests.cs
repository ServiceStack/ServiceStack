/*
// $Id: Get@ModelName@sTests.cs 692 2008-12-23 14:11:58Z DDNGLOBAL\Demis $
//
// Revision      : $Revision: 692 $
// Modified Date : $LastChangedDate: 2008-12-23 14:11:58 +0000 (Tue, 23 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Demis $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.ServiceModel.Channels;
using Ddn.Common.Wcf;
using NUnit.Framework;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.Wcf
{
	[Ignore]
	[TestFixture]
	public class GetSeriesPortTests : BaseIntegrationTest
	{
		[Test]
		public void Wcf_get_series_by_series_ids()
		{
			using (var wcfClient = new Soap12ServiceClient(TestUtils.WcfSyncReplyUri))
			{
				var request = new Get@ModelName@s { Ids = new ArrayOfIntId(base.@ModelName@Ids) };
				Message response = wcfClient.Send(request);
				var dto = response.GetBody<Get@ModelName@sResponse>();
				Assert.IsNotNull(response);
				Assert.AreEqual(base.@ModelName@Ids.Count, dto.@ModelName@s.Count);
			}
	}	
	}
}