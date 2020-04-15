using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class GameMove : Protocol
    {
        public delegate void OnGameMove(GameMove move);
        public static OnGameMove onGameMove;

        public GameMove() : base(ProtocolMode.read) { }

        public GameMove(uint id, int move) : base(ProtocolMode.write)
        {
            playerId = id;
            moveId = move;
        }

        uint playerId;
        int moveId;

        public uint PlayerId => playerId;
        public int MoveId => moveId;

        protected override void Read(Packet packet)
        {
            playerId = packet.ReadUint();
            moveId = packet.ReadInt();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(playerId);
            packet.Write(moveId);
        }

        public override void Execute(Client client)
        {
            Console.WriteLine(playerId + " made move " + moveId);
            onGameMove?.Invoke(this);
        }

        public override void Execute(Server server, Client client)
        {
            onGameMove?.Invoke(this);

            Console.WriteLine(playerId + " made move " + moveId);
        }

        public static implicit operator int(GameMove move)
        {
            return move.moveId;
        }
    }
}
