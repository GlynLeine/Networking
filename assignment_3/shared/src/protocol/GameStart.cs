using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class GameStart : Protocol
    {
        public static Action<ClientData, bool> onGameStart;

        public GameStart() : base(ProtocolMode.read) { }

        public GameStart(ClientData opponent, bool firstTurn) : base(ProtocolMode.write)
        {
            this.opponent = opponent;
            this.firstTurn = firstTurn;
        }

        ClientData opponent;
        bool firstTurn;

        protected override void Read(Packet packet)
        {
            opponent = new ClientData();
            opponent.id = packet.ReadUint();
            opponent.username = packet.ReadString();
            firstTurn = packet.ReadBool();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(opponent.id);
            packet.Write(opponent.username);
            packet.Write(firstTurn);
        }

        public override void Execute(Client client)
        {
            onGameStart?.Invoke(opponent, firstTurn);
        }
    }
}
