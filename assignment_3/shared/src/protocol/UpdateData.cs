using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class UpdateData : Protocol
    {
        public delegate void OnClientDataUpdate(ClientData data);
        public static OnClientDataUpdate onClientDataUpdate;

        public UpdateData() : base(ProtocolMode.read)
        {
        }

        public UpdateData(ClientData data) : base(ProtocolMode.write)
        {
            this.data = data;
        }

        ClientData data;

        protected override void Read(Packet packet)
        {
            data.id = packet.ReadUint();
            data.username = packet.ReadString();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(data.id);
            packet.Write(data.username);
        }

        public override void Execute(Server server, Client client)
        {
            Console.WriteLine(data.id + " username: " + data.username);
            server.UpdateClientData(data);
        }

        public override void Execute(Client client)
        {
            onClientDataUpdate?.Invoke(data);
        }
    }
}
