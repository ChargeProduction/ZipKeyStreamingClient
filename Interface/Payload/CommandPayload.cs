using System;
using ZeroFormatter;

namespace ZipKeyStreamingClient.Interface.Payload
{
    [ZeroFormattable]
    public class CommandPayload
    {
        [Index(0)]
        public virtual String Command { get; set; }
    }
}
