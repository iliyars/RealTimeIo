using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace RealTimeIo.Core
{
    public interface IDataSource : IAsyncDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        ChannelReader<byte[]> Bloks { get; }

        void Start();

        void Stop();




    }
}
