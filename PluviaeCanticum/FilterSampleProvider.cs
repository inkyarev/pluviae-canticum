using NAudio.Dsp;
using NAudio.Wave;

namespace PluviaeCanticum;

// straight from chatgpt
public class FilterSampleProvider(ISampleProvider source, BiQuadFilter filter) : ISampleProvider
{
    public WaveFormat WaveFormat => source.WaveFormat;
    
    public int Read(float[] buffer, int offset, int count)
    {
        var samplesRead = source.Read(buffer, offset, count);
        for (var i = 0; i < samplesRead; i++)
        {
            buffer[offset + i] = filter.Transform(buffer[offset + i]);
        }
        return samplesRead;
    }
}