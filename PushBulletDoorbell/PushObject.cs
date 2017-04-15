using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PushBulletDoorbell
{
    public class PushObject
    {
        public string iden { get; set; }
        public string type { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public float created { get; set; }
        public float modified { get; set; }
        public bool active { get; set; }
        public bool dismissed { get; set; }
        public string sender_iden { get; set; }
        public string sender_email { get; set; }
        public string sender_email_normalized { get; set; }
        public string receiver_iden { get; set; }
        public string receiver_email { get; set; }
        public string receiver_email_normalized { get; set; }
        public string target_device_iden { get; set; }
    }
}