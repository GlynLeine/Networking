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
            data.skinId = packet.ReadUint();
            data.x = packet.ReadFloat();
            data.y = packet.ReadFloat();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(data.id);
            packet.Write(data.skinId);
            packet.Write(data.x);
            packet.Write(data.y);
        }

        public override void Execute(Server server, Client client)
        {
            Console.WriteLine(data.id + " has skin " + data.skinId + " and position [" + data.x + ", " + data.y + "]");
            server.UpdateClientData(data);
        }

        public override void Execute(Client client)
        {
            onClientDataUpdate?.Invoke(data);
        }
    }
}
