using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Disconnection : Protocol
    {
        public delegate void OnOtherDisconnect(uint id);
        public static OnOtherDisconnect onOtherDisconnect;

        public Disconnection() : base(ProtocolMode.read) { }
        public Disconnection(string message) : base(ProtocolMode.write)
        {
            justification = message;
        }

        public Disconnection(uint id) : base(ProtocolMode.write)
        {
            this.id = id;
        }

        uint id = 0;
        string justification = "";

        protected override void Read(Packet pPacket)
        {
            id = pPacket.ReadUint();
            justification = pPacket.ReadString();
        }

        protected override void Write(Packet pPacket)
        {
            pPacket.Write(id);
            pPacket.Write(justification);
        }

        public override void Execute(Server server, Client client)
        {
            client.Close();
            Console.WriteLine("Disconnected client: " + (client as CustomDataClient<ClientData>).data.id);
        }

        public override void Execute(Client client)
        {
            if (id == 0)
            {
                client.Close();
                client.onDisconnect?.Invoke(client, "Disconnected by server.");
            }
            else
            {
                onOtherDisconnect?.Invoke(id);
            }
        }
    }
}
