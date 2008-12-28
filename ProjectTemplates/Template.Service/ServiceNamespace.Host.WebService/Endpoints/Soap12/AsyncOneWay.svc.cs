/*
// $Id: AsyncOneWay.svc.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.ServiceModel.Channels;
using Ddn.Common.Wcf;
using @ServiceModelNamespace@;

namespace @ServiceNamespace@.Host.WebService.Endpoints.Soap12
{
	public class AsyncOneWay : IOneWay
	{
		public void SendOneWay(Message msg)
		{
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			App.Instance.ExecuteXmlService(xml, ModelInfo.Instance);
		}
	}
}