using System;
using System.Text;
using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp.CrestronWebSocketClient;
using Crestron.SimplSharp.Net;  
using Crestron.SimplSharp.Net.Https;    
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Linq;                       				

namespace PushBulletDoorbell
{
    public class Class1
    {
        private string doorbellAccessToken;
        private string doorbellIden ;
        private string doorbellNickname;

        WebSocketClient webSocketClient = null;

        /// <summary>
        /// SIMPL+ can only execute the default constructor. If you have variables that require initialization, please
        /// use an Initialize method
        /// </summary>
        public Class1()
        {
        }
        // Event handlers
        public delegate void DoorLockPushRecievedEventHandler(object sender, DoorLockPushReceivedEventArgs args);
        public event DoorLockPushRecievedEventHandler DoorLockPusedReceived;
        // public methods
        /// <summary>
        /// This method is called from simpl+ at startup and is responsible for:
        ///     - setting up new doorbell device on pushbullet
        ///     - polling for missed door lock requests while offline
        ///     - connection to websocket  
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="deviceName"></param>
        public void ConnectToService(string accessToken, string doorbellName)
        {
            doorbellAccessToken = accessToken;
            doorbellNickname = doorbellName;

            OnDoorLockMessageReceived("Connecting to service");

            try
            {
                doorbellIden = PushBelletGetDoorbell(doorbellAccessToken, doorbellNickname);

                if (doorbellIden != "Error")
                {
                    PushBulletGetMissedPushes(doorbellAccessToken, doorbellIden);

                    for (int i = 0; i < 3; i++)
                    {
                        if (ConnectToWebSocket(accessToken))
                        {
                            PushBulletGetMissedPushes(doorbellAccessToken, doorbellIden);
                            break;
                        }
                    }
                }
                else
                {
                    OnDoorLockMessageReceived("Error Connecting to Pushbullet Service");
                }
            }
            catch(Exception e) 
            {
                ErrorLog.Error("Connection to PushBullet Failed: ", e.Message);
                webSocketClient.Disconnect();
            }  
        }
        public void DisconnectFromService()
        {
            if (webSocketClient != null)
            {
                webSocketClient.Disconnect();
                ErrorLog.Notice("Pushbullet websocket disconnected by user");
            }
        }        
        public void DoorBellPressed(string accessToken, string doorbellName, string lockCommand, string unlockCommand)
        {
            string message = string.Format("Sombody's at {0}! Select {0} and reply \"{1}\" to unlock door or \"{2}\" to lock", doorbellName, unlockCommand, lockCommand);
            PushBulletSendPushMessage(accessToken, doorbellIden, message);
        }
        // private methods
        // https
        private void OnDoorLockMessageReceived(string message)
        {
            if (DoorLockPusedReceived != null)
            {
                DoorLockPushReceivedEventArgs args = new DoorLockPushReceivedEventArgs();
                args.Message = message;
                DoorLockPusedReceived(this, args);
            }
        }
        /// <summary>
        /// Connect to pushbullet and make a send request
        /// </summary>
        private string PushBulletHttpRequest(string requestUrl, string accessToken, RequestType HttpRequestType, string pushBulletObject)
        {
            HttpsClient client = new HttpsClient();
            client.PeerVerification = false;
            client.HostVerification = false;
            client.Verbose = false;
            
            HttpsClientRequest httpRequest = new HttpsClientRequest();
            HttpsClientResponse response;

            try
            {
                httpRequest.KeepAlive = true;
                httpRequest.Url.Parse(requestUrl);
                httpRequest.RequestType = HttpRequestType; 
                httpRequest.Header.SetHeaderValue("Content-Type", "application/json");
                httpRequest.Header.SetHeaderValue("Access-Token", accessToken);
                httpRequest.ContentString = pushBulletObject; 
               
                response = client.Dispatch(httpRequest);

                if (response.Code >= 200 && response.Code < 300)
                {
                    // sucess
                    return response.ContentString;
                }
                else if(response.Code == 401 | response.Code == 403)
                {
                     ErrorLog.Error("PushBullet Invalid Access Token");
                     OnDoorLockMessageReceived("PushBullet Invalid Access Token");
                     return "Error";
                }
                else
                {
                    // error responce
                    ErrorLog.Error(response.ContentString.ToString());
                    return "Error";
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("PushBulletHttpRequest Error: " + e.Message);
                return "Error";
            }
            finally
            {
                client.Dispose();
            }
        } 
        /// <summary>
        /// This method requests a list of devices from pushbullet searches for the a our doorbell device and returns the device iden
        /// If the doorbell device does not exist we create a new doorbell in pushbullet, request the device again and return the iden.
        /// </summary>
        /// <param name="accessTocken"></param>
        /// <returns></returns>
        private string PushBelletGetDoorbell(string accessToken, string objectName)
        {
            RequestType httpRequestType = Crestron.SimplSharp.Net.Https.RequestType.Get;
            string requestResponse = PushBulletHttpRequest("https://api.pushbullet.com/v2/devices", accessToken, httpRequestType, "");
            if (requestResponse != "Error")
            {
                string iden = GetDoorbellIden(requestResponse, objectName);

                if (iden != null)
                {
                    OnDoorLockMessageReceived("Doorbell Iden = " + iden);
                    return iden;
                }
                else
                {
                    OnDoorLockMessageReceived("Creating New Doorbell");
                    PushBulletCreateNewDevice(accessToken, objectName);
                    requestResponse = PushBulletHttpRequest("https://api.pushbullet.com/v2/devices", accessToken, httpRequestType, "");
                    return GetDoorbellIden(requestResponse, objectName);
                }
            }
            else
            {
                return "Error";
            }
        }
        /// <summary>
        /// Create new doorbell in push bullet
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="objectName"></param>
        private void PushBulletCreateNewDevice(string accessToken, string objectName)
        {
            Device doorBell = new Device();
            doorBell.nickname = objectName;
            doorBell.model = "Crestron Doorbell";
            doorBell.manufacturer = "Crestron";

            string serialObject = JsonConvert.SerializeObject(doorBell, Formatting.Indented);
            PushBulletHttpRequest("https://api.pushbullet.com/v2/devices", accessToken, Crestron.SimplSharp.Net.Https.RequestType.Post, serialObject);
        }
        /// <summary>
        /// This method looks for pushes to the doorbell that we may have missed while controller was offline.
        /// We only look back 5 minutes and only respond to the last relevent message.
        /// </summary>
        private void PushBulletGetMissedPushes(string accessToken, string doorbellIden)
        {
            // workout timestamp 
            DateTime pushRequestTime = DateTime.Now.ToUniversalTime();  
            DateTime pushRequetPeriod = pushRequestTime.Add(new TimeSpan(0, -5, 0)); 
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc); 
            long unixTimestamp = ((long)(pushRequetPeriod - sTime).TotalSeconds);

            // make request and respond if need be
            String url = String.Format("https://api.pushbullet.com/v2/pushes?modified_after={0}&active=true", unixTimestamp);
            string requestResponse = PushBulletHttpRequest(url, accessToken, Crestron.SimplSharp.Net.Https.RequestType.Get,"");
            if (requestResponse != "Error")
            {
                PushBulletResponse pushBulletResponse = JsonConvert.DeserializeObject<PushBulletResponse>(requestResponse);
                List<PushObject> messagesToDoorbell = new List<PushObject>();

                foreach (var response in pushBulletResponse.pushes)
                {
                    if ((response.target_device_iden == doorbellIden) && (response.body.Length > 0))
                    {
                        messagesToDoorbell.Add(response);
                    }
                }

                if (messagesToDoorbell.Count > 0)
                {
                    string messageToDoorbell = messagesToDoorbell[0].body;
                    OnDoorLockMessageReceived(messageToDoorbell.ToUpper().Trim());
                }
                else
                {
                    OnDoorLockMessageReceived("No new messages");
                }
            }
            else
            {
                OnDoorLockMessageReceived("Server Response Error. Check error log");
            }
        }
        /// <summary>
        /// Finds the doorbell device and returns the doorbell iden.  If doorbell does not exist we create it
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string GetDoorbellIden(string requestResponse, string objectName)
        {
            string iden;
            PushBulletResponse pushBulletResponse = JsonConvert.DeserializeObject<PushBulletResponse>(requestResponse);
            List<Device> devices = new List<Device>();

            foreach (var device in pushBulletResponse.devices)
            {
                devices.Add(device);
            }

            Device result = devices.Find(item => item.nickname == objectName);

            if (result != null)
            {
                iden = result.iden ;
                return iden;
            }
            else
            {
                iden = null ;
                return iden;
            }  
        }
        /// <summary>
        /// This method send a Push message to PushBullet
        /// </summary>
        /// <param name="accessToken"></param>
        /// <param name="doorbellIden"></param>
        /// <param name="message"></param>
        private void PushBulletSendPushMessage(string accessToken, string doorbellIden, string message)
        {
            PushObject doorbellPressPush = new PushObject();
            doorbellPressPush.sender_iden = doorbellIden;
            doorbellPressPush.type = "note";
            doorbellPressPush.body = message;

            string serialObject = JsonConvert.SerializeObject(doorbellPressPush, Formatting.Indented);
            PushBulletHttpRequest("https://api.pushbullet.com/v2/pushes", accessToken, Crestron.SimplSharp.Net.Https.RequestType.Post, serialObject);
        }
        // websocket
        /// <summary>
        /// The following methods handle and monitor the websocket connection to pushbullet. 
        /// </summary>
        /// <param name="accessToken"></param>
        /// <returns></returns>
        private bool ConnectToWebSocket(string accessToken)
        {
            string token = accessToken;

            try
            {
                webSocketClient = new WebSocketClient();
                webSocketClient.Port = 443;
                webSocketClient.SSL = true;
                webSocketClient.KeepAlive = true;
                webSocketClient.URL = string.Format("wss://stream.pushbullet.com/websocket/{0}", token);
                webSocketClient.SendCallBack = WebSocketCallback;
                webSocketClient.ReceiveCallBack = WebSocketDataReceived;


                if (webSocketClient.Connect() == (int)WebSocketClient.WEBSOCKET_RESULT_CODES.WEBSOCKET_CLIENT_SUCCESS)
                {
                    CrestronConsole.PrintLine("Websocket Connection Success!!");
                    OnDoorLockMessageReceived("Websocket Connection Success!!");
                    webSocketClient.ReceiveAsync();
                    return true;
                }
                else
                {
                    CrestronConsole.PrintLine("Websocket Connection Failed. Try again");
                    OnDoorLockMessageReceived("Websocket Connection Failed. Try again");
                    return false;
                }
            }
            catch (Exception e)
            {
                ErrorLog.Error("Websocket Connection Failed: {0} ", e.Message);
                OnDoorLockMessageReceived("Websocket Connection Failed. Try again");
                return false;
            }

        }
        private int WebSocketCallback(WebSocketClient.WEBSOCKET_RESULT_CODES error)
        {
            try
            {
                webSocketClient.ReceiveAsync();
            }
            catch (Exception)
            {
                return -1;
            }

            return 0;
        }
        private int WebSocketDataReceived(byte[] bytesIn, uint length, WebSocketClient.WEBSOCKET_PACKET_TYPES packetType, WebSocketClient.WEBSOCKET_RESULT_CODES results)
        {
            try
            {
                string incomingStream = Encoding.UTF8.GetString(bytesIn, 0, bytesIn.Length);
                PushBulletStream pushBulletSteamIn = JsonConvert.DeserializeObject<PushBulletStream>(incomingStream);
                if (pushBulletSteamIn.subtype != null)
                {
                    PushBulletGetMissedPushes(doorbellAccessToken, doorbellIden); 
                }
                webSocketClient.ReceiveAsync();
            }
            catch (Exception e)
            {
                ErrorLog.Error("SocketDataReceived Error: {0}", e.Message);
                return -1;
            }
            return 0;
        }
    }
}
