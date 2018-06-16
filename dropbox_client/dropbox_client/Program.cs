using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting.Channels.Ipc;    //Importing IPC
                                               //channel
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using shared_objects;

namespace dropbox_client
{
    class Settings
    {
        public string Username { get; set; }
        public string Path { get; set; }
    }

    class Orchestrator
    {
        public Settings Settings { get; }
        public Sender Sender { get; }
        public FileSystemWatcher FileWatcher { get; }
        public Task MonitorTask { get; }

        public Orchestrator(Settings settings)
        {
            this.Settings = settings;
            this.Sender = new Sender();
            this.FileWatcher = new FileSystemWatcher(settings.Path);
            this.FileWatcher.InternalBufferSize = 64000;
            this.FileWatcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.FileName;
            this.FileWatcher.IncludeSubdirectories = false;
            this.FileWatcher.EnableRaisingEvents = true;
            this.FileWatcher.Created += (_, @event) =>
                        this.Sender.SendMessage(this.Settings.Username, Path.Combine(settings.Path, @event.Name));



        }

    }

    class Sender
    {
        public Sender()
        {
            IpcClientChannel channel = new IpcClientChannel();

            //Register the channel with ChannelServices.
            ChannelServices.RegisterChannel(channel, true);

            //Register the client type.
            RemotingConfiguration.RegisterWellKnownClientType(
                                typeof(ServerMethods),
                                $"ipc://{ Configuration.ChannelName }/ServerMethods");

            this.Comlink = new ServerMethods();
        }

        public void SendMessage(string username, string message)
        {
            this.Comlink.AddMessage(new MessageFormat { Username = username, Message = new MessageResult { Name = message } });
        }

        private ServerMethods Comlink { get; }
    }


    class Program
    {
        public static Orchestrator Orchestrator { get; private set; }

        static void Main(string[] args)
        {
            Bootstrap(args);
            Console.ReadLine();
        }

        private static void Bootstrap(string[] args)
        {
            Orchestrator = new Orchestrator(new Settings { Username = args[0], Path = args[1] });
        }

    }
}
