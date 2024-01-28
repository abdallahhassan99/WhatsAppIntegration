using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WhatsAppIntegration.Dtos;
using System.Net.Http.Headers;
using WhatsAppIntegration.Settings;
using Microsoft.Extensions.Options;
using WhatsAppIntegration.Models;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Net.Http;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Cors;
using CorePush.Google;
using Microsoft.AspNetCore.Builder.Extensions;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using System.Text.Json;
using Newtonsoft.Json;
using System.Net;
using Microsoft.AspNetCore.SignalR;

namespace WhatsAppIntegration.Controllers
{
    //    [Authorize(Policy = "JwtBearer")]
    //[Authorize]
    [EnableCors("AllowAll")]

    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly WhatsAppSettings _settings;
        static bool Flag = false;
        private readonly IHubContext<NotificationHub> _hubContext;

        public AuthController(IOptions<WhatsAppSettings> settings , IHubContext<NotificationHub> hubContext)
        {
            _settings = settings.Value;
            _hubContext = hubContext;

            //if (Flag == false)
            //{
            //    FirebaseApp.Create(new AppOptions
            //    {
            //        Credential = GoogleCredential.FromFile(@"C:\MyProjects\WhatsAppIntegration\WhatsAppIntegration\firebase-credentials.json")
            //    });
            //    Flag = true;
            //}
        }

        [HttpPost("send-welcome-message")]
        public async Task<IActionResult> SendWelcomeMessage(SendMessageDto dto)
        {   
            
            var language = Request.Headers["Language"].ToString();
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",_settings.Token);
            var body = new WhatsAppRequest()
            {
                to = dto.Mobile,
                template=new Template
                {
                    name="hello_world",
                    language=new Language { code= language },
                }
            };
            HttpResponseMessage response= await client.PostAsJsonAsync(new Uri(_settings.ApiUrl),body);
            if(!response.IsSuccessStatusCode)
            {
                throw new Exception("Something went wrong");
            }
            return Ok(response);
        }


        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.Token);
                var body = new WhatsAppRequest()
                { type = "text",
                    to = request.PhoneNumber,
                    text =new Models.Text
                    {
                       body=request.Message
                    }
                };
                HttpResponseMessage response = await client.PostAsJsonAsync(new Uri(_settings.ApiUrl), body);

                // Check the response and handle accordingly
                if (response.IsSuccessStatusCode)
                {
                    // Message sent successfully
                    return Ok(new { Status = "Success" });
                }
                else
                {
                    // Handle the error response
                    return StatusCode((int)response.StatusCode, new { Status = "Error", Message = await response.Content.ReadAsStringAsync() });
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
    














    const string VerfifyToken = "1234";

        [HttpGet("webhook")]
        public ActionResult<string> SetupWebHook([FromQuery(Name = "hub.mode")] string hubMode,
                                                 [FromQuery(Name = "hub.challenge")] int hubChallenge,
                                                 [FromQuery(Name = "hub.verify_token")] string hubVerifyToken)
        {
            Console.WriteLine("█ WebHook with get executed. ");
            Console.WriteLine($"█ Parameters: hub_mode={hubMode}  hub_challenge={hubChallenge}  hub_verify_token={hubVerifyToken}");
            if (!hubVerifyToken.Equals(VerfifyToken))
            {
                return Forbid("VerifyToken doesn't match");
            }
            return Ok(hubChallenge);
        }

        [HttpPost("webhook")]
        public IActionResult Webhook([FromBody] JObject payload)
        {
            try
            {
                string body = null;
                WebhookPayload WebhookPayload = payload.ToObject<WebhookPayload>();
                List<Entry> entries = WebhookPayload.Entry;
                if (entries.Count > 0)
                {
                    foreach (var entry in entries)
                    {
                        string entryId = entry.Id;
                        List<Change> changes = entry.Changes;
                        if (changes.Count > 0)
                        {
                            foreach (var change in changes)
                            {
                                string field = change.Field;
                                Value value = change.Value;

                                // Access specific properties based on your needs
                                string messagingProduct = value.MessagingProduct;
                                Metadata metadata = value.Metadata;
                                List<Contact> contacts = value.Contacts;
                                List<Message> messages = value.Messages;
                                if (messages!=null && messages.Count > 0)
                                {
                                    foreach (var message in messages)
                                    {
                                        string messageId = message.Id;
                                        string from = message.From;
                                        string timestamp = message.Timestamp;
                                        TextMessage text = message.Text;
                                        body = text?.Body;

                                        // Add your logic to process the extracted properties
                                    }
                                }
                            }
                        }
                    }
                    // Process the incoming WhatsApp message payload here
                    // You may want to validate the payload and handle the message accordingly
                }
                // Example: Log the received payload
                if (body != null)
                    {
                    //SendFirebaseMessage2(body);
                   var a= _hubContext.Clients.All.SendAsync("ReceiveNotification", body);
                }
                // Example: Respond with a success message
                return Ok(new { Status = "Success" });
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }

        public static void SendFirebaseMessage2(string body1)
        {

            WebRequest tRequest = WebRequest.Create("https://fcm.googleapis.com/fcm/send");
            tRequest.Method = "post";
            //serverKey - Key from Firebase cloud messaging server  
            tRequest.Headers.Add(string.Format("Authorization: key={0}", "AAAAarCf9Xw:APA91bHezBOuHXr_41Rb5VzIHvdtHZz5k3lx06P6yjd5-6BJ20q_28GnSJR8oV2iPSenwk4ZFg7yeQ-64qKZcjA-Kqqz8MLZ1UjzTxvVoxx4PZ3ecEphzhAAvjmwgrNr1dkyKEXEZBpc"));
            //Sender Id - From firebase project setting  
           // tRequest.Headers.Add(string.Format("Sender: id={0}", "458229806460"));
            tRequest.ContentType = "application/json";
            var payload = new
            {
                to = "caG-fjmCOBGEP6cPRAis6h:APA91bEtdghwgdKaPOcH6hrgsVOhHuzCbTDpUl-SahX1oXwQGqAGBLciqDX9NKj1Jn49utbPnqFSp7bIrtJip3oMRpZvB8n4zt22RURJAzeT00alDhw_VeaQnGu3uUGbFvb8gCJxrcWy",
              
                notification = new
                {
                    body = body1,
                    title = "Test",
                }

            };
            string postbody = JsonConvert.SerializeObject(payload).ToString();
            Byte[] byteArray = Encoding.UTF8.GetBytes(postbody);
            tRequest.ContentLength = byteArray.Length;
            using (Stream dataStream = tRequest.GetRequestStream())
            {
                dataStream.Write(byteArray, 0, byteArray.Length);
                using (WebResponse tResponse = tRequest.GetResponse())
                {
                    using (Stream dataStreamResponse = tResponse.GetResponseStream())
                    {
                        if (dataStreamResponse != null) using (StreamReader tReader = new StreamReader(dataStreamResponse))
                            {
                                String sResponseFromServer = tReader.ReadToEnd();
                                //result.Response = sResponseFromServer;
                            }
                    }
                }
            }
        }

        public static void SendFirebaseMessage(string body)
        {

             var message = new FirebaseAdmin.Messaging.Message
            {
                Token = "caG-fjmCOBGEP6cPRAis6h:APA91bEtdghwgdKaPOcH6hrgsVOhHuzCbTDpUl-SahX1oXwQGqAGBLciqDX9NKj1Jn49utbPnqFSp7bIrtJip3oMRpZvB8n4zt22RURJAzeT00alDhw_VeaQnGu3uUGbFvb8gCJxrcWy",
                Notification = new Notification
                {
                    Title = "Title",
                    Body = body
                }
                ,

                 Data = new Dictionary<string, string>
                    {
                        {"Content-Type", "application/json"},
                        {"Authorization", "key=AAAAarCf9Xw:APA91bHezBOuHXr_41Rb5VzIHvdtHZz5k3lx06P6yjd5-6BJ20q_28GnSJR8oV2iPSenwk4ZFg7yeQ-64qKZcjA-Kqqz8MLZ1UjzTxvVoxx4PZ3ecEphzhAAvjmwgrNr1dkyKEXEZBpc"}
                        // Add more custom headers as needed
                    },

             };

            var response = FirebaseMessaging.DefaultInstance.SendAsync(message);
            if (response != null && response.IsCompletedSuccessfully)
            {
                Console.WriteLine("Push message sent successfully!");
            }
            else
            {
                Console.WriteLine("Failed to send push message:", response?.ToString());
            }
        }

    }
}

public class WebhookPayload
{
    public string Object { get; set; }
    public List<Entry> Entry { get; set; }
}
public class Entry
{
    public string Id { get; set; }
    public List<Change> Changes { get; set; }
}

public class Change
{
    public Value Value { get; set; }
    public string Field { get; set; }
}

public class Value
{
    public string MessagingProduct { get; set; }
    public Metadata Metadata { get; set; }
    public List<Contact> Contacts { get; set; }
    public List<Message> Messages { get; set; }
}

public class Metadata
{
    public string DisplayPhoneNumber { get; set; }
    public string PhoneNumberId { get; set; }
}

public class Contact
{
    public Profile Profile { get; set; }
    public string WaId { get; set; }
}

public class Profile
{
    public string Name { get; set; }
}

public class Message
{
    public string From { get; set; }
    public string Id { get; set; }
    public string Timestamp { get; set; }
    public TextMessage Text { get; set; }
    public string Type { get; set; }
}

public class TextMessage
{
    public string Body { get; set; }
}