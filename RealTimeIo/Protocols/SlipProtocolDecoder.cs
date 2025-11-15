using RealTimeIo.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RealTimeIo.Protocols
{
    public sealed class SlipProtocolDecoder : IProtocolDecoder
    {
        public string Name => "SLIP";

        private const byte END = 0xC0;
        private const byte ESC = 0xDB;
        private const byte ESC_END = 0xDC;
        private const byte ESC_ESC = 0xDD;

        public SlipProtocolDecoder(CrcMode crcMode)
        {
            CrcMode = crcMode;
        }


        public CrcMode CrcMode { get; }
        public async IAsyncEnumerable<Frame> DecodeAsync(
       ChannelReader<byte[]> source,
       [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var buffer = new List<byte>(256);
            bool escape = false;

            while (await source.WaitToReadAsync(cancellationToken))
            {
                while (source.TryRead(out var block))
                {

                    for (int i = 0; i < block.Length; i++)
                    {
                        byte b = block[i];

                        if (b == END)
                        {
                            if (buffer.Count > 0)
                            {
                                // У нас есть потенциальный кадр: data + crc (зависит от CrcMode)
                                var frame = BuildFrameWithCrc(buffer, CrcMode);
                                yield return frame;
                                buffer.Clear();
                            }

                            escape = false;
                            continue;
                        }

                        if (b == ESC)
                        {
                            escape = true;
                            continue;
                        }

                        if (escape)
                        {
                            if (b == ESC_END) b = END;
                            else if (b == ESC_ESC) b = ESC;
                            else
                            {
                                // Некорректная ESC-последовательность
                                yield return new Frame(
                                    Array.Empty<byte>(),
                                    DateTime.UtcNow,
                                    FrameErrorFlags.DecodeError);

                                // сбрасываем текущий буфер (по желанию)
                                buffer.Clear();
                            }

                            escape = false;
                        }

                        buffer.Add(b);
                    }
                }
            }
        }

        private static Frame BuildFrameWithCrc(List<byte> data, CrcMode crcMode)
        {
            if (data.Count == 0)
            {
                return new Frame(Array.Empty<byte>(), DateTime.UtcNow, FrameErrorFlags.DecodeError);
            }

            // Используем универсальный помощник
            byte[] arr = data.ToArray();
            return CrcUtils.CheckAndBuildFrame(arr, crcMode);
        }



        }
    }
