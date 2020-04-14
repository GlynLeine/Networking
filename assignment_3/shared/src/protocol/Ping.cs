using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Ping : Protocol
    {
        public Ping() : base(ProtocolMode.irrelevant) { }

        public override void Execute(Server server, Client client)
        {
            if (!server.hidePing)
                Console.WriteLine((client as CustomDataClient<ClientData>).data.id + " " + client.ip + " is still here!");
        }
    }
}
