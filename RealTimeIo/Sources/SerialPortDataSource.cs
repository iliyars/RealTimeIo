using RealTimeIo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Runtime.InteropServices;
using System.Buffers;

namespace RealTimeIo.Sources
{
    public class SerialPortDataSource : IDataSource
    {
        private readonly SerialPort _port;
        private readonly Channel<byte[]> _channel;
        private readonly int _bufferSize;
        private readonly CancellationTokenSource _cts = new();
        private Task? _readTask;

        public ChannelReader<byte[]> Bloks => _channel.Reader;

        public SerialPortDataSource(SerialPort port, int bufferSize = 256, int channelCapacity = 64)
        {
            _port = port;
            _bufferSize = bufferSize;

            _channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(channelCapacity)
            {
                SingleWriter = true,
                SingleReader = false,
                FullMode = BoundedChannelFullMode.Wait
            });
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _cts.Cancel();
            if (_readTask != null)
                await _readTask;

            if (_port.IsOpen)
                try
                {
                    _port.Close();        
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Ошибка закрытия порта: {ex.Message}");        
                }
            }catch(Exception ex)
            {
                Console.Error.WriteLine($"Ошибка при закрытии порта: {ex.Message}");
            }
            finally
            {
                _cts.Dispose();
            }
            
        }

        public void Start()
        {
            if (_readTask != null)
                throw new InvalidOperationException("Already started");

            if(!_port.IsOpen)
                _port.Open();

            _readTask = Task.Run(ReadLoopAsync);
        }

        private async Task ReadLoopAsync()
        {
            var token = _cts.Token;
            var stream = _port.BaseStream;

            try
            {
                while(!token.IsCancellationRequested)
                {
                    var buffer = ArrayPool<byte>.Shared.Rent(_bufferSize);
                    try
                    {
                        int read = await stream.ReadAsync(buffer.AsMemory(0, _bufferSize), token);
                        if(read <= 0)
                            break;

                        if(read < _bufferSize)
                        {
                                // Обрезаем существующий буфер
                            var trimmed = new byte[read];
                            Array.Copy(buffer, trimmed, read);
                            buffer = trimmed;
                        }

                        if(!await _channel.Writer.WaitToWriteAsync(token))
                            break;

                        await _channel.Writer.WriteAsync(buffer, token);
                    }
                    catch(Exception ex)
                    {
                        Console.Error.WriteLine($"Ошибка чтения из порта: {ex.Message}");
                        ArrayPool<byte>.Shared.Return(buffer);
                        throw;
                    }
                    finally
                    {
                        if(buffer is byte[] rentedBuffer && rentedBuffer.Length == _bufferSize)
                            ArrayPool<byte>.Shared.Return(rentedBuffer);
                    }
                }
            }
            catch(OperationCanceledException)
            {
                
            }
            finally
            {
                _channel.Writer.TryComplete();
            }
        }

        public void Stop()
        {
            _cts.Cancel();
        }
    }
}
