using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using System.Collections;

namespace PushBulletDoorbell
{
    public class PushBulletResponse 
    {
        public object[] accounts { get; set; }
        public object[] blocks { get; set; }
        public object[] channels { get; set; }
        public object[] chats { get; set; }
        public object[] contacts { get; set; }
        public Device[] devices { get; set; }
        public object[] grants { get; set; }
        public PushObject[] pushes { get; set; }
        public object[] profiles { get; set; }
        public object[] subscriptions { get; set; }
        public object[] texts { get; set; }
    }
}