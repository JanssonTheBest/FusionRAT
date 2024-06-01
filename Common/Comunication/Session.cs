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
    public interface IConnectionProperties
    {
        TcpClient Client { get; set; }
        SslStream SslStream { get; set; }
    }
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
        private int bufferSize = 65536;
        private TcpClient _client;
        private MemoryStream tempBuffer = new();

        public EventHandler OnPing;
        public EventHandler OnClientInfo;
        public EventHandler OnRemoteDesktop;


        public Session(IConnectionProperties connectionProperties)
        {
            _sslStream = connectionProperties.SslStream;
            pipeReader = pipe.Reader;
            pipeWriter = pipe.Writer;
            _client = connectionProperties.Client;
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

        public async Task SendPacketAsync(IPacket packet)
        {
            await sendSemaphore.WaitAsync();
            byte[] data = MessagePackSerializer.Serialize(packet);
            await tempBuffer.WriteAsync(BitConverter.GetBytes(data.Length));
            await tempBuffer.WriteAsync(data);
            try
            {
                await _sslStream.WriteAsync(tempBuffer.ToArray());
            }
            catch (Exception ex)
            {

            }
            finally
            {
                tempBuffer.SetLength(0);
                sendSemaphore.Release();
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
                bool packetIncomplete = false;

                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (buffer.Length < bytesConsumed + headerLength)
                    {
                        packetIncomplete = true;
                        readThreashold = headerLength + 1;
                        break;
                    }
                    int packetLength = BitConverter.ToInt32(buffer.Slice(bytesConsumed, headerLength).ToArray());

                    if (buffer.Length < bytesConsumed + headerLength + packetLength)
                    {
                        packetIncomplete = true;
                        readThreashold = headerLength + packetLength;
                        break;
                    }
                    IPacket packet = MessagePackSerializer.Deserialize<IPacket>(
                        buffer.Slice(bytesConsumed + headerLength, packetLength),
                        messagePackSerializerOptions,
                        cancellationTokenSource.Token);
                    Task.Run(() => packet.HandlePacket(this));
                    bytesConsumed += headerLength + packetLength;
                }
                pipeReader.AdvanceTo(buffer.GetPosition(bytesConsumed), buffer.End);
                if (packetIncomplete)
                {
                    readThreashold = headerLength + 1;
                }
                else
                {
                    readThreashold = headerLength;
                }
            }
        }

    }
}
