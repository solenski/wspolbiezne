using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace shared_objects
{
    [Serializable]

    public class MessageResult
    {
        public string Name { get; set; }

    }

    [Serializable]
    public class MessageFormat
    {
        public string Username { get; set; }
        public MessageResult Message { get; set; }
    }

    public class Configuration
    {
        public const string ChannelName = "DROPBOX_CLONE_CHANNEL";
    }


    public class ServerData
    {
        public static string Status { get; set; }
        public static DateTime StartTime { get; set; }
        public static string IsProcessing { get; set; }

        public static ConcurrentQueue<MessageFormat> Messages { get; set; } = new ConcurrentQueue<MessageFormat>();

    }

    public class ServerMethods : MarshalByRefObject
    {
        public string Status
        {
            get { return ServerData.Status; }
        }

        public DateTime StartTime
        {
            get { return ServerData.StartTime; }
        }

        public string IsProcessing
        {
            get { return ServerData.IsProcessing; }
        }

        public void AddMessage(dynamic message)
        {
            ServerData.Messages.Enqueue(message);
        }
    }
}
