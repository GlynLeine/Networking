using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    public class Disconnect : Command
    {
        public Disconnect(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to disconnect from the server.";
        }

        public override string GetName()
        {
            return "disconnect";
        }

        public override bool Identify(string command)
        {
            return false;
        }
    }

    public class NickName : Command
    {
        public NickName(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (parameters == null)
            {
                client.WriteString("0 parameters found, 1 expected");
                return 0;
            }

            string nick = parameters[0];
            nick = nick.ToLower();

            server.ChangeNickName(client, nick);

            return -1;
        }

        public override string GetInfo()
        {
            return "This command is used to change your nickname. The set nickname is temporary and reset when loging in again. The command expects 1 parameter. The parameter being your new nickname.\nAliases: nick, cn, nn";
        }

        public override string GetName()
        {
            return "nickname";
        }

        public override bool Identify(string command)
        {
            return command == "nickname" || command == "nick" || command == "cn" || command == "nn";
        }
    }

    public class List : Command
    {
        public List(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (client.room == null)
                client.WriteString("You're currently not in a room.");
            else
            {
                string list = "Users in room " + client.room.name + ":";

                foreach (string name in client.room.clients.Keys)
                    list += "\n\t" + name;

                client.WriteString(list);
            }

            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to get a list of all users connected to this room.\nAliases: ls";
        }

        public override string GetName()
        {
            return "list";
        }

        public override bool Identify(string command)
        {
            return command == "ls" || command == "list";
        }
    }

    public class ListAll : Command
    {
        public ListAll(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            string list = "All users:";

            foreach (string name in server.GetClientList())
                list += "\n\t" + name;

            client.WriteString(list);

            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to get a list of all users connected to this server.\nAliases: lsa";
        }

        public override string GetName()
        {
            return "listall";
        }

        public override bool Identify(string command)
        {
            return command == "lsa" || command == "listall";
        }
    }

    public class ListRooms : Command
    {
        public ListRooms(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            string list = "Rooms:";

            foreach (string roomName in server.GetRoomList())
                list += "\n\t" + roomName;

            client.WriteString(list);
            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to get a list of all rooms.\nAliases: listrm, lsrm";
        }

        public override string GetName()
        {
            return "listrooms";
        }

        public override bool Identify(string command)
        {
            return command == "listrm" || command == "listrooms" || command == "lsrm";
        }
    }

    public class CreateRoom : Command
    {
        public CreateRoom(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (parameters == null)
            {
                client.WriteString("0 parameters found, 1 or 2 expected");
                return 0;
            }
            string roomName = parameters[0];

            if (server.rooms.ContainsKey(roomName))
            {
                client.WriteString("Room " + roomName + " already exists.");
                return 0;
            }

            string password = "";
            if (parameters.Length > 1)
                password = parameters[1];

            Room room = new Room(client, roomName, password);
            server.rooms.Add(roomName, room);

            server.MoveClientToRoom(client, room, password);
            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to create and join a new room. The command expects 1 or 2 parameters. The first parameter being the room name, the second the password if there is any.\nAliases: crm";
        }

        public override string GetName()
        {
            return "createroom";
        }

        public override bool Identify(string command)
        {
            return command == "createroom" || command == "crm";
        }
    }

    public class JoinRoom : Command
    {
        public JoinRoom(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (parameters != null)
            {
                if (!server.rooms.ContainsKey(parameters[0]))
                {
                    client.WriteString("Room " + parameters[0] + " does not exist.");
                    return 0;
                }

                if (parameters.Length > 1)
                    server.MoveClientToRoom(client, server.rooms[parameters[0]], parameters[1]);
                else
                    server.MoveClientToRoom(client, server.rooms[parameters[0]], "");
            }
            else
                client.WriteString("0 parameters found, 1 or 2 expected");

            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to join a certain room. The command expects 1 or 2 parameters. The first parameter being the room name, the second the password if there is any.\nAliases: join, jrm";
        }

        public override string GetName()
        {
            return "joinroom";
        }

        public override bool Identify(string command)
        {
            return command == "join" || command == "joinroom" || command == "jrm";
        }
    }

    public class Help : Command
    {
        public Help(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (parameters == null)
            {
                string commands = "Commands:";
                foreach (string command in server.commands.Keys)
                    if (command != "reconfirm")
                        commands += "\n\t" + command;

                commands += "\n\n use /help followed by a command name for more information.";

                client.WriteString(commands);
            }
            else
            {
                if (server.commands.ContainsKey(parameters[0]))
                    client.WriteString("\n" + server.commands[parameters[0]].GetInfo());
                else
                    client.WriteString("Command " + parameters[0] + " is unknown.");
            }

            return 0;
        }

        public override string GetInfo()
        {
            return "You just used this command... this command is used to get a list of all commands or get more information on a certain command.\nAliases: h";
        }

        public override string GetName()
        {
            return "help";
        }

        public override bool Identify(string command)
        {
            return command == "help" || command == "h";
        }
    }

    public class Reconfirm : Command
    {
        public Reconfirm(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            client.timeout = 0f;
            Console.WriteLine(client.name + " is still connected");
            return 0;
        }

        public override string GetInfo()
        {
            return "You shouldn't know about this command...";
        }

        public override string GetName()
        {
            return "reconfirm";
        }

        public override bool Identify(string command)
        {
            return command == "reconfirm";
        }
    }

    public class Whisper : Command
    {
        public Whisper(Server server) : base(server)
        {
        }

        public override int Execute(Client client, string parameter, string[] parameters)
        {
            if (parameters == null || parameters.Length < 2)
            {
                client.WriteString("This command requires at least 2 parameters.");
                return 0;
            }

            if(!server.clients.ContainsKey(parameters[0]))
            {
                client.WriteString("User " + parameters[0] + " cannot be found.");
                return 0;
            }

            string message = parameter.Substring(parameter.IndexOf(' ') + 1);
            
            server.clients[parameters[0]].WriteString(client.name + " whispered: " + message);
            client.WriteString("You whispered to " + parameters[0] + ": " + message);

            return 0;
        }

        public override string GetInfo()
        {
            return "This command is used to whisper to other users across rooms. The command expects at least 2 parameters. The first parameter being the user name, the second and onward collectively forming your message.\nAliases: w";
        }

        public override string GetName()
        {
            return "whisper";
        }

        public override bool Identify(string command)
        {
            return command == "whisper" || command == "w";
        }
    }
}
