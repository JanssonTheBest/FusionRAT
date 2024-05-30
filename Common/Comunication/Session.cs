using Common.DTOs.MessagePack;
using MessagePack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Comunication
{
    public class Session
    {
        private SslStream _sslStream;
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private Task receiveDataTask = Task.CompletedTask;
        private Task sendDataTask = Task.CompletedTask;
        private Task extractPacketTask = Task.CompletedTask;
        private Pipe pipe = new();
        private PipeReader pipeReader;
        private PipeWriter pipeWriter;
        private int headerLength = 4;
        private readonly MessagePackSerializerOptions messagePackSerializerOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block).WithResolver(MessagePack.Resolvers.StandardResolver.Instance);
        private SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        private int bufferSize = 1024;

        public EventHandler OnPing;

        public Session(SslStream sslStream)
        {
            _sslStream = sslStream;
            pipeReader = pipe.Reader;
            pipeWriter = pipe.Writer;
        }

        public void BeginSession()
        {
            Task.Run(StartReceivingData);
        }

        public async Task ChangeNetworkBufferSize(int newSize)
        {
            bufferSize = newSize;
        }

        private async Task StartReceivingData()
        {
            extractPacketTask = Task.Run(ExtractPacketsLoop, cancellationTokenSource.Token);
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _sslStream.ReadAsync(pipeWriter.GetMemory(bufferSize), cancellationTokenSource.Token);
                    pipeWriter.Advance(result);
                    await pipeWriter.FlushAsync();
                }
            }
            catch (Exception ex)
            {
                //Lost Connection
                return;
            }
        }

        private async Task ExtractPacketsLoop()
        {
            int readThreashold = headerLength + 1;
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                var result = await pipeReader.ReadAtLeastAsync(readThreashold);
                ReadOnlySequence<byte> buffer = result.Buffer;
                int bytesConsumed = 0;
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (buffer.Length < headerLength + 1 + bytesConsumed)
                    {
                        readThreashold = headerLength + 1;
                        break;
                    }

                    int packetLength = BitConverter.ToInt32(buffer.Slice(0, headerLength).ToArray());

                    if (buffer.Length < packetLength + headerLength + bytesConsumed)
                    {
                        readThreashold = packetLength + headerLength;
                        break;
                    }

                    IPacket packet = MessagePackSerializer.Deserialize<IPacket>(buffer.Slice(headerLength + bytesConsumed, packetLength), messagePackSerializerOptions, cancellationTokenSource.Token);
                    await packet.HandlePacket(this);
                    bytesConsumed += packetLength + headerLength;
                }
                pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed), buffer.End);
            }
        }
    }
}
