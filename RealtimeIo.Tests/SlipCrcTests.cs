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
    public class SlipCrcTests
    {
        private static async Task<List<Frame>> DecodeAllAsync(
        SlipProtocolDecoder decoder,
        byte[] encoded)
        {
            var channel = Channel.CreateUnbounded<byte[]>();
            await channel.Writer.WriteAsync(encoded);
            channel.Writer.Complete();

            var frames = new List<Frame>();

            await foreach (var frame in decoder.DecodeAsync(channel.Reader, CancellationToken.None))
            {
                frames.Add(frame);
            }

            return frames;
        }

        [Theory]
        [InlineData(CrcMode.Sum8)]
        [InlineData(CrcMode.Crc16Ccitt)]
        [InlineData(CrcMode.Crc16Modbus)]
        public async Task Slip_EncodeDecode_WithCrc_RoundtripOk(CrcMode mode)
        {
            var encoder = new SlipProtocolEncoder(mode);
            var decoder = new SlipProtocolDecoder(mode);

            byte[] payload =
            {
                0x01,
                0xC0, // END
                0xC0, // END
                0x02,
                0xDB, // ESC
                0x03,
                0x10
            };

            // Act: кодируем + декодируем

            byte[] encoded = encoder.Encode(payload);
            var frames = await DecodeAllAsync(decoder, encoded);

            // Assert
            Assert.Single(frames);
            var f = frames[0];

            Assert.Equal(FrameErrorFlags.None, f.ErrorFlags);
            Assert.Equal(payload.Length, f.Payload.Length);
            Assert.Equal(payload, f.Payload);
        }

        // --------- 2. Порченная CRC должна ловиться как ошибка ---------

        [Theory]
        [InlineData(CrcMode.Sum8)]
        [InlineData(CrcMode.Crc16Ccitt)]
        [InlineData(CrcMode.Crc16Modbus)]
        public async Task Slip_Decode_WithBadCrc_SetsChecksumFailed(CrcMode mode)
        {
            // Arrange
            var encoder = new SlipProtocolEncoder(mode);
            var decoder = new SlipProtocolDecoder(mode);

            byte[] payload = { 0x11, 0x22, 0x33, 0x44 };

            // Правильный пакет
            byte[] encoded = encoder.Encode(payload);

            // Немного портим последний байт полезных данных/CRC внутри пакета,
            // но оставляем первые и последние END (0xC0) нетронутыми.
            // encoded: [0] = 0xC0 ... [^1] = 0xC0
            if (encoded.Length <= 3)
                throw new Exception("Encoded packet too short для теста");

            var corrupted = (byte[])encoded.Clone();
            corrupted[corrupted.Length - 2] ^= 0xFF; // переворачиваем биты предпоследнего байта

            // Act
            var frames = await DecodeAllAsync(decoder, corrupted);

            // Assert: ожидаем хотя бы один кадр с флагом ChecksumFailed
            Assert.NotEmpty(frames);
            Assert.Contains(frames, f => (f.ErrorFlags & FrameErrorFlags.ChecksumFailed) != 0);
        }

        // --------- 3. Прямой тест CrcUtils для конкретных режимов ---------

        [Fact]
        public void CrcUtils_Sum8_ComputesExpectedValue()
        {
            byte[] data = { 0x01, 0x02, 0x03 };
            byte crc = CrcUtils.ComputeSum8(data);

            // 0x01 + 0x02 + 0x03 = 0x06
            Assert.Equal(0x06, crc);
        }

        [Fact]
        public void CrcUtils_Crc16Ccitt_IsStableOnKnownVector()
        {
            // Классический тестовый вектор "123456789"
            byte[] data = System.Text.Encoding.ASCII.GetBytes("123456789");
            ushort crc = CrcUtils.ComputeCrc16Ccitt(data);

            // Для CRC-16/CCITT-FALSE известное значение: 0x29B1s
            Assert.Equal(0x29B1, crc);
        }

        [Fact]
        public void CrcUtils_Crc16Modbus_IsStableOnKnownVector()
        {
            // Тот же "123456789"
            byte[] data = System.Text.Encoding.ASCII.GetBytes("123456789");
            ushort crc = CrcUtils.ComputeCrc16Modbus(data);

            // Для CRC-16/MODBUS ("123456789") известное значение: 0x4B37
            Assert.Equal(0x4B37, crc);
        }
    }
}
