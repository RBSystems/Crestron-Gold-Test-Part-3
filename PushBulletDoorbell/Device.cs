using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace PushBulletDoorbell
{
    public class Device
    {
        public bool active { get; set; }
        public string iden { get; set; }
        public float created { get; set; }
        public float modified { get; set; }
        public string type { get; set; }
        public string kind { get; set; }
        public string nickname { get; set; }
        public bool generated_nickname { get; set; }
        public string manufacturer { get; set; }
        public string model { get; set; }
        public int app_version { get; set; }
        public string fingerprint { get; set; }
        public string push_token { get; set; }
        public bool pushable { get; set; }
        public bool has_sms { get; set; }
        public bool has_mms { get; set; }
        public string icon { get; set; }
        public string remote_files { get; set; }
    }
}