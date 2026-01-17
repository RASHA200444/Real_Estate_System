using Confluent.Kafka;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using otherServices.Data_Project.Models;
using otherServices.Data_Project.service;
using otherServices.Models;
using otherServices.Models.DTOs;
using RentMate.Services;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MessageController : ControllerBase
    {
        private readonly AppDbContext2 _db;
        private readonly KafkaProducerService _kafkaService;
        private readonly IMessageService _messageService;
        public MessageController(AppDbContext2 db, KafkaProducerService kafkaService,IMessageService messageService)
        {
            this._db = db;
            _kafkaService = kafkaService;
            _messageService = messageService;
        }
        [HttpGet("{userId}/conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(long userId,long otherUserId)
        {
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var messages = await _messageService.GetConversationAsync(userId, otherUserId);
            return Ok(messages);
        }

        // GET: api/Messages/conversations
        [HttpGet("{userId}/conversations")]
        public async Task<IActionResult> GetConversations(long userId)
        {
            //var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var conversations = await _messageService.GetUserConversationsAsync(userId);
            return Ok(conversations);
        }

        // POST: api/Messages
        [HttpPost("{senderId}/create-message/{receiverId}")]
        public async Task<IActionResult> SendMessage(long senderId,CreateMessageDto messageDto,long receiverId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var message = await _messageService.CreateMessageAsync(senderId, messageDto,receiverId);
            return CreatedAtAction(nameof(GetConversation), new { userId=senderId,otherUserId = receiverId }, message);
        }

        // PUT: api/Messages/read/{id}
        //[HttpPut("read/{id}")]
        //public async Task<IActionResult> MarkAsRead(int id)
        //{
        //    await _messageService.MarkAsReadAsync(id);
        //    return NoContent();
        //}

        // GET: api/Messages/unread
        //[HttpGet("unread")]
        //public async Task<IActionResult> GetUnreadCount()
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    var count = await _messageService.GetUnreadMessageCountAsync(userId);
        //    return Ok(new { unreadCount = count });
        //}

        //[HttpPost("sendMessage")]
        //public async Task<IActionResult> sendMessageAsync([FromBody] MessageDTo MessageDTo)
        //{
        //    var message = new Message() {
        //        SenderId= MessageDTo.SenderId,
        //        ReceiverId = MessageDTo.ReceiverId,
        //        Message1= MessageDTo.Content,
        //        DateMessge= DateTime.Now
        //    };
        //    _db.Messages.Add(message);
        //    _db.SaveChanges(); // we save in other service db


        //    var kafkaMessage = new
        //    {
        //        sender_Id = MessageDTo.SenderId,
        //        receiver_Id = MessageDTo.ReceiverId,
        //        message = MessageDTo.Content,
        //        date_message = DateTime.Now
        //    };


        //    string jsonMessage = JsonConvert.SerializeObject(kafkaMessage);

        //    // 3. Send to Kafka topic "messages"
        //    var config = new ProducerConfig
        //    {
        //        BootstrapServers = "localhost:9092"
        //    };

        //    using var producer = new ProducerBuilder<Null, string>(config).Build();
        //    await producer.ProduceAsync("messages", new Message<Null, string> { Value = jsonMessage }); // we send

        //    return Ok(new { status = "Message saved and sent to Kafka" });

        //}


    }
}
