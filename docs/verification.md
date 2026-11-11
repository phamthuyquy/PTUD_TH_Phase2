# Minh chứng Phase 2 — Tuần 05, 06 và 07

Tài liệu này đối chiếu yêu cầu trong `TH_Phase2_QTKD.pdf` (mục 3–5) với mã nguồn, migration, API và test hiện có. Tuần 06/07 tiếp tục trên solution của Tuần 05. Kết quả test tự động không thay thế minh chứng demo trực tiếp với PostgreSQL.

## Kết quả kiểm thử tự động gần nhất

Chạy tại root repository:

```powershell
dotnet test .\QTKD.slnx --logger "trx;LogFileName=week07-tests.trx" --results-directory .\tests\QTKD.Api.Tests\TestResults
```

Kết quả chạy ngày 2026-11-11: **22 passed, 0 failed, 0 skipped** (10 unit nghiệp vụ, 8 service/EF Core, 4 API integration). Báo cáo TRX: [`week07-tests.trx`](../tests/QTKD.Api.Tests/TestResults/week07-tests.trx). Bảng chi tiết trạng thái tự động/thủ công và Pass/Fail: [`Bang-TC1-TC8.md`](./Bang-TC1-TC8.md).

Test phân nhóm bằng trait xUnit:

```powershell
dotnet test .\tests\QTKD.Api.Tests\QTKD.Api.Tests.csproj --filter "Category=Unit"
dotnet test .\tests\QTKD.Api.Tests\QTKD.Api.Tests.csproj --filter "Category=Service"
dotnet test .\tests\QTKD.Api.Tests\QTKD.Api.Tests.csproj --filter "Category=Integration"
```

- `Category=Unit`: 10 unit test thuần cho quy tắc CTR-01/02/03, không truy cập database.
- `Category=Service`: 8 test service kiểm tra xử lý EF Core trên SQLite in-memory.
- `Category=Integration`: gọi HTTP qua `WebApplicationFactory`; mỗi test dùng SQLite in-memory riêng, có schema và seed. Không dùng PostgreSQL cá nhân hay dữ liệu quan trọng.
- Cả ba nhóm chạy trong cùng project xUnit `tests/QTKD.Api.Tests`, tham chiếu trực tiếp API.

Khi NuGet không truy cập được, restore có thể cảnh báo `NU1900` vì không lấy được dữ liệu kiểm tra lỗ hổng package. Cảnh báo này không làm test thất bại trong lần chạy nêu trên.

## Tuần 05 — Nền tảng, dữ liệu và API đọc

| Yêu cầu PDF | Minh chứng trong repository | Trạng thái |
|---|---|---|
| Một solution/API có phân lớp | [`QTKD.slnx`](../QTKD.slnx), [`src/QTKD.Api/`](../src/QTKD.Api/), [`docs/architecture.md`](./architecture.md) | Có |
| Entity Customer, Contract, PriceList, PriceItem; DbContext và quan hệ | Domain entities và [`QtkdDbContext`](../src/QTKD.Api/Infrastructure/QtkdDbContext.cs) | Có; có thêm ContractSubmission cho luồng tuần 06 |
| PostgreSQL, migration và dữ liệu mẫu | [`Program.cs`](../src/QTKD.Api/Program.cs), [`src/QTKD.Api/Migrations/`](../src/QTKD.Api/Migrations/) | Có cấu hình PostgreSQL, hai migration và seed trong model/migration |
| GET Customer và Contract đọc từ DB | [`CustomersController`](../src/QTKD.Api/Controllers/CustomersController.cs), [`ContractsController`](../src/QTKD.Api/Controllers/ContractsController.cs), [`ReadServices.cs`](../src/QTKD.Api/Application/ReadServices.cs) | Có; API dùng EF Core, không hard-code response |
| Cấu hình/migration/chạy API có thể lặp lại | [`README.md`](../README.md) | Có hướng dẫn Docker, User Secrets, migration và launch profile |
| Đối chiếu API với PostgreSQL chạy thật | Video demo PostgreSQL đã ghi lại | Đã có minh chứng demo; bộ test tự động dùng SQLite |
| Sơ đồ và ảnh/video demo | [`docs/architecture.md`](./architecture.md); file `.http`; video demo lưu riêng | Có sơ đồ, request mẫu và video demo |

Seed hiện có hai Customer, một Contract `Draft`, một PriceList và hai PriceItem. Migration `InitialCreate` tạo schema nền; `Week06BusinessFunctions` bổ sung trường và bảng cho phần nghiệp vụ tiếp theo.

## Tuần 06 — API nghiệp vụ và US03

| Chức năng/quy tắc | Đoạn xử lý | Kiểm chứng |
|---|---|---|
| Tạo/xem/cập nhật Customer | [`CustomersController`](../src/QTKD.Api/Controllers/CustomersController.cs), [`CustomerService`](../src/QTKD.Api/Application/CustomerService.cs) | Integration TC01–TC02; service tests |
| Tạo/xem/cập nhật Contract | [`ContractsController`](../src/QTKD.Api/Controllers/ContractsController.cs), [`ContractCommandService`](../src/QTKD.Api/Application/ContractCommandService.cs) | Integration workflow TC03–TC07; service tests |
| CTR-01: chỉ sửa Draft/Revision Requested | [`ContractBusinessRules.CanEdit`](../src/QTKD.Api/Application/ContractBusinessRules.cs) và `UpdateContractAsync` | Unit test ca cho phép Draft/Revision Requested và từ chối PendingApproval; TC03/TC04 kiểm tra persistence/API |
| CTR-02: Customer Active, thời hạn và nội dung hợp lệ | [`ContractBusinessRules.GetFailedSubmissionConditions`](../src/QTKD.Api/Application/ContractBusinessRules.cs) | Unit test dữ liệu hợp lệ và từng điều kiện lỗi; TC05/TC06 kiểm tra service/API |
| CTR-03: không chuyển thẳng Draft → Approved | [`ContractBusinessRules`](../src/QTKD.Api/Application/ContractBusinessRules.cs) và `UpdateContractAsync` | Unit test submit chỉ nhận Draft, submit chuyển sang PendingApproval, direct status update bị từ chối; TC07 kiểm tra API |
| Submit lặp và key idempotency | `SubmitContractAsync`, unique constraint trong [`QtkdDbContext`](../src/QTKD.Api/Infrastructure/QtkdDbContext.cs) | TC08: cùng key replay cùng `ApprovalId`; submit mới sau đó trả 409; chỉ một submission |
| Pricing cơ bản | [`PriceListsController.cs`](../src/QTKD.Api/Controllers/PriceListsController.cs), [`PricingService.cs`](../src/QTKD.Api/Application/PricingService.cs) | Có service test tạo PriceList; API được mô tả trong [`api.md`](./api.md) |
| Mẫu request/response và kịch bản demo | [`docs/api.md`](./api.md), [`QTKD.Api.http`](../src/QTKD.Api/QTKD.Api.http) | Có; HTTP sample đã dùng cùng ContractNo khi cập nhật để tránh trùng số với dữ liệu seed |

Giả định nghiệp vụ: `ServiceTerms` đại diện cho nội dung bắt buộc CTR-02 vì đặc tả Phase 1 chưa cung cấp danh sách file/nội dung cụ thể. Không xem giả định này là yêu cầu nghiệp vụ đã được xác nhận. Upload tệp và quy trình Approval đầy đủ chưa triển khai. AC01 (authorization) cũng chưa đạt vì ứng dụng chưa có authentication/authorization.

## Tuần 07 — kiểm thử TC01–TC08

Bảng Pass/Fail, tên test tự động, trạng thái chạy thủ công và lý do được tách tại [`docs/Bang-TC1-TC8.md`](./Bang-TC1-TC8.md). `ContractsControllerTests` gọi luồng tạo Customer → tạo Contract → submit thiếu điều kiện → đọc lại `Draft` → thử đổi thẳng `Approved` → bổ sung nội dung → submit thành công → đọc lại `PendingApproval` → replay và kiểm tra số bản ghi. Test xác nhận HTTP status, response body và dữ liệu sau thao tác.

## Lỗi tìm thấy và sửa

- **Cách tái hiện:** gọi `CustomerService.CreateCustomerAsync` trực tiếp với `Name = ""`. Trước sửa, service dựa vào `[Required]` của API model validation; khi được gọi bên ngoài pipeline controller, request vẫn được lưu.
- **Nguyên nhân:** `[ApiController]` kiểm tra validation cho HTTP request, nhưng service không tự đảm bảo trường tên bắt buộc.
- **Sửa:** thêm `string.IsNullOrWhiteSpace(request.Name)` guard tại `CustomerService`, trả lỗi 400 `InvalidCustomer`; giữ test service TC02 và integration TC02 để kiểm tra hai đường gọi.
- **Commit sửa lỗi:** `367fecc` — `test: complete phase 2 verification`.

Các lỗi fixture cũng được xử lý trong lúc hoàn thiện test: tạo PriceList khi chưa có Contract; ID/số hợp đồng test trùng seed; test controller thay provider nhưng vẫn giữ cấu hình PostgreSQL; và dữ liệu theo dõi `ExecuteUpdate` không được reload trước khi assert.

## Minh chứng demo và cách nộp

| Hoạt động | Trạng thái hiện tại | Minh chứng cần lưu |
|---|---|---|
| Clone, cấu hình `.env`/User Secrets, migration, chạy API | Hướng dẫn có trong README; đã ghi hình demo | Đính kèm video/ảnh khi nộp; không đưa secrets vào ảnh hoặc repository |
| Tuần 05: gọi GET Customer/Contract và đối chiếu PostgreSQL | Video demo có đối chiếu PostgreSQL | Đính kèm video hoặc ảnh response và bản ghi tương ứng |
| Tuần 06: demo Customer → Contract Draft → submit fail → bổ sung → submit success | Video demo đã ghi lại luồng | Đính kèm video/ảnh khi nộp; test tự động xác minh luồng tương ứng trên SQLite |
| Tuần 07: chạy `dotnet test` và lưu báo cáo | Tự động; TRX tại [`tests/QTKD.Api.Tests/TestResults/week07-tests.trx`](../tests/QTKD.Api.Tests/TestResults/week07-tests.trx) | Đính kèm TRX hoặc ảnh console khi nộp |

## Lệnh kiểm chứng PostgreSQL và demo

Sau khi cấu hình theo README:

```powershell
docker compose ps
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project .\src\QTKD.Api --startup-project .\src\QTKD.Api
dotnet run --project .\src\QTKD.Api --launch-profile https
```

Mở một terminal khác để gọi API:

```powershell
Invoke-RestMethod -Uri "https://localhost:7090/api/customers"
Invoke-RestMethod -Uri "https://localhost:7090/api/contracts/1"
```

Thực hiện tiếp các request Customer/Contract/submit trong [`QTKD.Api.http`](../src/QTKD.Api/QTKD.Api.http), rồi kiểm tra trạng thái và submission trong PostgreSQL. Đổi các mã định danh duy nhất nếu đã chạy demo trước đó.
