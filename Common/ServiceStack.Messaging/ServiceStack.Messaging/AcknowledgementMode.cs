using System;

namespace ServiceStack.Messaging
{
    public enum AcknowledgementMode
    {
        AutoAcknowledge,
        ClientAcknowledge,
        DuplicatesOkAcknowledge,
        Transactional
    }
}
