/*
// $Id: WsSyncReply.svc.cs 242 2008-11-28 09:34:35Z DDNGLOBAL\Pete $
//
// Revision      : $Revision: 242 $
// Modified Date : $LastChangedDate: 2008-11-28 09:34:35 +0000 (Fri, 28 Nov 2008) $
// Modified By   : $LastChangedBy: DDNGLOBAL\Pete $
//
// (c) Copyright 2008 Digital Distribution Networks Ltd
*/

using System.IO;
using System.ServiceModel.Channels;
using System.Xml;
using Ddn.Common.Wcf;

namespace @ServiceNamespace@.Host.WebService.Endpoints.Soap12
{
	public class SyncReply : ISyncReply
	{
		public Message Send(Message msg)
		{
			string action = msg.Headers.Action;
			string xml = msg.GetReaderAtBodyContents().ReadOuterXml();

			string responseXml = App.Instance.ExecuteXmlService(xml, @ServiceModelNamespace@.ModelInfo.Instance);

			return Message.CreateMessage
				(
					MessageVersion.Default, action + "Response",
					XmlReader.Create(new StringReader(responseXml))
				);
		}
	}
}