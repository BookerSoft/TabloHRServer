using System.Security.Cryptography.X509Certificates;

namespace TabloHRServer
{
    public class Program
    {
        public  static string tunedCh = "";
        public static bool tunerbusy = false;
        static webServer? server;

        static void Main(string[] args)
        {
            server = new webServer(AppContext.BaseDirectory);
            
        }
    }

}