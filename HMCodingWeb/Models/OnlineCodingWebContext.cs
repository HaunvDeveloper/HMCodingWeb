using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HMCodingWeb.Models;

public partial class OnlineCodingWebContext : DbContext
{
    public OnlineCodingWebContext()
    {
    }

    public OnlineCodingWebContext(DbContextOptions<OnlineCodingWebContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccessRole> AccessRoles { get; set; }

    public virtual DbSet<Authority> Authorities { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<Codepad> Codepads { get; set; }

    public virtual DbSet<CopyPasteHistory> CopyPasteHistories { get; set; }

    public virtual DbSet<DifficultyLevel> DifficultyLevels { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<ExerciseBelongType> ExerciseBelongTypes { get; set; }

    public virtual DbSet<ExerciseType> ExerciseTypes { get; set; }

    public virtual DbSet<Marking> Markings { get; set; }

    public virtual DbSet<MarkingDetail> MarkingDetails { get; set; }

    public virtual DbSet<ProgramLanguage> ProgramLanguages { get; set; }

    public virtual DbSet<Rank> Ranks { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccessRole>(entity =>
        {
            entity.ToTable("AccessRole");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AccessCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.AccessName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Authority>(entity =>
        {
            entity.ToTable("Authority");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AuthCode).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.ToTable("Chapter");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChapterCode).HasMaxLength(50);
            entity.Property(e => e.ChapterName).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(256);
        });

        modelBuilder.Entity<Codepad>(entity =>
        {
            entity.ToTable("Codepad");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreateDate).HasColumnType("datetime");
            entity.Property(e => e.FileName).HasMaxLength(50);
            entity.Property(e => e.InputFile)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.OutputFile)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.UpdateDate).HasColumnType("datetime");

            entity.HasOne(d => d.Access).WithMany(p => p.Codepads)
                .HasForeignKey(d => d.AccessId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Codepad_AccessRole");

            entity.HasOne(d => d.ProgramLanguage).WithMany(p => p.Codepads)
                .HasForeignKey(d => d.ProgramLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Codepad_ProgramLanguage");

            entity.HasOne(d => d.User).WithMany(p => p.Codepads)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Codepad_User");
        });

        modelBuilder.Entity<CopyPasteHistory>(entity =>
        {
            entity.ToTable("CopyPasteHistory");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.LaunchTime).HasColumnType("datetime");
            entity.Property(e => e.Type).HasMaxLength(10);
            entity.Property(e => e.Zone).HasMaxLength(10);

            entity.HasOne(d => d.Exercise).WithMany(p => p.CopyPasteHistories)
                .HasForeignKey(d => d.ExerciseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CopyPasteHistory_Exercise");

            entity.HasOne(d => d.User).WithMany(p => p.CopyPasteHistories)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_CopyPasteHistory_User");
        });

        modelBuilder.Entity<DifficultyLevel>(entity =>
        {
            entity.ToTable("DifficultyLevel");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DifficultyCode).HasMaxLength(50);
            entity.Property(e => e.DifficultyName).HasMaxLength(50);
        });

        modelBuilder.Entity<Exercise>(entity =>
        {
            entity.ToTable("Exercise");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedDate).HasColumnType("datetime");
            entity.Property(e => e.ExerciseCode)
                .HasMaxLength(50)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.ExerciseName).HasMaxLength(125);
            entity.Property(e => e.InputFile).HasMaxLength(50);
            entity.Property(e => e.KindMarking).HasMaxLength(20);
            entity.Property(e => e.OutputFile).HasMaxLength(50);
            entity.Property(e => e.TypeMarking).HasMaxLength(20);

            entity.HasOne(d => d.Access).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.AccessId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exercise_AccessRole");

            entity.HasOne(d => d.Chapter).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.ChapterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exercise_Chapter");

            entity.HasOne(d => d.Difficulty).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.DifficultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exercise_DifficultyLevel");

            entity.HasOne(d => d.UserCreated).WithMany(p => p.Exercises)
                .HasForeignKey(d => d.UserCreatedId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Exercise_User");
        });

        modelBuilder.Entity<ExerciseBelongType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK_ExerciseBelongType_1");

            entity.ToTable("ExerciseBelongType");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(50);

            entity.HasOne(d => d.Exercise).WithMany(p => p.ExerciseBelongTypes)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_ExerciseBelongType_Exercise");

            entity.HasOne(d => d.ExerciseType).WithMany(p => p.ExerciseBelongTypes)
                .HasForeignKey(d => d.ExerciseTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ExerciseBelongType_ExerciseType");
        });

        modelBuilder.Entity<ExerciseType>(entity =>
        {
            entity.ToTable("ExerciseType");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.TypeCode)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TypeName).HasMaxLength(100);
        });

        modelBuilder.Entity<Marking>(entity =>
        {
            entity.ToTable("Marking");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.InputFile).HasMaxLength(50);
            entity.Property(e => e.KindMarking)
                .HasMaxLength(20)
                .HasDefaultValue("io");
            entity.Property(e => e.MarkingDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.OutputFile).HasMaxLength(50);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.TimeLimit).HasDefaultValue(1.0);
            entity.Property(e => e.TypeMarking)
                .HasMaxLength(20)
                .HasDefaultValue("Tương đối");

            entity.HasOne(d => d.Exercise).WithMany(p => p.Markings)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_Marking_Exercise");

            entity.HasOne(d => d.ProgramLanguage).WithMany(p => p.Markings)
                .HasForeignKey(d => d.ProgramLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Marking_ProgramLanguage");

            entity.HasOne(d => d.User).WithMany(p => p.Markings)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_Marking_User");
        });

        modelBuilder.Entity<MarkingDetail>(entity =>
        {
            entity.ToTable("MarkingDetail");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.Marking).WithMany(p => p.MarkingDetails)
                .HasForeignKey(d => d.MarkingId)
                .HasConstraintName("FK_MarkingDetail_Marking");
        });

        modelBuilder.Entity<ProgramLanguage>(entity =>
        {
            entity.ToTable("ProgramLanguage");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ProgramLanguageCode).HasMaxLength(50);
            entity.Property(e => e.ProgramLanguageName).HasMaxLength(50);
        });

        modelBuilder.Entity<Rank>(entity =>
        {
            entity.ToTable("Rank");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Prerequisites).HasMaxLength(50);
            entity.Property(e => e.RankCode).HasMaxLength(20);
            entity.Property(e => e.RankName).HasMaxLength(50);
        });

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.ToTable("TestCase");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.Exercise).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_TestCase_Exercise");
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.ToTable("Theme");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ThemeCode).HasMaxLength(50);
            entity.Property(e => e.ThemeStyle).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("User");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.Fullname).HasMaxLength(256);
            entity.Property(e => e.Otp)
                .HasMaxLength(6)
                .IsUnicode(false)
                .IsFixedLength()
                .HasColumnName("OTP");
            entity.Property(e => e.OtplatestSend)
                .HasColumnType("datetime")
                .HasColumnName("OTPLatestSend");
            entity.Property(e => e.Password)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(12)
                .IsUnicode(false);
            entity.Property(e => e.RegisterTime).HasColumnType("datetime");
            entity.Property(e => e.RetypePassword)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Auth).WithMany(p => p.Users)
                .HasForeignKey(d => d.AuthId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Authority");

            entity.HasOne(d => d.ProgramLanguage).WithMany(p => p.Users)
                .HasForeignKey(d => d.ProgramLanguageId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_ProgramLanguage");

            entity.HasOne(d => d.Rank).WithMany(p => p.Users)
                .HasForeignKey(d => d.RankId)
                .HasConstraintName("FK_User_Rank");

            entity.HasOne(d => d.ThemeCode).WithMany(p => p.Users)
                .HasForeignKey(d => d.ThemeCodeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_User_Theme");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
