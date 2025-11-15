using RealTimeIo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeIo.Runtime
{
    public sealed class RealtimeBus : IAsyncDisposable
    {

        private readonly IDataSource _source;
        private readonly IProtocolDecoder _decoder;
        private readonly CancellationTokenSource _cts = new();
        private Task? _decodeTask;

        public RealtimeBus(IDataSource source, IProtocolDecoder decoder)
        {
            _source = source;
            _decoder = decoder;
        }

        public event Action<Frame>? FrameRecieved;

        public void Start()
        {
            _source.Start();
            _decodeTask = Task.Run(DecodeLoopAsync);
        }
        public void Stop()
        {
            _cts.Cancel();
            _source.Stop();
        }
        private async Task DecodeLoopAsync()
        {
            var token = _cts.Token;

            try
            {
                await foreach (var frame in _decoder.DecodeAsync(_source.Bloks, token))
                {
                    FrameRecieved?.Invoke(frame);
                }
            }
            catch (OperationCanceledException)
            {

            }
        }
        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_decodeTask != null)
                await _decodeTask;
            await _source.DisposeAsync();
            _cts.Dispose();
        }
    }
}
