using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using shared;
using System.Threading;

/**
 * This class implements a simple tcp echo server.
 * Read carefully through the comments below.
 * Note that the server does not contain any sort of error handling.
 */
class TCPServerSample
{
    public static void Main(string[] args)
    {
        Server server = new Server();
        GameManager gameManager = new GameManager(server);
        server.Run();
    }
}

