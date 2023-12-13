using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Models
{
    internal class Message
    {
		private string sender;
		public string Sender
        {
			get { return sender; }
		}

        private string textMessage;
        public string TextMessage
        {
            get { return textMessage; }
        }

        private string timeStamp;
        public string TimeStamp
        {
            get { return timeStamp; }
        }
        public Message(string sender, string textMessage, string timeStamp)
        {
            this.sender = sender;
            this.textMessage = textMessage;
            this.timeStamp = timeStamp;
        }
    }
}
