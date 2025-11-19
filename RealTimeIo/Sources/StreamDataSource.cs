using RealTimeIo.Core;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RealTimeIo.Sources
{
    public sealed class StreamDataSource : IDataSource
    {

        private readonly Stream _stream;
        private readonly Channel<byte[]> _channel;
        private readonly int _bufferSize;
        private readonly CancellationTokenSource _cts = new();
        private Task? _readTask;


        public StreamDataSource(Stream stream, int bufferSize = 4096, int chanelCapacity = 64)
        {
            _stream = stream;
            _bufferSize = bufferSize;
            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(chanelCapacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.DropOldest
            });
        }


        public void Start()
        {
            if (_readTask != null)
            {
                throw new InvalidOperationException("Already started");
            }

            _readTask = Task.Run(ReadLoopAsync);
        }

        private async Task ReadLoopAsync()
        {
            var token = _cts.Token;

            try
            {
                while(!token.IsCancellationRequested)
                {
                    //todo: new byte[] заменить на ArrayPool<byte>
                    var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                    try
                    {
                        int read = await _stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token);

                        if(read <= 0)
                            break;

                        if(read < _bufferSize)
                        {
                            ArrayPool<byte>.Shared.Return(buffer);

                            buffer = new byte[read];
                            int newRead = await _stream.ReadAsync(buffer, 0, read, token);
                            if(newRead != read)
                                throw new IOException("Unexpected end of stream");
                        }
                        if (!await _channel.Writer.WaitToWriteAsync(token))
                            break;

                        await _channel.Writer.WriteAsync(buffer,token);
                    }
                    catch
                    {
                        ArrayPool<byte>.Shared.Return(buffer);
                        throw;
                    }
                }
            }catch(OperationCanceledException)
            {

            }
            finally
            {
                _channel.Writer.TryComplete();
            }
        }

        public void Stop() => _cts.Cancel();

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            if (_readTask != null)
                await _readTask;
            _cts.Dispose();
        }

        public ChannelReader<byte[]> Bloks => _channel;
    }
}
