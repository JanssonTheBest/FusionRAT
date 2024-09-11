using Client.ConnectionPhase;
using System.Diagnostics;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;

        Task.Run(ConnectionHelper.Connect);

        while (true) 
        { 
            await Task.Delay(5000);
        }   
    }
}