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

    public virtual DbSet<Announcement> Announcements { get; set; }

    public virtual DbSet<Authority> Authorities { get; set; }

    public virtual DbSet<Chapter> Chapters { get; set; }

    public virtual DbSet<ChatMeeting> ChatMeetings { get; set; }

    public virtual DbSet<Codepad> Codepads { get; set; }

    public virtual DbSet<CommentToExercise> CommentToExercises { get; set; }

    public virtual DbSet<CopyPasteHistory> CopyPasteHistories { get; set; }

    public virtual DbSet<DifficultyLevel> DifficultyLevels { get; set; }

    public virtual DbSet<Exercise> Exercises { get; set; }

    public virtual DbSet<ExerciseBelongType> ExerciseBelongTypes { get; set; }

    public virtual DbSet<ExerciseType> ExerciseTypes { get; set; }

    public virtual DbSet<Marking> Markings { get; set; }

    public virtual DbSet<MarkingDetail> MarkingDetails { get; set; }

    public virtual DbSet<Meeting> Meetings { get; set; }

    public virtual DbSet<MeetingParticipant> MeetingParticipants { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NotificationSeenStatus> NotificationSeenStatuses { get; set; }

    public virtual DbSet<Prerequisite> Prerequisites { get; set; }

    public virtual DbSet<ProgramLanguage> ProgramLanguages { get; set; }

    public virtual DbSet<Rank> Ranks { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserContactToCommentEx> UserContactToCommentExes { get; set; }

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

        modelBuilder.Entity<Announcement>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Announce__3213E83F996492C0");

            entity.ToTable("Announcement");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsVisible).HasDefaultValue(true);
            entity.Property(e => e.Title).HasMaxLength(255);

            entity.HasOne(d => d.CreatedByUser).WithMany(p => p.Announcements)
                .HasForeignKey(d => d.CreatedByUserId)
                .HasConstraintName("FK_Announcement_User");
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

        modelBuilder.Entity<ChatMeeting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ChatMeet__3214EC07F2BEA8C5");

            entity.ToTable("ChatMeeting");

            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.HasOne(d => d.Meeting).WithMany(p => p.ChatMeetings)
                .HasForeignKey(d => d.MeetingId)
                .HasConstraintName("FK__ChatMeeti__Meeti__0D44F85C");

            entity.HasOne(d => d.SenderUser).WithMany(p => p.ChatMeetings)
                .HasForeignKey(d => d.SenderUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ChatMeeti__Sende__0E391C95");
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

        modelBuilder.Entity<CommentToExercise>(entity =>
        {
            entity.ToTable("CommentToExercise");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsApproved).HasDefaultValue(true);

            entity.HasOne(d => d.AnswerToCmt).WithMany(p => p.InverseAnswerToCmt)
                .HasForeignKey(d => d.AnswerToCmtId)
                .HasConstraintName("FK_CommentToExercise_CommentToExercise");

            entity.HasOne(d => d.Exercise).WithMany(p => p.CommentToExercises)
                .HasForeignKey(d => d.ExerciseId)
                .HasConstraintName("FK_CommentToExercise_Exercise");

            entity.HasOne(d => d.User).WithMany(p => p.CommentToExercises)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_CommentToExercise_User");
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
                .IsUnicode(false);
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

        modelBuilder.Entity<Meeting>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Meeting__3214EC07FEA28227");

            entity.ToTable("Meeting");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.CreatedAt).HasColumnType("datetime");
            entity.Property(e => e.EndTime).HasColumnType("datetime");
            entity.Property(e => e.StartTime).HasColumnType("datetime");
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.HostUser).WithMany(p => p.Meetings)
                .HasForeignKey(d => d.HostUserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Meeting__HostUse__0697FACD");
        });

        modelBuilder.Entity<MeetingParticipant>(entity =>
        {
            entity.HasKey(e => new { e.MeetingId, e.UserId }).HasName("PK__MeetingP__38816588E471C013");

            entity.ToTable("MeetingParticipant");

            entity.Property(e => e.JoinedAt).HasColumnType("datetime");
            entity.Property(e => e.Role).HasMaxLength(50);

            entity.HasOne(d => d.Meeting).WithMany(p => p.MeetingParticipants)
                .HasForeignKey(d => d.MeetingId)
                .HasConstraintName("FK__MeetingPa__Meeti__09746778");

            entity.HasOne(d => d.User).WithMany(p => p.MeetingParticipants)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__MeetingPa__UserI__0A688BB1");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.ToTable("Notification");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.CreatedByUsername).HasMaxLength(100);
            entity.Property(e => e.Title).HasMaxLength(255);
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .HasDefaultValue("system");
        });

        modelBuilder.Entity<NotificationSeenStatus>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.NotificationId });

            entity.ToTable("NotificationSeenStatus");

            entity.Property(e => e.SeenAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");

            entity.HasOne(d => d.Notification).WithMany(p => p.NotificationSeenStatuses)
                .HasForeignKey(d => d.NotificationId)
                .HasConstraintName("FK_NotificationSeenStatus_Notification");

            entity.HasOne(d => d.User).WithMany(p => p.NotificationSeenStatuses)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK_NotificationSeenStatus_User");
        });

        modelBuilder.Entity<Prerequisite>(entity =>
        {
            entity.HasKey(e => new { e.RankId, e.DifficultyId });

            entity.HasOne(d => d.Difficulty).WithMany(p => p.Prerequisites)
                .HasForeignKey(d => d.DifficultyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prerequisites_DifficultyLevel");

            entity.HasOne(d => d.Rank).WithMany(p => p.PrerequisitesNavigation)
                .HasForeignKey(d => d.RankId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Prerequisites_Rank");
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
                .UseCollation("Latin1_General_CS_AS");
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

        modelBuilder.Entity<UserContactToCommentEx>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.CommentExId });

            entity.ToTable("UserContactToCommentEx");

            entity.Property(e => e.ContactDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.IsLike).HasDefaultValue(true);

            entity.HasOne(d => d.CommentEx).WithMany(p => p.UserContactToCommentExes)
                .HasForeignKey(d => d.CommentExId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_UserContactToCommentEx_CommentToExercise");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
