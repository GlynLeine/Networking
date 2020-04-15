using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Forfeit : Protocol
    {
        public static Action<uint> onForfeit;

        public Forfeit() : base(ProtocolMode.read) { }
        public Forfeit(uint id) : base(ProtocolMode.write)
        {
            this.id = id;
        }

        uint id;
        protected override void Read(Packet packet)
        {
            id = packet.ReadUint();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(id);
        }

        public override void Execute(Server server, Client client)
        {
            onForfeit?.Invoke(id);
        }
    }
}
