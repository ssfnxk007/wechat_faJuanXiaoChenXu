# FaJuan MySQL BaoTa Deployment

## Goal

Deploy FaJuan API on BaoTa Ubuntu with local MySQL `fajuan_db`, without touching the existing BaoTa project/database.

## Files

- Schema script: `database/mysql/001-fajuan-schema.mysql.sql`
- Foundation-data exporter: `scripts/export-mssql-foundation-data.ps1`
- Generated foundation-data output: `release/private/fajuan-foundation-data.mysql.sql`
- API single-file publish output: `release/fajuan-api-mysql-linux-x64/FaJuan.Api`

`release/` is ignored by git and may contain payment secrets after data export.

## MySQL Target

Use the dedicated database/user only:

```text
Database: fajuan_db
User: fajuan_user
Host: 127.0.0.1
Port: 3306
Charset: utf8mb4
```

Do not import these scripts into `wenyihai_cn` or any other existing BaoTa database.

## Import Order

Run only after confirming `fajuan_db` is empty or after taking a database backup.

```bash
mysql -u fajuan_user -p fajuan_db < database/mysql/001-fajuan-schema.mysql.sql
mysql -u fajuan_user -p fajuan_db < release/private/fajuan-foundation-data.mysql.sql
```

The schema script intentionally uses normal `CREATE TABLE` statements so it fails if tables already exist. That is deliberate: do not silently merge into an unknown database.

## Production Config

Configure production with a real connection string and MySQL version:

```json
{
  "ConnectionStrings": {
    "Default": "Server=127.0.0.1;Port=3306;Database=fajuan_db;User=fajuan_user;Password=YOUR_PASSWORD;CharSet=utf8mb4;"
  },
  "Database": {
    "AutoMigrate": false,
    "MySqlServerVersion": "5.7.43"
  }
}
```

Keep payment keys and API secrets out of git. Put them in the deployed `appsettings.json`, BaoTa environment variables, or the generated private SQL data file.

## Deploy

1. Backup current FaJuan API directory:

```bash
cp -a /www/wwwroot/fajuan-api /www/wwwroot/fajuan-api.backup.$(date +%Y%m%d%H%M%S)
```

2. Upload the published contents from `release/fajuan-api-mysql-linux-x64/` to the FaJuan API directory.
3. Make the executable runnable:

```bash
chmod +x /www/wwwroot/fajuan-api/FaJuan.Api
```

4. Restart only the FaJuan API process/site in BaoTa.

## Verification

Run these checks after restart:

```bash
curl -s http://127.0.0.1:5265/api/health
curl -s https://fajuan-api.wenyihai.cn/api/health
```

Then verify in the UI/API:

- Admin login succeeds.
- Admin menus and permissions are visible.
- Theme/payment/store/product/coupon-template/coupon-pack data is present.
- Mini-program settings/home/product/coupon-pack APIs return 200.
- Order creation works.
- Payment initiation works.
- Successful payment processing grants coupons.
- Granted coupons appear in the user card/coupon area.
- Coupon write-off works.

## Rollback

If a serious issue appears:

1. Stop the FaJuan API process/site.
2. Move the broken deployment aside.
3. Restore the latest `/www/wwwroot/fajuan-api.backup.*` directory.
4. Restore the previous production config.
5. Restart the FaJuan API process/site.

Do not drop or overwrite `fajuan_db` until the rollback decision is clear.
