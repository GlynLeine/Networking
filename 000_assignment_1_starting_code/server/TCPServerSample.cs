using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;
using server;

class TCPServerSample
{
    /**
	 * This class implements a simple concurrent TCP Echo server.
	 * Read carefully through the comments below.
	 */

    public static void Main(string[] args)
    {
        Server server = new Server();

        server.Run();
    }
}


