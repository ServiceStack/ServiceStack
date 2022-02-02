namespace Xilium.CefGlue
{
    /// <summary>
    /// Device information for a MediaSink object.
    /// </summary>
    public readonly struct CefMediaSinkDeviceInfo
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private readonly string _modelName;

        public CefMediaSinkDeviceInfo(string ipAddress, int port, string modelName)
        {
            _ipAddress = ipAddress;
            _port = port;
            _modelName = modelName;
        }

        public readonly string IPAddress => _ipAddress;

        public readonly int Port => _port;

        public readonly string ModelName => _modelName;
    }
}
