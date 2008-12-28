/*
// $Id: Get@ModelName@sPortTests.cs 474 2008-12-11 16:16:16Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 474 $
// Modified Date : $LastChangedDate: 2008-12-11 16:16:16 +0000 (Thu, 11 Dec 2008) $ 
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using @ServiceModelNamespace@.Version100.Operations.@ServiceName@;
using @ServiceModelNamespace@.Version100.Types;
using @ServiceNamespace@.Tests.Integration.Support;

namespace @ServiceNamespace@.Tests.Integration.ServiceInterface.Version100
{
	[TestFixture]
	public class Get@ModelName@sPublicProfilePortTests : BaseIntegrationTest
	{
		[Test]
		public void Get@ModelName@sPublicProfilePortTest()
		{
			var requestDto = new Get@ModelName@sPublicProfile { @ModelName@Ids = new ArrayOfIntId(base.@ModelName@Ids) };

			var responseDto = (Get@ModelName@sPublicProfileResponse)base.ExecuteService(requestDto);

			Assert.That(responseDto.PublicProfiles.Count, Is.EqualTo(base.@ModelName@Ids.Count));
		}
	}
}