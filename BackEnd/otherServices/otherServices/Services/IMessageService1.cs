using otherServices.Data_Project.Models;
using otherServices.Models;
using otherServices.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RentMate.Services
{
    public interface IMessageService
    {
        Task<MessageDTo> CreateMessageAsync(long senderId, CreateMessageDto messageDto,long receiverId);
        Task<List<MessageDTo>> GetConversationAsync(long userId, long otherUserId);
        Task<List<Landlord>> GetUserConversationsAsync(long userId);
    }
}