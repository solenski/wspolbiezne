using System;
using System.Runtime.Remoting.Channels.Ipc;    //Importing IPC
                                               //channel
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting;
using System.Threading.Tasks;
using shared_objects;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace dropbox_server
{
    class TaskFolderTuple
    {
        public Task Saver { get; set; } = new Task(() => { });
        private const int Delay = 500;
        private string _Path;
        public bool InProgress = false;


        public TaskFolderTuple(string path)
        {
            this._Path = path;
        }

        public void SetUpTask(MessageFormat message)
        {

            this.Saver = Task.Run(async () =>
          {
              Console.WriteLine($"Copying from { message.Message.Name } to { this._Path }");

              File.Copy(message.Message.Name, Path.Combine(_Path, Path.GetFileName(message.Message.Name)), true);
              await Task.Delay(Delay);

              var pathToAppend = $@"C:\uczelnia\{ Path.GetFileName(_Path) }" + "\\" + Path.GetFileName(_Path) + ".log";
              var contentToAppend = $"Username : { message.Username }, File : { Path.GetFileName(message.Message.Name) }" + Environment.NewLine;

              Console.WriteLine($"Appending : {pathToAppend} with {contentToAppend}");

              File.AppendAllText(pathToAppend, contentToAppend);

          });

        }
    }



    class FileSaver
    {
        IReadOnlyList<string> Paths = new string[] {
            @"C:\uczelnia\folder1",
            @"C:\uczelnia\folder2",
            @"C:\uczelnia\folder3",
            @"C:\uczelnia\folder4",
            @"C:\uczelnia\folder5" };

        public FileSaver()
        {
            this.TaskFolderTuples = this.Paths.Select(x => new TaskFolderTuple(x)).ToList();
        }

        public List<TaskFolderTuple> TaskFolderTuples { get; }

        public void SaveFile(MessageFormat message)
        {
            while (true)
            {
                var taskFolderTuple = this.TaskFolderTuples.FirstOrDefault(x =>
                x.Saver.Status != TaskStatus.Running &&
                x.Saver.Status != TaskStatus.WaitingForActivation &&
                x.Saver.Status != TaskStatus.WaitingToRun &&
                x.Saver.Status != TaskStatus.WaitingForChildrenToComplete);


                if (taskFolderTuple != null)
                {
                    taskFolderTuple.SetUpTask(message);
                    break;
                }
               
                Console.WriteLine($"No Task found, waiting");

            }
        }


    }

    class Orchestrator
    {
        public Receiver Receiver { get; }
        public FileSaver FileSaver { get; }


        public Orchestrator(Receiver receiver)
        {
            Receiver = receiver;
            FileSaver = new FileSaver();
            this.Init();
        }


        private async void Init()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    var message = await AwaitMessagesAync();
                    Console.WriteLine($"Message from : { message.Username} recevied");
                    FileSaver.SaveFile(message);
                }
            });
        }

        private async Task<MessageFormat> AwaitMessagesAync()
        {
            return await this.Receiver.GetMessage();
        }
    }

    class Receiver
    {


        public Receiver()
        {
            this.Channel = new IpcServerChannel(Configuration.ChannelName);

            //Register the server channel.
            ChannelServices.RegisterChannel(this.Channel, true);

            //Register this service type.
            RemotingConfiguration.RegisterWellKnownServiceType(
                                        typeof(ServerMethods),
                                        "ServerMethods",
                                        WellKnownObjectMode.Singleton);

        }

        public IpcServerChannel Channel { get; }

        internal Task<MessageFormat> GetMessage()
        {
            return Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    if (ServerData.Messages.Count != 0 && ServerData.Messages.TryDequeue(out MessageFormat dequeued))
                    {
                        return dequeued;
                    }
                }
            });
        }
    }

    class Program
    {
        public static Orchestrator Orchestrator { get; private set; }

        static void Main(string[] args)
        {
            Bootstrap();
            Console.ReadLine();
        }

        private static void Bootstrap()
        {
            Orchestrator = new Orchestrator(new Receiver());
        }


    }
}
