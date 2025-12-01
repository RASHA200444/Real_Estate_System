using otherServices.Models.Enums;
namespace otherServices.Models
{
    public class Admin
    {
        public long AdminId { get; set; }
        public long UserId { get; set; }   //  FK to User

        public AdminType Type { get; set; }  // AdminBySys, AdminByAdmin
        public AdminPrivilageType PrivilegeType { get; set; }  // Add / Update / Delete / All

        public User User { get; set; }
    }
}
