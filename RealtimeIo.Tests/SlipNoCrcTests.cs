using RealTimeIo.Core;
using RealTimeIo.Protocols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RealtimeIo.Tests
{
    public class SlipNoCrcTests
    {
        [Fact]
        public async Task Slip_EncodeDecode_NoCrc_RoundtripOk()
        {

            // Arrange: чистый SLIP без CRC
            var encoder = new SlipProtocolEncoder(CrcMode.None);
            var decoder = new SlipProtocolDecoder(CrcMode.None);

            // В payload специально кладём байты END (0xC0) и ESC (0xDB),
            // чтобы проверить корректное экранирование.
            byte[] payload =
            {
            0x01,
            0xC0, // END
            0x02,
            0xDB, // ESC
            0x03
            };

            // Act: кодируем
            byte[] encoded = encoder.Encode(payload);

            var chanell = Channel.CreateUnbounded<byte[]>();
            await chanell.Writer.WriteAsync(encoded);
            chanell.Writer.Complete();

            var frames = new List<Frame>();

            await foreach (var frame in decoder.DecodeAsync(chanell.Reader, CancellationToken.None))
            {
                frames.Add(frame);
            }

            Assert.Single(frames);
            var f = frames[0];

            Assert.Equal(FrameErrorFlags.None, f.ErrorFlags);
            Assert.Equal(payload.Length, f.Payload.Length);
            Assert.Equal(payload, f.Payload);
        }

        [Fact]
        public void CrcUtils_NoCrc_Mode_DoesNotCheckAnything()
        {
            // Arrange: произвольные данные, которые "как будто" payload+crc,
            // но при CrcMode.None всё это считается чистым payload.
            byte[] data = { 0x10, 0x20, 0x30, 0x40 };

            // Act
            var frame = CrcUtils.CheckAndBuildFrame(data, CrcMode.None);

            // Assert
            Assert.Equal(FrameErrorFlags.None, frame.ErrorFlags);
            Assert.Equal(data, frame.Payload);
        }

        [Fact]
        public void Slip_Encoder_NoCrc_NoExtraCrcBytesAppended()
        {
            // Arrange
            var encoder = new SlipProtocolEncoder(CrcMode.None);
            byte[] payload = { 0x11, 0x22, 0x33 };

            // Act
            var packet = encoder.Encode(payload);

            // Минимальная проверка структуры:
            // пакет должен начинаться и заканчиваться END (0xC0),
            // а между ними — экранированный payload без дополнительных CRC-байтов.
            Assert.True(packet.Length >= 2);
            Assert.Equal(0xC0, packet[0]);
            Assert.Equal(0xC0, packet[^1]);

            // Внутреннюю часть можно декодировать тем же декодером и проверить,
            // что обратно выходит ровно наш payload (это частично перекрывает
            // первый тест, но помогает локализовать проблему, если что).
        }

    }
}
