# Kiểm chứng và minh chứng Tuần 06

## Kiểm tra tự động

Chạy tại root repository:

```powershell
dotnet build .\QTKD.slnx
dotnet test .\QTKD.slnx
```

Test service dùng SQLite tạm thời và kiểm tra:

- GET Customer/Contract tuần 05 không hồi quy.
- CTR-02: Customer không hoạt động, ngày hết hạn và thiếu `ServiceTerms` bị từ chối.
- CTR-02 thất bại không đổi trạng thái Contract và không tạo submission.
- Submit hợp lệ chỉ chuyển sang `PendingApproval`, tạo submission và phát lại kết quả với cùng `Idempotency-Key`.
- Sai trạng thái submit và cập nhật Contract bị từ chối.
- Pricing: kỳ hiệu lực sai và PriceList không tồn tại bị từ chối; đơn giá hợp lệ gắn đúng PriceList.

Test SQLite xác nhận luồng service và ràng buộc EF, nhưng không thay thế kiểm thử cạnh tranh trên PostgreSQL thật.

## Đối chiếu quy tắc

| Yêu cầu | Endpoint và đoạn xử lý | Kiểm thử / bằng chứng | Trạng thái |
|---|---|---|---|
| US03 — tạo yêu cầu submit và trả kết quả | `POST /api/contracts/{id}/submit`: [ContractsController.SubmitContract](../src/QTKD.Api/Controllers/ContractsController.cs#L70) gọi [ContractCommandService.SubmitContractAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L161) | [QTKD.Api.http](../src/QTKD.Api/QTKD.Api.http), test submit service và kiểm tra PostgreSQL thật | Đã triển khai ở mức mô phỏng approval |
| AC01 — người không có quyền | Cần authorization trước `SubmitContract`; chưa có authentication/user identity trong ứng dụng | Chưa có test 403 | **Chưa triển khai/chưa đạt** |
| AC02 — Contract sai trạng thái | Kiểm tra trạng thái `Draft` trong [ContractCommandService.SubmitContractAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L161) | [SubmitContract_WhenWrongState_ReturnsConflictWithoutCreatingSubmission](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L89) | Đã triển khai |
| AC03 / CTR-02 — Customer hợp lệ | [ContractCommandService.GetFailedConditionsAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L296) yêu cầu Customer `Active` | [SubmitContract_WhenCustomerIsSuspended_ReturnsCtr02Error](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L34) | Đã triển khai theo giả định `Active` |
| CTR-02 — thời hạn hợp lệ | [ContractCommandService.GetFailedConditionsAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L296) kiểm tra thứ tự ngày và ngày hết hạn | [SubmitContract_WhenEffectivePeriodIsExpired_ReturnsCtr02Error](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L51) | Đã triển khai theo giả định demo |
| AC03 / CTR-02 — nội dung bắt buộc | [ContractCommandService.GetFailedConditionsAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L296) kiểm tra `ServiceTerms`; Controller trả danh sách ở [ContractsController.SubmitContract](../src/QTKD.Api/Controllers/ContractsController.cs#L70) | [SubmitContract_WhenServiceTermsMissing_ReturnsCtr02ErrorsWithoutChangingData](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L13), [SubmitContract_WhenCtr02Fails_ReturnsBadRequestWithFailedConditions](../tests/QTKD.Api.Tests/ContractsControllerTests.cs#L48) | Đã triển khai với giả định demo; chưa có upload tệp |
| AC04 / CTR-03 — không chuyển thẳng Approved | Conditional update sang `PendingApproval` và lưu submission trong [ContractCommandService.SubmitContractAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L161) | [SubmitContract_WhenValid_TransitionsOnceAndReplaysSameSubmission](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L67), kiểm tra PostgreSQL thật | Đã triển khai; không có workflow approval đầy đủ |
| AC05 — request lặp/đồng thời | Idempotency lookup và unique constraints trong [ContractCommandService.SubmitContractAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L161), cấu hình DB ở [QtkdDbContext](../src/QTKD.Api/Infrastructure/QtkdDbContext.cs#L120) | Test replay tuần tự; hai request đồng thời trên PostgreSQL thật trả một `200`, một `409` | Đã kiểm tra |
| CTR-01 — chỉ sửa Draft/Revision Requested | Conditional update trạng thái tại [ContractCommandService.UpdateContractAsync](../src/QTKD.Api/Application/ContractCommandService.cs#L74) | [UpdateContract_WhenNotEditable_ReturnsConflictAndLeavesFieldsUnchanged](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L106), [UpdateContract_WhenRevisionRequested_IsAllowed](../tests/QTKD.Api.Tests/ContractCommandServiceTests.cs#L157) | Đã triển khai |

### Mapping các API tuần 06 vào service

| API | Đoạn xử lý |
|---|---|
| Customer GET/POST/PUT | [CustomersController](../src/QTKD.Api/Controllers/CustomersController.cs); [CustomerService](../src/QTKD.Api/Application/CustomerService.cs) |
| Contract GET/POST/PUT/submit | [ContractsController](../src/QTKD.Api/Controllers/ContractsController.cs); [ContractCommandService](../src/QTKD.Api/Application/ContractCommandService.cs) |
| PriceList và PriceItem GET/POST | [PriceListsController](../src/QTKD.Api/Controllers/PriceListsController.cs); [PricingService](../src/QTKD.Api/Application/PricingService.cs) |

## Kịch bản demo US03

1. Tạo Customer có trạng thái `Active`; lưu lại ID response.
2. Tạo Contract với Customer đó, trạng thái được service khởi tạo là `Draft`; để `serviceTerms` rỗng hoặc bỏ qua.
3. Gửi `POST /api/contracts/{id}/submit` với `Idempotency-Key`. Xác nhận `400`, danh sách `failedConditions`, trạng thái vẫn `Draft` và không có row trong `ContractSubmissions`.
4. Cập nhật Contract khi còn `Draft`, điền `serviceTerms` và dữ liệu ngày hợp lệ.
5. Gửi submit lại. Xác nhận `200`, `PendingApproval` và `approvalId`.
6. Gửi lại cùng key. Xác nhận cùng `approvalId`, `isReplay: true`, chỉ có một row submit.
7. Thử cập nhật Contract sau submit; xác nhận `409`.

`QTKD.Api.http` có mẫu request theo luồng này. ID mới cần thay vào biến `contractId`/`customerId` dựa trên response tạo.

## PostgreSQL thật — đã chạy

Docker PostgreSQL đã ở trạng thái `healthy`; migration `20261031163530_Week06BusinessFunctions` đã áp dụng thành công sau `InitialCreate`. Luồng HTTP thực tế đã được chạy với dữ liệu integration mới:

- Tạo Customer và Contract `Draft` thành công.
- Submit khi thiếu `ServiceTerms`: `400`; trạng thái còn `Draft`.
- Bổ sung nội dung rồi submit: `200`, trạng thái `PendingApproval`.
- Gửi lại cùng idempotency key: cùng `approvalId`, `isReplay: true`.
- Gửi đồng thời hai request với hai key cho cùng Contract: một request `200`, request còn lại `409`; chỉ một submission được tạo.
- Tạo PriceList cho Contract, thêm PriceItem với đơn giá dương và GET lại item: API trả `201`, `201`, rồi `200`.

Các truy vấn dưới đây có thể dùng để xem migration và đối chiếu dữ liệu sau demo:

```sql
SELECT "MigrationId" FROM public."__EFMigrationsHistory" ORDER BY "MigrationId";
SELECT "Id", "Code", "Name", "Status" FROM public."Customers" ORDER BY "Id";
SELECT "Id", "ContractNo", "Status", "ServiceTerms" FROM public."Contracts";
SELECT "Id", "ContractId", "ApprovalId", "IdempotencyKey", "SubmittedAt"
FROM public."ContractSubmissions";
SELECT "Id", "ContractId", "EffectiveDate", "EndDate" FROM public."PriceLists";
SELECT "PriceListId", "ServiceCode", "UnitPrice" FROM public."PriceItems";
```
