﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
//using System.Net.WebSockets;
using WebSocketSharp;


namespace Amino
{
    public class Client
    {
        public string deviceID { get; private set; } 
        public string sessionID { get; private set; }
        public string secret { get; private set; }
        public string userID { get; private set; }
        public string json { get; private set; }
        public string googleID { get; private set; }
        public string appleID { get; private set; }
        public string facebookID { get; private set; }
        public string twitterID { get; private set; }
        public string iconURL { get; private set; }
        public string aminoID { get; private set; }
        public string email { get; private set; }
        public string phoneNumber { get; private set; }
        public string nickname { get; private set; }
        public bool is_Global { get; private set; }
        public bool debug { get; set; } = false;

        //public bool renew_device { get; }
        //public string signiture { get; }

        public IDictionary<string, string> headers = new Dictionary<string, string>();

        //Handles the header stuff
        private Task headerBuilder()
        {
            headers.Clear();
            headers.Add("NDCDEVICEID", deviceID);
            headers.Add("Accept-Language", "en-US");
            headers.Add("Content-Type", "application/json; charset=utf-8");
            headers.Add("Host", "service.narvii.com");
            headers.Add("Accept-Encoding", "gzip");
            headers.Add("Connection", "Keep-Alive");
            if(sessionID != null) { headers.Add("NDCAUTH", $"sid={sessionID}"); }

            return Task.CompletedTask;
        }

        public Client(string _deviceID = null)
        {
            if(_deviceID == null) { deviceID = helpers.generate_device_id(); } else { deviceID = _deviceID; }
            headerBuilder();
        }

        public Task request_verify_code(string email, bool resetPassword = false)
        {
           
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/auth/request-security-validation", Method.Post);
                if(!resetPassword) { request.AddJsonBody(new { identity = email, type = 1, deviceID = deviceID}); } else { request.AddJsonBody(new { identity = email, type = 1, deviceID = deviceID, level = 2, purpose = "reset-password"}); }
                request.AddHeaders(headers);
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;

            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Task Login(string _email, string _password, string _secret = null)
        {
            try
            {
                string secret;
                if (_secret == null) { secret = $"0 {_password}"; } else { secret = _secret; }
                var data = new { email = _email, v = 2, secret = secret, deviceID = deviceID, clientType = 100, action = "normal", timestamp = helpers.GetTimestamp() * 1000};

                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/auth/login", Method.Post);
                request.AddHeaders(headers);
                request.AddJsonBody(data);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                json = response.Content;
                dynamic jsonObj = (JObject)JsonConvert.DeserializeObject(json);
                try
                {
                    sessionID = (string)jsonObj["sid"];
                    this.secret = (string)jsonObj["secret"];
                    userID = (string)jsonObj["account"]["uid"];
                    googleID = (string)jsonObj["account"]["googleID"];
                    appleID = (string)jsonObj["account"]["appleID"];
                    facebookID = (string)jsonObj["account"]["facebookID"];
                    twitterID = (string)jsonObj["account"]["twitterID"];
                    iconURL = (string)jsonObj["userProfile"]["icon"];
                    aminoID = (string)jsonObj["account"]["aminoId"];
                    email = (string)jsonObj["account"]["email"];
                    phoneNumber = (string)jsonObj["account"]["phoneNumber"];
                    nickname = (string)jsonObj["userProfile"]["nickname"];
                    is_Global = (bool)jsonObj["userProfile"]["isGlobal"];
                }catch(Exception e) { throw new Exception(e.Message); }
                headerBuilder();
                WebSockets socket = new WebSockets(this);
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Task Register(string _name, string _email, string _password, string _verificationCode, string _deviceID = null)
        {
            try
            {
                if (_deviceID == null) { if (deviceID != null) { _deviceID = deviceID; } else { _deviceID = helpers.generate_device_id(); } }
                var data = new
                {
                    secret = $"0 {_password}",
                    deviceID = _deviceID,
                    email = _email,
                    clientType = 100,
                    nickname = _name,
                    latitude = 0,
                    longtitude = 0,
                    address = String.Empty,
                    clientCallbackURL = "narviiapp://relogin",
                    validationContext = new
                    {
                        data = new { code = _verificationCode },
                        type = 1,
                        identity = _email
                    },
                    type = 1,
                    identity = _email,
                    timestamp = helpers.GetTimestamp() * 1000
                };
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/auth/register");
                request.AddHeaders(headers);
                request.AddJsonBody(data);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public Task Restore_account(string _email, string _password, string _deviceID = null)
        {
            try
            {
                if (_deviceID == null) { if (deviceID != null) { _deviceID = deviceID; } else { _deviceID = helpers.generate_device_id(); } }
                var data = new { secret = $"0 {_password}", deviceID = _deviceID, email = _email, timestamp = helpers.GetTimestamp() * 1000 };
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/account/delete-request/cancel");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if(debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }
        public Task Delete_account(string _password)
        {
            var data = new { deviceID = deviceID, secret = $"0 {_password}" };
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/account/delete-request");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e)
            {
                throw new Exception(e.Message);
            }
        }
        public Task activate_account(string _email, string _verificationCode, string _deviceID = null)
        {
            if (_deviceID == null) { if (deviceID != null) { _deviceID = deviceID; } else { _deviceID = helpers.generate_device_id(); } }
            var data = new
            {
                type = 1,
                identity = _email,
                data = new { code = _verificationCode },
                deviceID = _deviceID
            };
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/auth/activate-email");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task configure_account(Types.account_gender _gender, int _age)
        {
            if(_age <= 12) { throw new Exception("The given account age is too low"); }
            int gender;
            switch(_gender)
            {
                case Types.account_gender.Male:
                    gender = 1;
                    break;
                case Types.account_gender.Female:
                    gender = 2;
                    break;
                case Types.account_gender.Non_Binary:
                    gender = 255;
                    break;
                default:
                    gender = 255;
                    break;
            }
            var data = new { age = _age, gender = gender, timestamp = helpers.GetTimestamp() * 1000 };
            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/persona/profile/basic");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            } catch(Exception e) { throw new Exception(e.Message); }
        }

        public Task Change_password(string _email, string _password, string _verificationCode)
        {
            var data = new
            {
                updateSecret = $"0 {_password}",
                emailValidationContext = new
                {
                    data = new { code = _verificationCode },
                    type = 1,
                    identity = _email,
                    level = 2,
                    deviceID = deviceID
                },
                phoneNumberValidationContext = String.Empty,
                deviceID = deviceID
            };

            try
            {
                RestClient client = new RestClient(helpers.BaseUrl);
                RestRequest request = new RestRequest("/g/s/auth/reset-password");
                request.AddHeaders(headers);
                request.AddHeader("NDC-MSG-SIG", helpers.generate_signiture(System.Text.Json.JsonSerializer.Serialize(data)));
                request.AddJsonBody(data);
                var response = client.ExecutePost(request);
                if ((int)response.StatusCode != 200) { throw new Exception(response.Content); }
                if (debug) { Trace.WriteLine(response.Content); }
                return Task.CompletedTask;
            }
            catch (Exception e) { throw new Exception(e.Message); }
        }
        internal class WebSockets
        {

            private IDictionary<string, string> client_headers = new Dictionary<string, string>();
            private IDictionary<string, string> ws_headers = new Dictionary<string, string>();
            private string WebSocketURL = "wss://ws1.narvii.com";

            public WebSockets(Amino.Client client)
            {
                client_headers = client.headers;
                var final = $"{client.deviceID}|{(int)helpers.GetTimestamp() * 1000}";
                ws_headers.Add("NDCDEVICEID", client.deviceID);
                ws_headers.Add("NDCAUTH", $"sid={client.sessionID}");
                ws_headers.Add("NDC-MSG-SIG", helpers.generate_signiture(final));

                startSocket(client);

            }

            private void startSocket(Amino.Client _client)
            {
                var final = $"{_client.deviceID}|{(int)helpers.GetTimestamp() * 1000}";
                /*
                ClientWebSocket client = new ClientWebSocket();
                client.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
                client.Options.SetRequestHeader("NDCDEVICEID", _client.deviceID);
                client.Options.SetRequestHeader("NDCAUTH", $"sid={_client.sessionID}");
                client.Options.SetRequestHeader("NDC-MSG-SIG", helpers.generate_signiture(final));
                client.ConnectAsync(new Uri($"{WebSocketURL}/?signbody={final.Replace("|", "%7")}"), System.Threading.CancellationToken.None); */



                using (var ws = new WebSocket($"{WebSocketURL}?signbody={final.Replace("|", "%7")}"))
                {
                    ws.OnMessage += (sender, e) => Console.WriteLine(e.Data);
                    ws.OnOpen += (sender, e) => Console.WriteLine("Opened!");
                    ws.OnError += (sender, e) => Console.WriteLine("Failed: " + e.Message);
                    ws.OnClose += (sender, e) => Console.WriteLine("Closed! " + e.Reason);
                    ws.SetCredentials("NDCDEVICEID", _client.deviceID, false);
                    ws.SetCredentials("NDCAUTH", $"sid={_client.sessionID}", false);
                    ws.SetCredentials("NDC-MSG-SIG", helpers.generate_signiture(final), false);
                    
                    
                    ws.Connect();
                }



            }
        }

    }
}
