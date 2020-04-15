using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared
{
    public class Ready : Protocol
    {
        public static Action<CustomDataClient<ClientData>> onReady;

        public Ready() : base(ProtocolMode.irrelevant) { }

        public override void Execute(Server server, Client client)
        {
            onReady?.Invoke(client as CustomDataClient<ClientData>);
        }
    }
}
