using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class UserList : Protocol
    {
        public delegate void OnUserListReceived(List<ClientData> data);
        public static OnUserListReceived onUserListReceived;

        public UserList() : base(ProtocolMode.read)
        {
        }

        public UserList(List<ClientData> clientData) : base(ProtocolMode.write)
        {
            data = clientData;
        }

        List<ClientData> data;

        protected override void Read(Packet packet)
        {
            data = new List<ClientData>();
            uint userCount = packet.ReadUint();
            for (uint i = 0; i < userCount; i++)
            {
                ClientData clientData = new ClientData();
                clientData.id = packet.ReadUint();
                clientData.username = packet.ReadString();
                data.Add(clientData);
            }
        }

        protected override void Write(Packet packet)
        {
            packet.Write((uint)data.Count);

            foreach(ClientData clientData in data)
            {
                packet.Write(clientData.id);
                packet.Write(clientData.username);
            }
        }

        public override void Execute(Client client)
        {
            onUserListReceived?.Invoke(data);
        }
    }
}
