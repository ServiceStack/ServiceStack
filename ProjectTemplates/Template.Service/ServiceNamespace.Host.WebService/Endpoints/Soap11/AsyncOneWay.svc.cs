/*
// $Id: WsSyncReply.svc.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.ServiceModel.Channels;
using Ddn.Common.Wcf;

namespace @ServiceNamespace@.Host.WebService.Endpoints.Soap11
{
	public class AsyncOneWay : IOneWay
	{
		public void SendOneWay(Message msg)
		{
			var xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			App.Instance.ExecuteXmlService(xml, @ServiceModelNamespace@.ModelInfo.Instance);
		}
	}
}