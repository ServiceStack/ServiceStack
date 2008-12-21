using System;

namespace ServiceStack.Messaging.ActiveMq.Support.Converters
{
    public class AcknowledgementModeConverter  
    {
        public static AcknowledgementMode Parse(NMS.AcknowledgementMode acknowledgementMode)
        {
            switch (acknowledgementMode)
            {
                case NMS.AcknowledgementMode.AutoAcknowledge:
                    return AcknowledgementMode.AutoAcknowledge;
                case NMS.AcknowledgementMode.ClientAcknowledge:
                    return AcknowledgementMode.ClientAcknowledge;
                case NMS.AcknowledgementMode.DupsOkAcknowledge:
                    return AcknowledgementMode.DuplicatesOkAcknowledge;
                case NMS.AcknowledgementMode.Transactional:
                    return AcknowledgementMode.Transactional;
                default:
                    throw new NotSupportedException(acknowledgementMode.ToString());
            }
        }

        public static NMS.AcknowledgementMode ToNmsAcknowledgementMode(AcknowledgementMode acknowledgementMode)
        {
            switch (acknowledgementMode)
            {
                case AcknowledgementMode.AutoAcknowledge:
                    return NMS.AcknowledgementMode.AutoAcknowledge;
                case AcknowledgementMode.ClientAcknowledge:
                    return NMS.AcknowledgementMode.ClientAcknowledge;
                case AcknowledgementMode.DuplicatesOkAcknowledge:
                    return NMS.AcknowledgementMode.DupsOkAcknowledge;
                case AcknowledgementMode.Transactional:
                    return NMS.AcknowledgementMode.Transactional;
                default:
                    throw new NotSupportedException(acknowledgementMode.ToString());
            }
        }
    }
}