# QTKD — Phase 2

ASP.NET Core Web API dùng .NET 10, Entity Framework Core và PostgreSQL. Repository này được dùng xuyên suốt Tuần 05–07; Tuần 06 và 07 tiếp tục trên nền Tuần 05, không tạo hệ thống khác.

## Cấu trúc

```text
src/QTKD.Api/           Web API, domain, application, EF Core và migrations
tests/QTKD.Api.Tests/   xUnit service tests và API integration tests
docs/                   Kiến trúc, API và minh chứng Tuần 05–07
docker-compose.yml      PostgreSQL cục bộ qua Docker
QTKD.slnx               Solution
```

- Service/business-rule tests được gắn trait `Category=Unit`.
- Integration tests gọi API qua `WebApplicationFactory`; mỗi test dùng SQLite in-memory riêng, có schema, seed và ràng buộc quan hệ. Không test nào kết nối database PostgreSQL có dữ liệu thật.

## Yêu cầu

- .NET 10 SDK
- Docker Desktop nếu chạy PostgreSQL cục bộ
- EF Core CLI (`dotnet-ef`) phiên bản 10 nếu chạy migration

Kiểm tra .NET/EF CLI:

```powershell
dotnet --version
dotnet ef --version
```

Nếu chưa cài EF CLI:

```powershell
dotnet tool install --global dotnet-ef --version 10.*
```

## Clone và cấu hình PostgreSQL

Tại thư mục repository:

```powershell
if (-not (Test-Path .\.env)) { Copy-Item .env.example .env }
notepad .env
```

Đặt `QTKD_POSTGRES_PASSWORD` thành mật khẩu cục bộ mạnh trong `.env`; không commit file này. Khởi động PostgreSQL:

```powershell
docker compose up -d
docker compose ps
```

Đặt connection string trong .NET User Secrets, không ghi mật khẩu vào `appsettings*.json` hoặc source:

```powershell
$dbPassword = Read-Host "Mật khẩu PostgreSQL"
$connectionString = "Host=localhost;Port=5435;Database=qtkd;Username=postgres;" + "Password=" + $dbPassword
dotnet user-secrets set "ConnectionStrings:QtkdDatabase" $connectionString --project .\src\QTKD.Api
Remove-Variable dbPassword, connectionString
```

Nhập cùng mật khẩu với `QTKD_POSTGRES_PASSWORD`. Nếu đổi `QTKD_POSTGRES_PORT` trong `.env`, dùng cổng host đó thay cho `5435`.

## Migration và chạy API

Repository đã có migration `InitialCreate` và `Week06BusinessFunctions`; không tạo lại `InitialCreate`. Áp dụng migration vào database:

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project .\src\QTKD.Api --startup-project .\src\QTKD.Api
```

`InitialCreate` tạo các bảng nền và dữ liệu mẫu; `Week06BusinessFunctions` mở rộng Customer/Contract/Pricing và thêm `ContractSubmissions`.

Build và chạy API:

```powershell
dotnet build .\QTKD.slnx
dotnet run --project .\src\QTKD.Api --launch-profile https
```

Khi API chạy:

- Swagger UI: `https://localhost:7090/swagger`
- OpenAPI: `https://localhost:7090/openapi/v1.json`
- Request demo tuần 06: [`src/QTKD.Api/QTKD.Api.http`](./src/QTKD.Api/QTKD.Api.http)
- Danh sách route và mẫu request/response: [`docs/api.md`](./docs/api.md)

Các request trong file `.http` dùng mã demo cố định; đổi mã Customer, mã số thuế và số Contract nếu chạy lại trên database đã có dữ liệu.

## Chạy test Tuần 07

Tại root repository:

```powershell
dotnet test .\QTKD.slnx
```

Chạy riêng từng nhóm:

```powershell
dotnet test .\tests\QTKD.Api.Tests\QTKD.Api.Tests.csproj --filter "Category=Unit"
dotnet test .\tests\QTKD.Api.Tests\QTKD.Api.Tests.csproj --filter "Category=Integration"
```

Lưu kết quả TRX để làm minh chứng:

```powershell
dotnet test .\QTKD.slnx --logger "trx;LogFileName=week07-tests.trx" --results-directory .\docs\test-results
```

Integration test chạy bằng SQLite in-memory biệt lập, tạo schema và dữ liệu seed riêng cho từng test; không cần Docker, PostgreSQL hoặc User Secrets. Báo cáo kiểm thử và bảng TC01–TC08 được ghi tại [`docs/verification.md`](./docs/verification.md). File TRX được tạo khi chạy lệnh và nằm dưới `docs/test-results/`.

## Tài liệu và minh chứng

- [Kiến trúc](./docs/architecture.md)
- [API Tuần 06](./docs/api.md)
- [Đối chiếu yêu cầu và minh chứng Tuần 05–07](./docs/verification.md)

Nội dung đối chiếu dựa trên `TH_Phase2_QTKD.pdf`, các migration, source code, test và tài liệu trong repository. Các ảnh/video demo hoặc bằng chứng PostgreSQL thật cần được nhóm tự lưu khi thực hiện demo trên môi trường nộp bài; test SQLite không thay thế bước đó.

## Phạm vi và giới hạn đã biết

- Đã có Customer, Contract, PriceList/PriceItem và luồng submit mô phỏng; submit thành công chỉ chuyển `Draft` sang `PendingApproval`.
- `ServiceTerms` là giả định demo đại diện cho nội dung bắt buộc CTR-02; chưa có upload/lưu tệp.
- Chưa có authentication/authorization (AC01), quy trình approval đầy đủ, audit actor, ký điện tử hoặc xử lý PRC-03/versioning.
- Khởi tạo PostgreSQL, chạy migration, chạy API và demo với database thật cần được xác nhận trên máy nộp bài; bộ integration test tự động sử dụng SQLite.
