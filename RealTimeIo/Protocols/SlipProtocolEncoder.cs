using RealTimeIo.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeIo.Protocols
{
    public class SlipProtocolEncoder : IProtocolEncoder
    {


        private const byte END = 0xC0;
        private const byte ESC = 0xDB;
        private const byte ESC_END = 0xDC;
        private const byte ESC_ESC = 0xDD;

        public SlipProtocolEncoder(CrcMode crcMode = CrcMode.Sum8)
        {
            CrcMode = crcMode;
        }

        public string Name => "SLIP";

        public CrcMode CrcMode { get; }

        public byte[] Encode(ReadOnlySpan<byte> payload)
        {

            // Считаем CRC по выбранному режиму
            int crcLen = CrcUtils.ComputeCrc(payload, CrcMode, out var crc8, out var crc16);

            // Оцениваем worst case: каждый байт может экранироваться.
            // payload + crc + 2 END.
            int maxDataLen = payload.Length + crcLen;
            int worstSize = 2 + maxDataLen * 2; // начало END, конец END, каждый байт — 2 байта максимум


            var buffer = new byte[worstSize];
            int index = 0;

            // Начальный END
            buffer[index++] = END;

            // Кодируем payload
            for (int i = 0; i < payload.Length; i++)
            {
                EncodeByte(payload[i], buffer, ref index);
            }

            // Добавляем CRC (если есть)
            if (CrcMode != CrcMode.None)
            {
                if (crcLen == 1)
                {
                    EncodeByte(crc8, buffer, ref index);
                }
                else if (crcLen == 2)
                {
                    // Little-endian: low, high
                    byte lo = (byte)(crc16 & 0xFF);
                    byte hi = (byte)((crc16 >> 8) & 0xFF);
                    EncodeByte(lo, buffer, ref index);
                    EncodeByte(hi, buffer, ref index);
                }
            }

            // Конечный END
            buffer[index++] = END;

            // Обрезаем до реального размера
            if (index == buffer.Length)
                return buffer;

            var result = new byte[index];
            Array.Copy(buffer, result, index);
            return result;
        }

        private static void EncodeByte(byte b, byte[] buffer, ref int index)
        {
            switch (b)
            {
                case END:
                    buffer[index++] = ESC;
                    buffer[index++] = ESC_END;
                    break;
                case ESC:
                    buffer[index++] = ESC;
                    buffer[index++] = ESC_ESC;
                    break;
                default:
                    buffer[index++] = b;
                    break;
            }
        }
    }
}
