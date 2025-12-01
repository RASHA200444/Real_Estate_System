using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using otherServices.Data_Project.Models;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;
using WebAPIDotNet.DTOs;

namespace WebAPIDotNet.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILandlordRepository _landlordRepository;
        private readonly IJwtService _jwtService;
        private readonly IWebHostEnvironment _env;
        private readonly IMediaService _mediaService;
        private readonly IPasswordHasher _hasher;

        public AuthService(
            IJwtService jwtService,
            IUserRepository userRepository,
            ILandlordRepository landlordRepository,
            IWebHostEnvironment env,
            IMediaService mediaService,
            IPasswordHasher hasher)
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _landlordRepository = landlordRepository;
            _env = env;
            _mediaService = mediaService;
            _hasher = hasher;
        }


        public async Task<LoginResponseDTO> GetUserLoginDataAsync(LoginDTO loginDTO)
        {
            var users = await _userRepository.FindAsync(u =>
                (u.UserName == loginDTO.UsernameOrEmail || u.Email == loginDTO.UsernameOrEmail) &&
                u.Password == loginDTO.Password);

            var user = users.FirstOrDefault();
            if (user == null)
                return null;

            var token = _jwtService.GenerateJwtToken(user.UserName, user.RoleName.ToString());

            //int? landlordStatus = null;
            //if (user.RoleName == UserRole.Landlord)
            //{
            //    var landlord = await _landlordRepository.FindAsync(l => l.UserId == user.UserId);
            //    landlordStatus = landlord.FirstOrDefault()?.PendingStatus.GetHashCode();
            //}


            int? landlordStatus = null;
            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = await _landlordRepository.FindAsync(l => l.UserId == user.UserId);
                var landlordEntity = landlord.FirstOrDefault();
                if (landlordEntity != null)
                {
                    landlordStatus = (int)landlordEntity.PendingStatus;

                    // لو مش Active نرفض الدخول
                    if (landlordEntity.PendingStatus != PendingStatus.Active)
                    {
                        throw new Exception("User not active"); // أو ترمي Exception برسالة "User not active"
                    }
                }
            }


            return new LoginResponseDTO
            {
                Token = token,
                User = new UserDataDTO
                {
                    _id = user.UserId.ToString(),
                    Name = user.UserName,
                    Email = user.Email,
                    Role = user.RoleName.ToString(),
                    LandlordStatus = landlordStatus
                }
            };
        }
        
        
        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            var users = await _userRepository.FindAsync(u =>
                 u.UserName == loginDTO.UsernameOrEmail || u.Email == loginDTO.UsernameOrEmail);

            var user = users.FirstOrDefault();
            if (user == null)
                throw new Exception("Username or Email doesn't exists");

            // هنا بنعمل تحقق من الباسورد
            bool isPasswordValid = _hasher.Verify(user.Password, loginDTO.Password );
            if (!isPasswordValid)
                throw new Exception("Invalid password");

            var token = _jwtService.GenerateJwtToken(user.UserName, user.RoleName.ToString());

            int? landlordStatus = null;
            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = await _landlordRepository.FindAsync(l => l.UserId == user.UserId);
                var landlordEntity = landlord.FirstOrDefault();
                if (landlordEntity != null)
                {
                    landlordStatus = (int)landlordEntity.PendingStatus;

                    if (landlordEntity.PendingStatus != PendingStatus.Active)
                    {
                        throw new Exception("User not active"); 
                    }
                }
            }

            return new LoginResponseDTO
            {
                Token = token,
                User = new UserDataDTO
                {
                    _id = user.UserId.ToString(),
                    Name = user.UserName,
                    Email = user.Email,
                    Role = user.RoleName.ToString(),
                    LandlordStatus = landlordStatus
                }
            };
        }

        public async Task<RegisterResponseDTO> Register(RegisterDTO registerDto)
        {
            if (registerDto.Password != registerDto.ConfirmPassword)
                throw new Exception("Password and Confirm Password do not match");

            // 1️⃣ التحقق من وجود Username أو Email مسبقًا
            var usernameExists = (await _userRepository.FindAsync(u => u.UserName == registerDto.Username)).Any();
            if (usernameExists)
                throw new Exception("Username already exists");

            var emailExists = (await _userRepository.FindAsync(u => u.Email == registerDto.Email)).Any();
            if (emailExists)
                throw new Exception("Email already exists");

            // 2️⃣ حفظ الملف لو موجود باستخدام MediaService
            string? filePath = null;
            if (registerDto.Role_name == UserRole.Landlord && registerDto.File == null)
            {
                throw new Exception("As a Landlord You should upload an ownership document");
            }

            if (registerDto.Role_name == UserRole.Landlord && registerDto.File != null)
            {
                filePath = await _mediaService.SaveFileAsync(registerDto.File);
            }
            string NIDPath = null;
            NIDPath = await _mediaService.SaveFileAsync(registerDto.NIDFile);


            // 3️⃣ إنشاء الـ User
            var user = new User
            {
                UserName = registerDto.Username,
                Email = registerDto.Email,
                Password = _hasher.Hash(registerDto.Password),   
                RoleName = registerDto.Role_name, // UserRole
                NIDPath = NIDPath,
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            int flagWaitingUser = 0;

            // 4️⃣ إذا كان الدور Landlord، نضيف Landlord profile
            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = new Landlord
                {
                    UserId = user.UserId,
                    PendingStatus = PendingStatus.Pending, // الحالة الافتراضية
                    OwnershipDocPath = filePath,           
                    Rate = 0,
                    IsPro = false
                };

                await _landlordRepository.AddAsync(landlord);
                await _landlordRepository.SaveChangesAsync();

                // نرجع PendingStatus كرقم للـ DTO
                flagWaitingUser = (int)landlord.PendingStatus;
            }

            // 5️⃣ تجهيز الـ DTO بنفس أسماء الحقول القديمة
            return new RegisterResponseDTO
            {
                UserId = user.UserId,
                Username = user.UserName,
                Email = user.Email,
                Role = user.RoleName,              // كما في الـ API القديم
                FlagWaitingUser = flagWaitingUser, // من Landlord.PendingStatus
                NIDPath = user.NIDPath ,
                FileName = filePath != null ? Path.GetFileName(filePath) : null
            };
        }





        //public async Task<RegisterResponseDTO> Register(RegisterDTO registerDto)
        //{

        //    var usernameExists = (await _userRepository.FindAsync(u => u.UserName == registerDto.Username)).Any();
        //    if (usernameExists)
        //        throw new Exception("Username already exists");

        //    var emailExists = (await _userRepository.FindAsync(u => u.Email == registerDto.Email)).Any();
        //    if (emailExists)
        //        throw new Exception("Email already exists");

        //    string? filePath = null;

        //    if (registerDto.Role_name == "landlord" && registerDto.File != null)
        //    {
        //        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles");

        //        if (!Directory.Exists(uploadsFolder))
        //        {
        //            Directory.CreateDirectory(uploadsFolder);
        //        }

        //        string uploadPath = Path.Combine(_env.ContentRootPath, "../Media");
        //        Directory.CreateDirectory(uploadPath);

        //        string uniqueFileName = Guid.NewGuid() + Path.GetExtension(registerDto.File.FileName);
        //        filePath = Path.Combine(uploadPath, uniqueFileName);

        //        using (var stream = new FileStream(filePath, FileMode.Create))
        //        {
        //            await registerDto.File.CopyToAsync(stream);
        //        }
        //    }
        //    var user = new User
        //    {
        //        UserName = registerDto.Username,
        //        Email = registerDto.Email,
        //        Pass = registerDto.Password,
        //        RoleName = registerDto.Role_name,
        //        FlagWaitingUser = registerDto.Role_name == "landlord" ? 1 : 0,
        //        FName = string.Empty,
        //        LName = string.Empty,
        //        FilePath = filePath
        //    };

        //    await _userRepository.AddAsync(user);
        //    await _userRepository.SaveChangesAsync();
        //    //var kafkaMessage = new
        //    //{
        //    //    username = registerDto.Username,
        //    //    email = registerDto.Email,
        //    //    status = registerDto.Role_name == "landlord" ? "pending" : "succes",
        //    //    DateMessage = DateTime.Now,

        //    //};


        //    //string jsonMessage = JsonConvert.SerializeObject(kafkaMessage);

        //    //// 3. Send to Kafka topic "messages"
        //    //var config = new ProducerConfig
        //    //{
        //    //    BootstrapServers = "localhost:9092"
        //    //};

        //    //using var producer = new ProducerBuilder<Null, string>(config).Build();
        //    //await producer.ProduceAsync("WelcomeEmail", new Message<Null, string> { Value = jsonMessage }); // we send

        //    ////return Ok(new { status = "Message saved and sent to Kafka" });


        //    return new RegisterResponseDTO
        //    {
        //        UserId = user.UserId,
        //        Username = user.UserName,
        //        Email = user.Email,
        //        Role = user.RoleName,
        //        FlagWaitingUser = user.FlagWaitingUser,
        //    };
        //}



        //var kafkaMessage = new
        //{
        //    username = registerDto.Username,
        //    email = registerDto.Email,
        //    status = registerDto.Role_name == "landlord" ? "pending" : "succes",
        //    DateMessage = DateTime.Now,

        //};


        //string jsonMessage = JsonConvert.SerializeObject(kafkaMessage);

        //// 3. Send to Kafka topic "messages"
        //var config = new ProducerConfig
        //{
        //    BootstrapServers = "localhost:9092"
        //};

        //using var producer = new ProducerBuilder<Null, string>(config).Build();
        //await producer.ProduceAsync("WelcomeEmail", new Message<Null, string> { Value = jsonMessage }); // we send

        ////return Ok(new { status = "Message saved and sent to Kafka" });






    }
}














//using System;
//using System.Threading.Tasks;
//using Confluent.Kafka;
//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using otherServices.Data_Project.Models;
//using otherServices.Models;
//using otherServices.Models.DTOs;
//using otherServices.Repositories;
//using otherServices.Services;
//using WebAPIDotNet.DTOs;
//namespace WebAPIDotNet.Services { public class AuthService : IAuthService { private readonly IJwtService _jwtService; private readonly ILandlordRepository _userRepository; private readonly IWebHostEnvironment _env; public AuthService(IJwtService jwtService, ILandlordRepository userRepository, IWebHostEnvironment env) { _jwtService = jwtService; _userRepository = userRepository; _env = env; } 
//        public async Task<LoginResponseDTO> GetUserLoginDataAsync(LoginDTO loginDTO) { var users = await _userRepository.FindAsync(u => (u.UserName == loginDTO.UsernameOrEmail || u.Email == loginDTO.UsernameOrEmail) && u.Pass == loginDTO.Password); var user = users.FirstOrDefault(); if (user == null) return null; var token = _jwtService.GenerateJwtToken(user.UserName, user.RoleName); return new LoginResponseDTO { Token = token, User = new UserDataDTO { _id = user.UserId.ToString(), Name = user.UserName, Email = user.Email, Role = user.RoleName } }; } 
//        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO) { var users = await _userRepository.FindAsync(u => (u.UserName == loginDTO.UsernameOrEmail || u.Email == loginDTO.UsernameOrEmail) && u.Pass == loginDTO.Password); var user = users.FirstOrDefault(); if (user == null) return null; var token = _jwtService.GenerateJwtToken(user.UserName, user.RoleName); int? landlordStatus = null; if (user.RoleName == "landlord") { var landlord = await _userRepository.GetByIdAsync(user.UserId); landlordStatus = landlord?.FlagWaitingUser; } return new LoginResponseDTO { Token = token, User = new UserDataDTO { _id = user.UserId.ToString(), Name = user.UserName, Email = user.Email, Role = user.RoleName, LandlordStatus = user.FlagWaitingUser } }; } 
//        public async Task<RegisterResponseDTO> Register(RegisterDTO registerDto) { var usernameExists = (await _userRepository.FindAsync(u => u.UserName == registerDto.Username)).Any(); if (usernameExists) throw new Exception("Username already exists"); var emailExists = (await _userRepository.FindAsync(u => u.Email == registerDto.Email)).Any(); if (emailExists) throw new Exception("Email already exists"); string? filePath = null; if (registerDto.Role_name == "landlord" && registerDto.File != null) { var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "UploadedFiles"); if (!Directory.Exists(uploadsFolder)) { Directory.CreateDirectory(uploadsFolder); } string uploadPath = Path.Combine(_env.ContentRootPath, "../Media"); Directory.CreateDirectory(uploadPath); string uniqueFileName = Guid.NewGuid() + Path.GetExtension(registerDto.File.FileName); filePath = Path.Combine(uploadPath, uniqueFileName); using (var stream = new FileStream(filePath, FileMode.Create)) { await registerDto.File.CopyToAsync(stream); } } var user = new User { UserName = registerDto.Username, Email = registerDto.Email, Pass = registerDto.Password, RoleName = registerDto.Role_name, FlagWaitingUser = registerDto.Role_name == "landlord" ? 1 : 0, FName = string.Empty, LName = string.Empty, FilePath = filePath }; await _userRepository.AddAsync(user); await _userRepository.SaveChangesAsync(); //var kafkaMessage = new //{ // username = registerDto.Username, // email = registerDto.Email, // status = registerDto.Role_name == "landlord" ? "pending" : "succes", // DateMessage = DateTime.Now, //}; //string jsonMessage = JsonConvert.SerializeObject(kafkaMessage); //// 3. Send to Kafka topic "messages" //var config = new ProducerConfig //{ // BootstrapServers = "localhost:9092" //}; //using var producer = new ProducerBuilder<Null, string>(config).Build(); //await producer.ProduceAsync("WelcomeEmail", new Message<Null, string> { Value = jsonMessage }); // we send ////return Ok(new { status = "Message saved and sent to Kafka" }); return new RegisterResponseDTO { UserId = user.UserId, Username = user.UserName, Email = user.Email, Role = user.RoleName, FlagWaitingUser = user.FlagWaitingUser, }; } } }