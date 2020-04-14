using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace shared
{
    public class Connection : Protocol
    {
        public Connection() : base(ProtocolMode.read) { }
        public Connection(string username, string password) : base(ProtocolMode.write)
        {
            this.username = username;
            StringBuilder builder = new StringBuilder();
            foreach (byte hashByte in SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(password)))
            {
                builder.Append(hashByte.ToString("x2"));
            }
            passwordHash = builder.ToString();
        }

        string username;
        string passwordHash;

        public string Username => username;
        public string PassHash => passwordHash;

        protected override void Read(Packet pPacket)
        {
            username = pPacket.ReadString();
            passwordHash = pPacket.ReadString();
        }

        protected override void Write(Packet pPacket)
        {
            pPacket.Write(username);
            pPacket.Write(passwordHash);
        }
    }
}
