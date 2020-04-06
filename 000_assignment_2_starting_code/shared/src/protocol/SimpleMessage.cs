using System;

namespace shared
{
    public class SimpleMessage : Protocol
    {
        public delegate void MessageHandler(uint source, string message);
        public static MessageHandler messageHandler;

        public SimpleMessage() : base(ProtocolMode.read)
        {

        }

        public SimpleMessage(uint source, string message) : base(ProtocolMode.write)
        {
            text = message;
            this.source = source;
        }

        protected string text;
        protected uint source;

        public override string ToString()
        {
            return "id " + source.ToString() + ": " + text;
        }

        protected override void Write(Packet pPacket)
        {
            pPacket.Write(text);
            pPacket.Write(source);
        }

        protected override void Read(Packet pPacket)
        {
            text = pPacket.ReadString();
            source = pPacket.ReadUint();
        }

        public override void Execute(Server server, Client client)
        {
            Console.WriteLine(source + " said: " + text);
            server.BroadcastFromId(source, new SimpleMessage(source, text));
        }

        public override void Execute(Client client)
        {
            messageHandler?.Invoke(source, text);
        }
    }
}
