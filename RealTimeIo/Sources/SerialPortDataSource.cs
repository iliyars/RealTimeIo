using RealTimeIo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.IO.Ports;

namespace RealTimeIo.Sources
{
    public class SerialPortDataSource : IDataSource
    {
        private readonly SerialPort _port;
        private readonly Channel<byte[]> _channel;
        private readonly int _bufferSize;
        private readonly CancellationTokenSource _cts = new();
        private Task? _readTask;


        public ChannelReader<byte[]> Bloks => throw new NotImplementedException();

        public ValueTask DisposeAsync()
        {
            throw new NotImplementedException();
        }

        public void Start()
        {
            throw new NotImplementedException();
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
