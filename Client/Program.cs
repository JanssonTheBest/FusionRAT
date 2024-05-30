using Client.ConnectionPhase;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Task.Run(ConnectionHelper.Connect);

        while (true) 
        { 
            await Task.Delay(5000);
        }   
    }
}