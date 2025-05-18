using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Spool_Meter_Server.Models;

public partial class SpoolMetersManagementSystemContext : DbContext
{
    public SpoolMetersManagementSystemContext()
    {
    }

    public SpoolMetersManagementSystemContext(DbContextOptions<SpoolMetersManagementSystemContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Account> Accounts { get; set; }

    public virtual DbSet<AccountToFcmtoken> AccountToFcmtokens { get; set; }

    public virtual DbSet<MaterialType> MaterialTypes { get; set; }

    public virtual DbSet<NotificationSetting> NotificationSettings { get; set; }

    public virtual DbSet<SpoolMeter> SpoolMeters { get; set; }

    public virtual DbSet<Token> Tokens { get; set; }

    public virtual DbSet<Usage> Usages { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlServer(Environment.GetEnvironmentVariable("SpoolMeterManagemenSystem_ConnectionString"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Account__3214EC27254568AE");

            entity.ToTable("Account");

            entity.Property(e => e.Id)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.HasMany(d => d.SpoolMeters).WithMany(p => p.Accounts)
                .UsingEntity<Dictionary<string, object>>(
                    "AccountToSpoolMeter",
                    r => r.HasOne<SpoolMeter>().WithMany()
                        .HasForeignKey("SpoolMeterId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ATS_SPOOLMETER_ID"),
                    l => l.HasOne<Account>().WithMany()
                        .HasForeignKey("AccountId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_ATS_ACCOUNT_ID"),
                    j =>
                    {
                        j.HasKey("AccountId", "SpoolMeterId");
                        j.ToTable("AccountToSpoolMeter");
                        j.HasIndex(new[] { "AccountId" }, "IDX_ATS_ACCOUNT_ID");
                        j.HasIndex(new[] { "SpoolMeterId" }, "IDX_ATS_SPOOLMETER_ID");
                        j.IndexerProperty<string>("AccountId")
                            .HasMaxLength(5)
                            .IsUnicode(false)
                            .HasColumnName("AccountID");
                        j.IndexerProperty<string>("SpoolMeterId")
                            .HasMaxLength(50)
                            .IsUnicode(false)
                            .HasColumnName("SpoolMeterID");
                    });
        });

        modelBuilder.Entity<AccountToFcmtoken>(entity =>
        {
            entity.HasKey(e => e.Fcmtoken).HasName("PK__AccountT__BBACE64F0F85ACA2");

            entity.ToTable("AccountToFCMToken");

            entity.Property(e => e.Fcmtoken)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("FCMToken");
            entity.Property(e => e.AccountId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("AccountID");

            entity.HasOne(d => d.Account).WithMany(p => p.AccountToFcmtokens)
                .HasForeignKey(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_ATF_ACCOUNT_ID");
        });

        modelBuilder.Entity<MaterialType>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Material__3214EC271FBFA0C1");

            entity.ToTable("MaterialType");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false);
        });

        modelBuilder.Entity<NotificationSetting>(entity =>
        {
            entity.HasKey(e => e.AccountId).HasName("PK__Notifica__349DA586B64E6CA0");

            entity.Property(e => e.AccountId)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("AccountID");

            entity.HasOne(d => d.Account).WithOne(p => p.NotificationSetting)
                .HasForeignKey<NotificationSetting>(d => d.AccountId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_NS_ACCOUNT_ID");
        });

        modelBuilder.Entity<SpoolMeter>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__SpoolMet__3214EC27DAD55A54");

            entity.ToTable("SpoolMeter");

            entity.Property(e => e.Id)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.Color)
                .HasMaxLength(7)
                .IsUnicode(false);
            entity.Property(e => e.MaterialTypeId).HasColumnName("MaterialTypeID");
            entity.Property(e => e.Name)
                .HasMaxLength(64)
                .IsUnicode(false);
            entity.Property(e => e.Password)
                .HasMaxLength(256)
                .IsUnicode(false);

            entity.HasOne(d => d.MaterialType).WithMany(p => p.SpoolMeters)
                .HasForeignKey(d => d.MaterialTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SM_MaterialTypeID");
        });

        modelBuilder.Entity<Token>(entity =>
        {
            entity.HasKey(e => e.TokenValue).HasName("PK__Token__FE1B80ED40AE07D5");

            entity.ToTable("Token");

            entity.Property(e => e.TokenValue)
                .HasMaxLength(128)
                .IsUnicode(false);
            entity.Property(e => e.Id)
                .HasMaxLength(5)
                .IsUnicode(false)
                .HasColumnName("ID");
            entity.Property(e => e.TokenType)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Usage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Usage__3214EC27CB585945");

            entity.ToTable("Usage");

            entity.Property(e => e.Id).HasColumnName("ID");
            entity.Property(e => e.SpoolMeterId)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("SpoolMeterID");
            entity.Property(e => e.Time).HasColumnType("datetime");

            entity.HasOne(d => d.SpoolMeter).WithMany(p => p.Usages)
                .HasForeignKey(d => d.SpoolMeterId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_U_SPOOLMETER_ID");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
