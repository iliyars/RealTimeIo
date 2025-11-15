using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeIo.Core
{
    [Flags]
    public enum FrameErrorFlags
    {
        None = 0,
        ChecksumFailed  = 1 << 0,
        DecodeError    = 1 << 1,
        RxOverflow      = 1 << 2,
        TxError         = 1 << 3
    }


    public readonly struct Frame
    {
        /// <summary>
        /// 
        /// </summary>
        public byte[] Payload { get; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Timestamp { get; }

        public FrameErrorFlags ErrorFlags { get; }

        public Frame(byte[] payload, DateTime timestamp, FrameErrorFlags errorFlags)
        {
            Payload = payload ?? throw new ArgumentNullException(nameof(payload));
            Timestamp = timestamp;
            ErrorFlags = errorFlags;
        }
    }
}
