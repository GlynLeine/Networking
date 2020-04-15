using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Disconnection : Protocol
    {
        public delegate void OnOtherDisconnect(ClientData clientData);
        public static OnOtherDisconnect onOtherDisconnect;

        public Disconnection() : base(ProtocolMode.read) { }
        public Disconnection(string message) : base(ProtocolMode.write)
        {
            justification = message;
            clientData.id = 0;
            clientData.username = "";
        }

        public Disconnection(ClientData clientData) : base(ProtocolMode.write)
        {
            this.clientData = clientData;
        }

        ClientData clientData;
        string justification = "";

        protected override void Read(Packet packet)
        {
            clientData.id = packet.ReadUint();
            clientData.username = packet.ReadString();
            justification = packet.ReadString();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(clientData.id);
            packet.Write(clientData.username);
            packet.Write(justification);
        }

        public override void Execute(Server server, Client client)
        {
            client.Close();
            Console.WriteLine("Disconnected client: " + (client as CustomDataClient<ClientData>).data.id);
        }

        public override void Execute(Client client)
        {
            if (clientData.id == 0)
            {
                client.Close();
                client.onDisconnect?.Invoke(client, "Disconnected by server.");
            }
            else
            {
                onOtherDisconnect?.Invoke(clientData);
            }
        }
    }
}
