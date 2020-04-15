using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class GameEnd : Protocol
    {
        public static Action<uint, string> onGameEnd;

        public GameEnd() : base(ProtocolMode.read) { }

        public GameEnd(uint winnerId, string reason) : base(ProtocolMode.write)
        {
            this.winnerId = winnerId;
            this.reason = reason;
        }

        uint winnerId;
        string reason;

        protected override void Read(Packet packet)
        {
            winnerId = packet.ReadUint();
            reason = packet.ReadString();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(winnerId);
            packet.Write(reason);
        }

        public override void Execute(Client client)
        {
            onGameEnd?.Invoke(winnerId, reason);
        }
    }
}
