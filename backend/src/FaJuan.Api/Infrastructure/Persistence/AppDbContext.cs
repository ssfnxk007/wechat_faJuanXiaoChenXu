using FaJuan.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace FaJuan.Api.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> AppUsers => Set<AppUser>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CouponTemplate> CouponTemplates => Set<CouponTemplate>();
    public DbSet<CouponTemplateProductScope> CouponTemplateProductScopes => Set<CouponTemplateProductScope>();
    public DbSet<CouponTemplateStoreScope> CouponTemplateStoreScopes => Set<CouponTemplateStoreScope>();
    public DbSet<CouponPack> CouponPacks => Set<CouponPack>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<CouponPackItem> CouponPackItems => Set<CouponPackItem>();
    public DbSet<CouponOrder> CouponOrders => Set<CouponOrder>();
    public DbSet<UserCoupon> UserCoupons => Set<UserCoupon>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<MiniAppShareEvent> MiniAppShareEvents => Set<MiniAppShareEvent>();
    public DbSet<CouponWriteOffRecord> CouponWriteOffRecords => Set<CouponWriteOffRecord>();
    public DbSet<CouponIssueImportBatch> CouponIssueImportBatches => Set<CouponIssueImportBatch>();
    public DbSet<CouponIssueImportDetail> CouponIssueImportDetails => Set<CouponIssueImportDetail>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<AdminRole> AdminRoles => Set<AdminRole>();
    public DbSet<AdminMenu> AdminMenus => Set<AdminMenu>();
    public DbSet<AdminUserRole> AdminUserRoles => Set<AdminUserRole>();
    public DbSet<AdminRoleMenu> AdminRoleMenus => Set<AdminRoleMenu>();
    public DbSet<AdminPermission> AdminPermissions => Set<AdminPermission>();
    public DbSet<AdminRolePermission> AdminRolePermissions => Set<AdminRolePermission>();
    public DbSet<WeChatPaySetting> WeChatPaySettings => Set<WeChatPaySetting>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUser");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.MiniOpenId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Mobile).HasMaxLength(20);
            entity.Property(x => x.Nickname).HasMaxLength(100);
            entity.Property(x => x.UnionId).HasMaxLength(64);
            entity.Property(x => x.OfficialOpenId).HasMaxLength(64);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.MiniOpenId).IsUnique();
            entity.HasIndex(x => x.Mobile);
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("Store");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ContactName).HasMaxLength(50);
            entity.Property(x => x.ContactPhone).HasMaxLength(20);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Product");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.ErpProductCode).HasMaxLength(64).IsRequired();
            entity.Property(x => x.DetailImageAssetIds).HasColumnType("nvarchar(max)");
            entity.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.ErpProductCode).IsUnique();
        });

        modelBuilder.Entity<CouponTemplate>(entity =>
        {
            entity.ToTable("CouponTemplate");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DiscountAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ThresholdAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ValidFrom).HasColumnType("datetime");
            entity.Property(x => x.ValidTo).HasColumnType("datetime");
            entity.Property(x => x.Remark).HasMaxLength(500);
            entity.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<MediaAsset>(entity =>
        {
            entity.ToTable("MediaAsset");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.FileUrl).HasMaxLength(500).IsRequired();
            entity.Property(x => x.MediaType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.BucketType).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Tags).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.BucketType, x.IsEnabled, x.Sort, x.Id });
        });

        modelBuilder.Entity<Banner>(entity =>
        {
            entity.ToTable("Banner");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LinkUrl).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.IsEnabled, x.Sort, x.Id });
        });

        modelBuilder.Entity<CouponTemplateProductScope>(entity =>
        {
            entity.ToTable("CouponTemplateProductScope");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.CouponTemplateId, x.ProductId }).IsUnique();
        });

        modelBuilder.Entity<CouponTemplateStoreScope>(entity =>
        {
            entity.ToTable("CouponTemplateStoreScope");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.CouponTemplateId, x.StoreId }).IsUnique();
        });

        modelBuilder.Entity<CouponPack>(entity =>
        {
            entity.ToTable("CouponPack");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
            entity.Property(x => x.SaleStartTime).HasColumnType("datetime");
            entity.Property(x => x.SaleEndTime).HasColumnType("datetime");
            entity.Property(x => x.Remark).HasMaxLength(500);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CouponPackItem>(entity =>
        {
            entity.ToTable("CouponPackItem");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.CouponPackId, x.CouponTemplateId });
        });

        modelBuilder.Entity<CouponOrder>(entity =>
        {
            entity.ToTable("CouponOrder");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.OrderNo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OrderAmount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.PaymentNo).HasMaxLength(64);
            entity.Property(x => x.PaidAt).HasColumnType("datetime");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.OrderNo).IsUnique();
        });

        modelBuilder.Entity<UserCoupon>(entity =>
        {
            entity.ToTable("UserCoupon");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CouponCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.ReceivedAt).HasColumnType("datetime");
            entity.Property(x => x.EffectiveAt).HasColumnType("datetime");
            entity.Property(x => x.ExpireAt).HasColumnType("datetime");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.CouponCode).IsUnique();
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.ToTable("PaymentTransaction");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.PaymentNo).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Amount).HasColumnType("decimal(18,2)");
            entity.Property(x => x.ChannelTradeNo).HasMaxLength(64);
            entity.Property(x => x.PaidAt).HasColumnType("datetime");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.PaymentNo).IsUnique();
        });

        modelBuilder.Entity<MiniAppShareEvent>(entity =>
        {
            entity.ToTable("MiniAppShareEvent");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventKey).HasMaxLength(100).IsRequired();
            entity.Property(x => x.EventType).HasMaxLength(16).IsRequired();
            entity.Property(x => x.ShareId).HasMaxLength(40).IsRequired();
            entity.Property(x => x.VisitorKey).HasMaxLength(64);
            entity.Property(x => x.TargetType).HasMaxLength(16).IsRequired();
            entity.Property(x => x.TargetKey).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PagePath).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Scene).HasMaxLength(32);
            entity.Property(x => x.QueryJson).HasMaxLength(1000);
            entity.Property(x => x.ClientTime).HasColumnType("datetime");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.Property(x => x.Ip).HasMaxLength(64);
            entity.Property(x => x.UserAgent).HasMaxLength(256);

            entity.HasIndex(x => x.EventKey).IsUnique();
            entity.HasIndex(x => x.ShareId);
            entity.HasIndex(x => new { x.EventType, x.CreatedAt });
            entity.HasIndex(x => new { x.TargetType, x.TargetKey, x.CreatedAt });
            entity.HasIndex(x => new { x.FromUserId, x.CreatedAt });
            entity.HasIndex(x => new { x.OpenUserId, x.CreatedAt });
        });

        modelBuilder.Entity<CouponWriteOffRecord>(entity =>
        {
            entity.ToTable("CouponWriteOffRecord");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CouponCode).HasMaxLength(50).IsRequired();
            entity.Property(x => x.OperatorName).HasMaxLength(50);
            entity.Property(x => x.DeviceCode).HasMaxLength(50);
            entity.Property(x => x.WriteOffAt).HasColumnType("datetime");
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CouponIssueImportBatch>(entity =>
        {
            entity.ToTable("CouponIssueImportBatch");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100);
            entity.Property(x => x.FileName).HasMaxLength(200);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<CouponIssueImportDetail>(entity =>
        {
            entity.ToTable("CouponIssueImportDetail");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Mobile).HasMaxLength(20);
            entity.Property(x => x.MiniOpenId).HasMaxLength(64);
            entity.Property(x => x.OfficialOpenId).HasMaxLength(64);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.Mobile, x.Status });
            entity.HasIndex(x => x.BatchId);
        });

        modelBuilder.Entity<AdminUser>(entity =>
        {
            entity.ToTable("AdminUser");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DisplayName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.Username).IsUnique();
        });

        modelBuilder.Entity<AdminRole>(entity =>
        {
            entity.ToTable("AdminRole");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<AdminMenu>(entity =>
        {
            entity.ToTable("AdminMenu");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Path).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Component).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
        });

        modelBuilder.Entity<AdminUserRole>(entity =>
        {
            entity.ToTable("AdminUserRole");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.AdminUserId, x.AdminRoleId }).IsUnique();
        });

        modelBuilder.Entity<AdminRoleMenu>(entity =>
        {
            entity.ToTable("AdminRoleMenu");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.AdminRoleId, x.AdminMenuId }).IsUnique();
        });

        modelBuilder.Entity<AdminPermission>(entity =>
        {
            entity.ToTable("AdminPermission");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MenuPath).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => x.Code).IsUnique();
        });

        modelBuilder.Entity<AdminRolePermission>(entity =>
        {
            entity.ToTable("AdminRolePermission");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.CreatedAt).HasColumnType("datetime");
            entity.HasIndex(x => new { x.AdminRoleId, x.AdminPermissionId }).IsUnique();
        });

        modelBuilder.Entity<WeChatPaySetting>(entity =>
        {
            entity.ToTable("WeChatPaySetting");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedNever();
            entity.Property(x => x.AppId).HasMaxLength(64).IsRequired();
            entity.Property(x => x.MerchantId).HasMaxLength(32).IsRequired();
            entity.Property(x => x.MerchantSerialNo).HasMaxLength(128).IsRequired();
            entity.Property(x => x.PrivateKeyPem).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(x => x.ApiV3Key).HasMaxLength(128).IsRequired();
            entity.Property(x => x.NotifyUrl).HasMaxLength(512).IsRequired();
            entity.Property(x => x.EnableMockFallback).IsRequired();
            entity.Property(x => x.UpdatedAt).HasColumnType("datetime");

            entity.HasData(new WeChatPaySetting
            {
                Id = 1,
                AppId = string.Empty,
                MerchantId = string.Empty,
                MerchantSerialNo = string.Empty,
                PrivateKeyPem = string.Empty,
                ApiV3Key = string.Empty,
                NotifyUrl = string.Empty,
                EnableMockFallback = true,
                UpdatedAt = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            });
        });
    }
}
