using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeIo.Core
{
    public interface IProtocolEncoder
    {
        string Name { get; }

        byte[] Encode(ReadOnlySpan<byte> payload);


    }
}
