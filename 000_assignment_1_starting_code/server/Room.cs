using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class Room
    {
        public string name;
        public string password;
        public Client owner;

        public Dictionary<string, Client> clients = new Dictionary<string, Client>();

        public Room(string name)
        {
            owner = null;
            this.name = name;
            password = "";

            Console.WriteLine("Created global room with name " + name);
        }

        public Room(Client owner, string name, string password)
        {
            this.owner = owner;
            this.name = name;
            this.password = password;

            Console.WriteLine("Created room with name " + name);
        }

        public void AddClient(Client client)
        {
            if (clients.ContainsKey(client.name))
                return;

            client.WriteString("Welcome to room " + name + "!");
            Console.WriteLine(client.name + "has joined room " + name);

            foreach (var other in clients)
                other.Value.WriteString("Welcome " + client.name + " to the room!");

            clients.Add(client.name, client);

        }

        public void RemoveClient(Client client)
        {
            if (!clients.ContainsKey(client.name))
                return;

            client.WriteString("Disconnected from room " + name);

            Console.WriteLine(client.name + " has disconnected from room " + name);

            clients.Remove(client.name);
            client.room = null;

            bool transfer = (client == owner && clients.Count > 0);
            if (transfer)
            {
                owner = clients.ElementAt(0).Value;
            }

            foreach (var other in clients)
            {
                other.Value.WriteString(client.name + " has left the room.");

                if (transfer)
                    other.Value.WriteString("Transfered ownership to " + owner.name);
            }
        }
    }
}
