using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RealTimeIo.Core
{
    public interface IProtocolDecoder
    {

        string Name { get; }

        IAsyncEnumerable<Frame> DecodeAsync(ChannelReader<byte[]> source, CancellationToken cancellationToken = default);
    }
}
