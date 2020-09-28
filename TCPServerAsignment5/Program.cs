using System;

namespace TCPServerAsignment5
{
    class Program
    {
        static void Main(string[] args)
        {
            ServerWorker server = new ServerWorker();

            server.Start();
        }
    }
}
