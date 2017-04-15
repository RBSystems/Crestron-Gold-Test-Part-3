using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PushBulletDoorbell
{
    public class DoorLockPushReceivedEventArgs
    {
        public string Message { get; set; }
    }
}