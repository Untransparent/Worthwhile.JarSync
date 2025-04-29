
using Serilog;
using Worthwhile.JarSync.CommonConfiguration;

namespace Worthwhile.JarSync.ConsoleDriver
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ResourceSyncEngineDriver driver = new ResourceSyncEngineDriver();

            try
            {
                driver.Initialize();
            }
            catch (Exception aExc)
            {
                Console.WriteLine("Error initializing ResourceSyncEngineDriver. Exiting.");
                Console.WriteLine(aExc.ToString());
                throw;
            }
            driver.Run();
            Log.CloseAndFlush();
        }
    }
}
