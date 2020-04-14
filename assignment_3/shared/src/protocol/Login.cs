using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Login : Protocol
    {
        uint acceptedId;
        string message;

        public delegate void OnLogin(uint acceptedId, string message);
        public static OnLogin onLogin;

        public Login() : base(ProtocolMode.read){ }
        public Login(uint acceptedId, string message = "") : base(ProtocolMode.write)
        {
            this.acceptedId = acceptedId;
            this.message = message;
        }

        protected override void Read(Packet pPacket)
        {
            acceptedId = pPacket.ReadUint();
            message = pPacket.ReadString();
        }

        protected override void Write(Packet pPacket)
        {
            pPacket.Write(acceptedId);
            pPacket.Write(message);
        }

        public override void Execute(Client client)
        {
            onLogin?.Invoke(acceptedId, message);
        }
    }
}
