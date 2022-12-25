using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pedantic.Genetics
{
    public class Message
    {
        public MessageType MessageType { get; init; }

        public Message(MessageType msgType)
        {
            MessageType = msgType;
        }

        public static Message Start = new Message(MessageType.Start);
        public static Message RoundComplete = new Message(MessageType.RoundComplete);
        public static Message GenerationComplete = new Message(MessageType.GenerationComplete);
        public static Message EvolutionComplete = new Message(MessageType.EvolutionComplete);
        public static Message Restart = new Message(MessageType.Restart);
        public static Message Cancel = new Message(MessageType.Cancel);
    }

    public class PostMessage : Message
    {
        public Game Game { get; init; }

        public PostMessage(Game game)
            : base(MessageType.Post)
        {
            Game = game;
        }

    }


}
