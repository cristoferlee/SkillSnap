using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SkillSnap.Api.Models;

namespace SkillSnap.Api.Data;

public class SkillSnapContext : IdentityDbContext<ApplicationUser>
{
    public SkillSnapContext(
        DbContextOptions<SkillSnapContext> options)
        : base(options)
    {
    }

    public DbSet<PortfolioUser> PortfolioUsers { get; set; }
        = null!;

    public DbSet<Project> Projects { get; set; }
        = null!;

    public DbSet<Skill> Skills { get; set; }
        = null!;

    public DbSet<ContactMessage> ContactMessages { get; set; }
        = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Project>()
            .ToTable("Projects", table =>
            {
                table.HasCheckConstraint(
                    "CK_Projects_LiveUrl_Length",
                    "\"LiveUrl\" IS NULL OR length(\"LiveUrl\") <= 2048");

                table.HasCheckConstraint(
                    "CK_Projects_RepositoryUrl_Length",
                    "\"RepositoryUrl\" IS NULL OR length(\"RepositoryUrl\") <= 2048");
            });

        modelBuilder.Entity<ProjectSkill>(entity =>
        {
            entity.HasKey(projectSkill => new
            {
                projectSkill.ProjectId,
                projectSkill.SkillId
            });

            entity.HasOne(projectSkill => projectSkill.Project)
                .WithMany(project => project.ProjectSkills)
                .HasForeignKey(projectSkill => projectSkill.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(projectSkill => projectSkill.Skill)
                .WithMany(skill => skill.ProjectSkills)
                .HasForeignKey(projectSkill => projectSkill.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactMessage>()
            .ToTable("ContactMessages", table =>
            {
                table.HasCheckConstraint(
                    "CK_ContactMessages_Name_Length",
                    "length(trim(\"Name\")) BETWEEN 2 AND 100");

                table.HasCheckConstraint(
                    "CK_ContactMessages_Name_NoDigits",
                    "\"Name\" NOT GLOB '*[0-9]*'");

                table.HasCheckConstraint(
                    "CK_ContactMessages_Email_Length",
                    "length(trim(\"Email\")) BETWEEN 3 AND 254");

                table.HasCheckConstraint(
                    "CK_ContactMessages_Email_Format",
                    "instr(\"Email\", '@') > 1");

                table.HasCheckConstraint(
                    "CK_ContactMessages_Message_Length",
                    "length(trim(\"Message\")) BETWEEN 10 AND 2000");
            });
    }
}
