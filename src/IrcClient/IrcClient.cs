using System.Buffers;
using System.IO.Pipelines;
using System.Text;

namespace IrcClient;

public sealed class IrcClient : IDisposable
{
    private readonly Stream _stream;

    private async Task ProcessLinesAsync()
    {
        var pipe = new Pipe();
        var writer = FillPipeAsync(pipe.Writer);
        var reader = ReadPipeAsync(pipe.Reader);

        await Task.WhenAll(writer, reader);
    }

    private async Task FillPipeAsync(PipeWriter writer)
    {
        const int bufferSize = 1024;

        while (true)
        {
            var memory = writer.GetMemory(bufferSize);
            var bytesRead = await _stream.ReadAsync(memory);
            if (bytesRead == 0) break;
            writer.Advance(bytesRead);
            var result = await writer.FlushAsync();
            if (result.IsCompleted) break;
        }

        await writer.CompleteAsync();
    }

    private async Task ReadPipeAsync(PipeReader reader)
    {
        while (true)
        {
            var result = await reader.ReadAsync();
            var buffer = result.Buffer;

            var consumed = ReadLines(buffer);
            reader.AdvanceTo(consumed: consumed, examined: buffer.End);

            if (result.IsCompleted) break;
        }

        await reader.CompleteAsync();
    }

    private SequencePosition ReadLines(ReadOnlySequence<byte> buffer)
    {
        var reader = new SequenceReader<byte>(buffer);
        while (reader.TryReadTo(out ReadOnlySequence<byte> read, delimiter: "\r\n"u8, advancePastDelimiter: true))
        {
            var message = IrcMessage.Parse(Encoding.UTF8.GetString(read));
        }

        return reader.Position;
    }

    public void Dispose()
    {
        _stream.Dispose();
    }
}
