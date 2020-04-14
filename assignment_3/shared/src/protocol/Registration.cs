using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace shared
{
    public class Registration : Protocol
    {
        public Registration() : base(ProtocolMode.read)
        {

        }

        public Registration(string username, string password) : base(ProtocolMode.write)
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

        protected override void Read(Packet packet)
        {
            username = packet.ReadString();
            passwordHash = packet.ReadString();
        }

        protected override void Write(Packet packet)
        {
            packet.Write(username);
            packet.Write(passwordHash);
        }
    }
}
