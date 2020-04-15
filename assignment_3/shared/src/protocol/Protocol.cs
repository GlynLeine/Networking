using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public enum ProtocolMode
    {
        write = 1, read = -1, irrelevant = 0
    }
    public abstract class Protocol : ISerializable
    {
        ProtocolMode mode;
        public Protocol(ProtocolMode mode)
        {
            this.mode = mode;
        }

        public ProtocolMode Mode => mode;

        /// <summary>
        /// Server-side execution.
        /// </summary>
        /// <param name="server"></param>
        /// <param name="client"></param>
        public virtual void Execute(Server server, Client client) { }
        /// <summary>
        /// Client-side execution.
        /// </summary>
        /// <param name="client"></param>
        public virtual void Execute(Client client) { }

        public void Deserialize(Packet pPacket)
        {
            if(mode > 0)
                throw new InvalidOperationException("Attempted read operation on a write only packet.");

            Read(pPacket);
        }

        protected virtual void Read(Packet packet) { }

        public void Serialize(Packet pPacket)
        {
            if(mode < 0)
                throw new InvalidOperationException("Attempted write operation on a read only packet.");

            Write(pPacket);
        }

        protected virtual void Write(Packet packet) { }

    }
}
