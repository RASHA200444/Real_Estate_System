using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RentMate.Services;
using otherServices.Models;
using otherServices.Data_Project.Models;
using otherServices.Models.DTOs;
using Confluent.Kafka;
using Newtonsoft.Json;
using otherServices.Repositories;
namespace RentMate.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepository;
        private readonly ILandlordRepository _userRepository;

        public MessageService(IMessageRepository messageRepository,ILandlordRepository userRepository )
        {
            _messageRepository = messageRepository;
            _userRepository = userRepository;
        }

        public async Task<MessageDTo> CreateMessageAsync(long senderId, CreateMessageDto messageDto,long receiverId)
        {
            var sender = await _userRepository.GetByIdAsync(senderId);
            var receiver = await _userRepository.GetByIdAsync(receiverId);

            if (sender == null || receiver == null)
            {
                throw new ArgumentException("Invalid sender or receiver");
            }

            var message = new Message
            {
                SenderId = senderId,
                ReceiverId = receiverId,
                Message1 = messageDto.Content,
                DateMessge = DateTime.UtcNow,
                //IsRead = false
            };

            _messageRepository.AddAsync(message);
            await _messageRepository.SaveChangesAsync();

            var kafkaMessage = new
            {
                message = messageDto.Content,
                date_message = DateTime.Now,
                receiver_Id =receiverId,
                sender_Id = senderId,
            };


            string jsonMessage = JsonConvert.SerializeObject(kafkaMessage);

            // 3. Send to Kafka topic "messages"
            var config = new ProducerConfig
            {
                BootstrapServers = "localhost:9092"
            };

            using var producer = new ProducerBuilder<Null, string>(config).Build();
            await producer.ProduceAsync("receivedMessages", new Message<Null, string> { Value = jsonMessage }); // we send

            //return Ok(new { status = "Message saved and sent to Kafka" });
            return new MessageDTo
            {
                Id = message.MessageId,
                SenderId = message.SenderId,
                SenderName = sender.User.UserName,
                ReceiverId = message.ReceiverId,
                ReceiverName = receiver.User.UserName,
                Content = message.Message1,
                Timestamp = message.DateMessge,
                //IsRead = message.IsRead
            };
        }

        public async Task<List<MessageDTo>> GetConversationAsync(long userId, long otherUserId)
        {
            var messages = await _messageRepository.NestedFind(
                m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                     (m.SenderId == otherUserId && m.ReceiverId == userId),
                m => m.Sender,
                m => m.Receiver
            );

            return messages.OrderBy(m => m.DateMessge).Select(m => new MessageDTo
            {
                Id = m.MessageId,
                SenderId = m.SenderId,
                SenderName = m.Sender?.UserName,
                ReceiverId = m.ReceiverId,
                ReceiverName = m.Receiver?.UserName,
                Content = m.Message1,
                Timestamp = m.DateMessge,
            }).ToList();
        }

        public async Task<List<Landlord>> GetUserConversationsAsync(long userId)
        {
            var messages = await _messageRepository.FindAsync(
                m => m.SenderId == userId || m.ReceiverId == userId
            );

            var userIds = messages
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToList();

            var users = await _userRepository.NestedFind(
                u => userIds.Contains(u.UserId),
                u => u.User.SentMessages,
                u => u.User.ReceivedMessages
            );

            return users.ToList();
        }



    }
}