using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Whisper : SimpleMessage
    {
        public Whisper() : base()
        {
        }

        public Whisper(uint source, string message) : base(source, message)
        {
        }

        public override string ToString()
        {
            return "id " + source.ToString() + " whispers: " + text;
        }

        public override void Execute(Server server, Client client)
        {
            Console.WriteLine(source + " whispered: " + text);
            server.BroadcastFromId(source, new SimpleMessage(source, text),
                (object data) =>
                {
                    ClientData otherdata = (ClientData)data;
                    ClientData ourdata = (ClientData)client.data;
                    float x = otherdata.x - ourdata.x;
                    float y = otherdata.y - ourdata.y;
                    return Math.Sqrt(x * x + y * y) < 2;
                });
        }
    }
}
