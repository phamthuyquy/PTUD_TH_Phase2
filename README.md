# QTKD — Phase 2

ASP.NET Core Web API dùng .NET 10, Entity Framework Core và PostgreSQL. Giữ nguyên repository này xuyên suốt Tuần 05–07.

## Cấu trúc repository

```text
src/QTKD.Api/           Web API, domain, application, EF Core và migrations
tests/QTKD.Api.Tests/   xUnit tests cho các API GET
docs/                   Kiến trúc, API và checklist minh chứng
docker-compose.yml      PostgreSQL cục bộ qua Docker
README.md               Hướng dẫn chạy và kiểm thử
```

## Yêu cầu môi trường

- .NET 10 SDK
- Docker Desktop nếu dùng PostgreSQL bằng Docker
- EF Core CLI (`dotnet-ef`) phiên bản 10

## Chạy PostgreSQL bằng Docker

PostgreSQL trong container dùng cổng `5432`; cổng host Windows mặc định là `5435` và chỉ nhận kết nối từ máy này. Tại thư mục repository, tạo `.env` cục bộ:

```powershell
if (-not (Test-Path .\.env)) { Copy-Item .env.example .env }
notepad .env
docker compose up -d
docker compose ps
```

Đổi `QTKD_POSTGRES_PASSWORD` trong `.env` thành mật khẩu cục bộ trước khi chạy Compose. `.env` đã được Git ignore; không commit mật khẩu. Muốn dùng cổng host khác, chỉnh `QTKD_POSTGRES_PORT` trong `.env`, rồi dùng cùng cổng đó trong connection string.

## Cấu hình kết nối API

Từ PowerShell tại root repository:

```powershell
$dbPassword = Read-Host "Mật khẩu PostgreSQL"
$connectionString = "Host=localhost;Port=5435;Database=qtkd;Username=postgres;Password=$dbPassword"
dotnet user-secrets set "ConnectionStrings:QtkdDatabase" $connectionString --project .\src\QTKD.Api
```

Nhập cùng mật khẩu đang đặt trong `QTKD_POSTGRES_PASSWORD` của file `.env`. Project đã có `UserSecretsId`, nên không cần chạy lại `dotnet user-secrets init`. Nếu đổi `QTKD_POSTGRES_PORT`, thay `5435` bằng cổng host mới. User-secrets giữ connection string cục bộ; không ghi mật khẩu vào `appsettings*.json` hoặc Git.

## Migration và database

Migration `InitialCreate` đã có trong source. Để tạo/cập nhật database mới:

```powershell
dotnet ef database update --project .\src\QTKD.Api --startup-project .\src\QTKD.Api
```

Không chạy lại `migrations add InitialCreate` trên repository đã có migration. Khi mô hình dữ liệu thay đổi ở các tuần sau, tạo migration mới với tên phù hợp rồi cập nhật database:

```powershell
dotnet ef migrations add <TenMigration> --project .\src\QTKD.Api --startup-project .\src\QTKD.Api
dotnet ef database update --project .\src\QTKD.Api --startup-project .\src\QTKD.Api
```

Migration ban đầu tạo `Customers`, `Contracts`, `PriceLists`, `PriceItems` và seed 2 khách hàng, hợp đồng `HD-2026-018` trạng thái `Draft`, một bảng giá có 2 dịch vụ.

## Build, test và chạy API

```powershell
dotnet build .\QTKD.slnx
dotnet test .\QTKD.slnx
dotnet run --project .\src\QTKD.Api --launch-profile https
```

Các test xUnit kiểm tra response của hai controller GET bằng service giả lập, không cần PostgreSQL. Muốn xác nhận luồng database thật, cần khởi động Docker, áp dụng migration, chạy API rồi thử:

```powershell
Invoke-RestMethod -Uri "https://localhost:7090/api/customers"
Invoke-RestMethod -Uri "https://localhost:7090/api/contracts/1"
```

Swagger UI khi chạy Development: `https://localhost:7090/swagger`. Trang này dùng OpenAPI document tại `https://localhost:7090/openapi/v1.json`; có thể mở UI, chọn endpoint và nhấn **Try it out** để gửi request. Contract không tồn tại (ví dụ `/api/contracts/999`) trả `404 Not Found`. Request mẫu có trong [`src/QTKD.Api/QTKD.Api.http`](./src/QTKD.Api/QTKD.Api.http).

## Tài liệu và minh chứng

- [Sơ đồ kiến trúc](./docs/architecture.md)
- [API và cách thử](./docs/api.md)
- [Checklist kiểm chứng/minh chứng](./docs/verification.md)

## Phạm vi

Tuần 05 bao gồm nền tảng dữ liệu và API đọc. Chưa triển khai tạo/cập nhật hợp đồng, gửi trình duyệt US03, phân quyền hoặc quy trình Approval; các nghiệp vụ này thuộc tuần sau. Xem [`docs/`](./docs/) để biết phạm vi test và phần minh chứng cần tự ghi sau khi chạy thực tế.
