using ZeroFormatter;

namespace ZipKeyStreamingClient.Interface.Payload
{
    [ZeroFormattable]
    public class CameraDataPayload
    {
        [Index(0)]
        public virtual byte[] JpegData { get; set; }
    }
}
