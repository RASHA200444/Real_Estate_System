//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using Microsoft.EntityFrameworkCore;
//using otherServices.Models;
//using System;

//namespace otherServices.Repositories
//{
//    public class UnitOfWork : IUnitOfWork
//    {
//        private readonly AppDbContext2 _context;

//        public IGenericRepository<User> Users { get; }
//        public IGenericRepository<Proposal> Proposals { get; }
//        public IGenericRepository<Post> Posts { get; }
//        public IGenericRepository<PostImage> PostImages { get; }
//        public IGenericRepository<Comment> Comments { get; }
//        public IGenericRepository<Transaction> Transactions { get; }
//        public IGenericRepository<SavedPost> SavedPosts { get; }
//        public IGenericRepository<Notification> Notifications { get; }
//        public IGenericRepository<Complaint> Complaints { get; }
//        public IGenericRepository<Admin> Admins { get; }
//        public IGenericRepository<Message> Messages { get; }
//        public IRatingsRepository Ratings { get; }
//        //public IGenericRepository<Landlord> Landlords { get; }

//        public ILikeRepository Likes { get; }
//        public ILandlordRepository Landlords { get; }

//        public UnitOfWork(AppDbContext2 context)
//        {
//            _context = context;

//            Users = new GenericRepository<User>(_context);
//            //Landlords = new GenericRepository<Landlord>(_context);
//            Posts = new GenericRepository<Post>(_context);
//            PostImages = new GenericRepository<PostImage>(_context);
//            Comments = new GenericRepository<Comment>(_context);
//            Transactions = new GenericRepository<Transaction>(_context);
//            SavedPosts = new GenericRepository<SavedPost>(_context);
//            Notifications = new GenericRepository<Notification>(_context);
//            Complaints = new GenericRepository<Complaint>(_context);
//            Admins = new GenericRepository<Admin>(_context);
//            Likes = new LikeRepository(_context);
//            Landlords = new LandlordRepository(_context);
//            Ratings = new RatingsRepository(_context);
//        }
//        public async Task<int> CompleteAsync()
//        {
//            return await _context.SaveChangesAsync();
//        }

//        public void Dispose()
//        {
//            _context.Dispose();
//        }
//    }
//}
