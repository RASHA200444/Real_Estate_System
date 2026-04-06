using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Projects;
using otherServices.Models.DTOs.Posts; // عشان الـ PostSummaryDto والـ AllPostsDto لو احتاجناهم

namespace otherServices.Services.Interfaces
{
    public interface ICompanyProjectService
    {
        // 1. إنشاء مشروع جديد مع النماذج (شغال تمام)
        Task<ProjectResponseDto> CreateProjectWithTemplates(CreateProjectWithTemplatesDto dto);

        // 2. حذف مشروع (بشرط عدم وجود شقق محجوزة/مباعة)
        Task<DeleteProjectResultDto> DeleteProject(long companyUserId, long projectId);

        // 3. جلب كل مشاريع الشركة (للعرض في الـ Dashboard)
        // استخدمنا ProjectDto لأنه شامل كل البيانات الأساسية
        Task<List<ProjectDto>> GetProjectsByCompany(long companyUserId);

        // 4. جلب تفاصيل مشروع معين بالوحدات بتاعته (للشركة)
        // بنرجع Object فيه بيانات المشروع + لستة من الـ PostSummaryDto لحالة الشقق
        Task<CompanyProjectFullDetailsDto> GetProjectDetailsForCompany(long companyUserId, long projectId);
    }

    // DTO بسيط للتغليف (Wrapper) عشان نبعت المشروع وشققه مع بعض في الـ GetById
    public class CompanyProjectFullDetailsDto
    {
        public ProjectDto Project { get; set; } = null!;
        public List<PostSummaryDto> Units { get; set; } = new();
    }
}