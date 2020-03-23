using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public abstract class Command
    {
        protected Server server;

        public Command(Server server)
        {
            this.server = server;
        }

        public abstract string GetName();

        public abstract string GetInfo();

        public abstract bool Identify(string command);
        public abstract int Execute(Client client, string parameter, string[] parameters);
    }
}
