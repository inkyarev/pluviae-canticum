using NAudio.Wave;

namespace PluviaeCanticum;

public class LoopStream(WaveStream sourceStream, bool shouldLoop) : WaveStream
{
    public override WaveFormat WaveFormat => sourceStream.WaveFormat;

    public override long Position 
    {
        get => sourceStream.Position;
        set => sourceStream.Position = value;
    }

    public override long Length => sourceStream.Length;

    public override int Read(byte[] buffer, int offset, int count) 
    {
        var read = 0;
        while (read < count) 
        {
            var required = count - read;
            var readThisTime = sourceStream.Read(buffer, offset + read, required);
            if (readThisTime < required || sourceStream.Position >= sourceStream.Length) 
            {
                if (!shouldLoop) 
                {
                    break;
                }
                sourceStream.Position = 0;
            }
            read += readThisTime;
        }
        return read;
    }
}