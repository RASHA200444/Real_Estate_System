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
using WebAPIDotNet.DTOs;
using WebAPIDotNet.Services;

namespace otherServices.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILandlordRepository _landlordRepository;
        private readonly IJwtService _jwtService;
        private readonly IWebHostEnvironment _env;
        private readonly IMediaService _mediaService;
        private readonly IPasswordHasher _hasher;
        private readonly IGenericRepository<Company> _companyRepository;


        public AuthService(
            IJwtService jwtService,
            IUserRepository userRepository,
            ILandlordRepository landlordRepository,
            IGenericRepository<Company> companyRepository,
            IWebHostEnvironment env,
            IMediaService mediaService,
            IPasswordHasher hasher)
        {
            _jwtService = jwtService;
            _userRepository = userRepository;
            _landlordRepository = landlordRepository;
            _companyRepository = companyRepository;
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


        public async Task<LoginResponseDTO> LoginAsync(LoginDTO loginDTO)
        {
            var users = await _userRepository.FindAsync(u =>
                 u.UserName == loginDTO.UsernameOrEmail || u.Email == loginDTO.UsernameOrEmail);

            var user = users.FirstOrDefault();
            if (user == null)
                throw new Exception("Username or Email doesn't exists");

            bool isPasswordValid = _hasher.Verify(user.Password, loginDTO.Password);
            if (!isPasswordValid)
                throw new Exception("Invalid password");

            int? landlordStatus = null;

            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = await _landlordRepository.FindAsync(l => l.UserId == user.UserId);
                var landlordEntity = landlord.FirstOrDefault();

                if (landlordEntity != null)
                {
                    landlordStatus = (int)landlordEntity.PendingStatus;

                    if (landlordEntity.PendingStatus != PendingStatus.Active)
                        throw new Exception("User not active");
                }
            }

            // ✅ Company: ممنوع تدخل قبل موافقة الأدمن
            if (user.RoleName == UserRole.Company)
            {
                var companies = await _companyRepository.FindAsync(c => c.UserId == user.UserId);
                var companyEntity = companies.FirstOrDefault();

                // لو مش موجود أصلاً يبقى فيه مشكلة في الداتا
                if (companyEntity == null)
                    throw new Exception("Company profile not found");

                if (companyEntity.PendingStatus != PendingStatus.Active)
                    throw new Exception("User not active");
            }

            var token = _jwtService.GenerateJwtToken(user.UserName, user.RoleName.ToString());

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
            var usernameExists = (await _userRepository.FindAsync(u => u.UserName == registerDto.UserName)).Any();
            if (usernameExists)
                throw new Exception("Username already exists");

            var emailExists = (await _userRepository.FindAsync(u => u.Email == registerDto.Email)).Any();
            if (emailExists)
                throw new Exception("Email already exists");

            if (registerDto.Role_name == UserRole.Admin)
                throw new Exception("Sign up as an admin is Forbidden");

            if (registerDto.Role_name == UserRole.Tenant && registerDto.File != null)
                throw new Exception("As a Tenant You shouldn't upload an ownership document");

            string? filePath = null;
            string? NIDPath = null;

            if (registerDto.Role_name == UserRole.Landlord)
            {
                if (registerDto.File == null)
                    throw new Exception("As a Landlord You should upload an ownership document");

                filePath = await _mediaService.SaveFileAsync(registerDto.File);

                if (registerDto.NIDFile == null)
                    throw new Exception("As Landlord 'NID File' is required.");

                NIDPath = await _mediaService.SaveFileAsync(registerDto.NIDFile);
            }


            var user = new User
            {
                UserName = registerDto.UserName,
                Email = registerDto.Email,
                Password = _hasher.Hash(registerDto.Password),
                RoleName = registerDto.Role_name,
                NIDPath = NIDPath,
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            int flagWaitingUser = 0;

            // ✅ Landlord flow
            if (user.RoleName == UserRole.Landlord)
            {
                var landlord = new Landlord
                {
                    UserId = user.UserId,
                    PendingStatus = PendingStatus.Pending,
                    OwnershipDocPath = filePath,
                    Rate = 0,
                    IsPro = false
                };

                await _landlordRepository.AddAsync(landlord);
                await _landlordRepository.SaveChangesAsync();

                flagWaitingUser = (int)landlord.PendingStatus;
            }

            // ✅ Company flow
            if (user.RoleName == UserRole.Company)
            {
                // validate company fields
                if (string.IsNullOrWhiteSpace(registerDto.CompanyName))
                    throw new Exception("CompanyName is required for Company registration");

                if (registerDto.CommercialRegisterFile == null)
                    throw new Exception("Commercial register document is required for Company registration");

                // 1) publisher landlord record
                var publisher = new Landlord
                {
                    UserId = user.UserId,
                    PendingStatus = PendingStatus.Pending,
                    OwnershipDocPath = null,
                    Rate = 0,
                    IsPro = false
                };

                await _landlordRepository.AddAsync(publisher);
                await _landlordRepository.SaveChangesAsync();

                // 2) company row
                var commercialPath = await _mediaService.SaveFileAsync(registerDto.CommercialRegisterFile);

                var company = new Company
                {
                    UserId = user.UserId,
                    CompanyName = registerDto.CompanyName,
                    CommercialRegisterPath = commercialPath,
                    CommercialRegisterEvaluation = AIDecision.Uncertain,
                    PendingStatus = PendingStatus.Pending,
                    LandlordId = publisher.LandlordId
                };

                await _companyRepository.AddAsync(company);
                await _companyRepository.SaveChangesAsync();

                flagWaitingUser = (int)company.PendingStatus;

                // company doesn't use NID
                user.NIDPath = null;
                user.NIDEvaluation = AIDecision.NotReviewed;
                await _userRepository.SaveChangesAsync();
            }

            return new RegisterResponseDTO
            {
                UserId = user.UserId,
                Username = user.UserName,
                Email = user.Email,
                Role = user.RoleName,
                FlagWaitingUser = flagWaitingUser,
                NIDPath = user.NIDPath,
                FileName = filePath != null ? Path.GetFileName(filePath) : null
            };
        }


    }
}