using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using otherServices.Models;

namespace otherServices.ModelsConfiguration
{
    public class ContractSignatureConfiguration : IEntityTypeConfiguration<ContractSignature>
    {
        public void Configure(EntityTypeBuilder<ContractSignature> builder)
        {
            builder.HasKey(x => x.ContractSignatureId);

            builder.Property(x => x.ContractHash).IsRequired().HasMaxLength(64);
            builder.Property(x => x.SignatureAlgo).IsRequired().HasMaxLength(32);
            builder.Property(x => x.SignatureValue).IsRequired();

            builder.HasIndex(x => new { x.ContractId, x.SignerUserId }).IsUnique();
        }
    }
}
