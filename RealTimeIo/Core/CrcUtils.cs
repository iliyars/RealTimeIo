using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeIo.Core
{
    public enum CrcMode
    {
        None = 0,
        Sum8 = 1,
        Crc16Ccitt = 2,
        Crc16Modbus = 3
    }


    public static class CrcUtils
    {
        public static byte ComputeSum8(ReadOnlySpan<byte> data)
        {
            byte crc = 0;
            for (int i = 0; i < data.Length; i++)
                crc += data[i];
            return crc;
        }

        /// CRC-16/CCITT-FALSE (poly 0x1021, init 0xFFFF, no xorout)
        public static ushort ComputeCrc16Ccitt(ReadOnlySpan<byte> data, ushort initial = 0xFFFF)
        {
            ushort crc = initial;

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    else
                        crc <<= 1;
                }
            }

            return crc;
        }

        /// CRC-16/MODBUS (poly 0xA001, init 0xFFFF, little-endian при передаче)
        public static ushort ComputeCrc16Modbus(ReadOnlySpan<byte> data, ushort initial = 0xFFFF)
        {
            ushort crc = initial;

            for (int i = 0; i < data.Length; i++)
            {
                crc ^= data[i];
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((crc & 0x0001) != 0)
                        crc = (ushort)((crc >> 1) ^ 0xA001);
                    else
                        crc >>= 1;
                }
            }

            return crc;
        }

        /// Унифицированный расчёт CRC по режиму.
        public static int ComputeCrc(ReadOnlySpan<byte> data, CrcMode mode, out byte crc8, out ushort crc16)
        {
            crc8 = 0;
            crc16 = 0;

            switch (mode)
            {
                case CrcMode.None:
                    return 0;

                case CrcMode.Sum8:
                    crc8 = ComputeSum8(data);
                    return 1;

                case CrcMode.Crc16Ccitt:
                    crc16 = ComputeCrc16Ccitt(data);
                    return 2;

                case CrcMode.Crc16Modbus:
                    crc16 = ComputeCrc16Modbus(data);
                    return 2;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown CRC mode");
            }
        }

        /// Проверка CRC и выделение payload из "payload+crc".
        /// Ожидается: [payload][crc] (crc 1 байт для Sum8, 2 байта для 16-битных).
        public static Frame CheckAndBuildFrame(
            ReadOnlySpan<byte> dataWithCrc,
            CrcMode mode)
        {
            if (mode == CrcMode.None)
            {
                var payloadCopy = dataWithCrc.ToArray();
                return new Frame(payloadCopy, DateTime.UtcNow, FrameErrorFlags.None);
            }

            int crcLen = mode == CrcMode.Sum8 ? 1 : 2;
            if (dataWithCrc.Length < crcLen)
            {
                return new Frame(Array.Empty<byte>(), DateTime.UtcNow, FrameErrorFlags.DecodeError);
            }

            int payloadLen = dataWithCrc.Length - crcLen;
            var payload = dataWithCrc.Slice(0, payloadLen).ToArray();
            var crcSpan = dataWithCrc.Slice(payloadLen, crcLen);

            int _ = ComputeCrc(payload, mode, out var crc8, out var crc16);
            bool ok = false;

            if (mode == CrcMode.Sum8)
            {
                ok = crcSpan[0] == crc8;
            }
            else
            {
                // Little-endian: low, high
                ushort recv = (ushort)(crcSpan[0] | (crcSpan[1] << 8));
                ok = recv == crc16;
            }

            var flags = ok ? FrameErrorFlags.None : FrameErrorFlags.ChecksumFailed;
            return new Frame(payload, DateTime.UtcNow, flags);
        }
    }
}
