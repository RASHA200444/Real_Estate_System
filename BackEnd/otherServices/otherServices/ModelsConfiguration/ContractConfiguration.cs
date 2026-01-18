using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class ContractConfiguration : IEntityTypeConfiguration<Contract>
    {
        public void Configure(EntityTypeBuilder<Contract> builder)
        {
            builder.HasKey(x => x.ContractId);

            builder.Property(x => x.ContractJson).IsRequired();
            builder.Property(x => x.ContractHash).IsRequired().HasMaxLength(64);

            builder.HasIndex(x => x.ContractHash).IsUnique();

            builder.HasMany(x => x.Signatures)
                   .WithOne(s => s.Contract)
                   .HasForeignKey(s => s.ContractId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
