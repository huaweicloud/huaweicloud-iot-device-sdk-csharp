using System.Threading;
using IoT.Gateway.Demo;

namespace IoT.TCP.Device.Test
{
    class Program
    {
        private static ManualResetEvent mre = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            new TcpDevice("localhost", 8080);

            mre.WaitOne();
        }
    }
}
