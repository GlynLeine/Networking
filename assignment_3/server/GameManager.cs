using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

using shared;

public class GameManager
{
    List<CustomDataClient<ClientData>> pendingClients = new List<CustomDataClient<ClientData>>();
    List<Game> games = new List<Game>();

    Mutex pendingClientsMutex = new Mutex();

    Server server;
    Thread updateThread;
    public GameManager(Server server)
    {
        this.server = server;
        Ready.onReady += OnClientReady;
        server.onThreadAwait += OnThreadAwait;
        server.onClientConnect += OnClientConnected;

        updateThread = new Thread(new ThreadStart(Update));
        updateThread.Start();
    }

    void OnClientConnected(CustomDataClient<ClientData> client)
    {
        server.Broadcast(new Message(0, client.data.username + " joined."), (object data) => { return ((ClientData)data).id != client.data.id; });
    }

    void OnThreadAwait()
    {
        updateThread.Join();
    }

    void OnClientReady(CustomDataClient<ClientData> client)
    {
        pendingClientsMutex.WaitOne();
        if (!pendingClients.Contains(client))
            pendingClients.Add(client);
        pendingClientsMutex.ReleaseMutex();

        client.Write(new Message(0, "Waiting for oponnent..."));
        Console.WriteLine(client.data.username + " is ready");
    }

    void Update()
    {
        while (!server.Exit)
        {
            while (pendingClients.Count >= 2)
            {
                CustomDataClient<ClientData> clientA;
                CustomDataClient<ClientData> clientB;

                Random random = new Random();
                pendingClientsMutex.WaitOne();
                clientA = pendingClients[random.Next(0, pendingClients.Count)];
                pendingClients.Remove(clientA);
                clientB = pendingClients[random.Next(0, pendingClients.Count)];
                pendingClients.Remove(clientB);
                pendingClientsMutex.ReleaseMutex();

                Game game = new Game(clientA, clientB);
                game.onGameEnd += OnGameEnd;
                games.Add(game);
                Console.WriteLine("started new game between " + clientA.data.username + " and " + clientB.data.username);
            }

            var gameList = games.ToArray();
            for (int i = 0; i < gameList.Length; i++)
                gameList[i].Update();

            if (server.relax)
                Thread.Sleep(100);
        }
    }

    void OnGameEnd(Game game, CustomDataClient<ClientData> winner)
    {
        Console.WriteLine("ended game between " + game.playerA.data.username + " and " + game.playerB.data.username);
        if (winner != null)
            Console.WriteLine(winner.data.username + " won!");
        else
            Console.WriteLine("they tied");
        games.Remove(game);
    }
}

