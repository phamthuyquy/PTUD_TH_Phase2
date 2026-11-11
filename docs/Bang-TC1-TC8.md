# Bảng kết quả kiểm thử TC01–TC08

## Kết quả lần chạy gần nhất

- Ngày chạy: 2026-11-11
- Tổng: **22 passed, 0 failed, 0 skipped**
- Unit nghiệp vụ: **10 passed**
- Service/EF Core: **8 passed**
- API integration: **4 passed**
- Báo cáo TRX: [`week07-tests.trx`](../tests/QTKD.Api.Tests/TestResults/week07-tests.trx)
- Toàn bộ test tự động; không có TC nào chỉ chạy thủ công hoặc chưa tự động hóa.

## Bảng đối chiếu

| TC | Trường hợp và kết quả mong đợi | Test tự động | Pass/Fail | Thủ công / lý do |
|---|---|---|---|---|
| TC01 | Tạo Customer hợp lệ; nhận `201`, kiểm tra body và GET lại đúng Customer đã lưu | Integration: `CreateCustomer_ValidPayload_CreatesAndCanBeReadBack_TC01` | Pass | Không cần chạy thủ công để xác nhận TC; tích hợp PostgreSQL thật được demo riêng |
| TC02 | Tạo Customer thiếu tên; bị từ chối, nhận `400` và body có validation error cho `Name` | Integration: `CreateCustomer_MissingRequiredName_IsRejectedWithValidationError_TC02`; service: `CreateCustomer_WithoutName_ShouldFail_TC02` | Pass | Không cần; đã tự động kiểm tra status, thông báo lỗi và service |
| TC03 | Sửa Contract ở `Draft` với dữ liệu hợp lệ; thành công, giá trị cập nhật được lưu | Unit rule `CanEdit_ReturnsTrueForEditableStatuses`; service `UpdateContract_WhenStatusIsDraft_ShouldSucceed_TC03`; API workflow | Pass | Không cần; cả quy tắc, lưu dữ liệu và HTTP được kiểm tra tự động |
| TC04 | Sửa Contract ở `PendingApproval`; bị từ chối và dữ liệu giữ nguyên | Unit rule `CanEdit_ReturnsFalseForPendingApproval`; service `UpdateContract_WhenStatusIsPendingApproval_ShouldFail_CTR01_TC04`; API workflow | Pass | Không cần; lỗi và dữ liệu đọc lại được kiểm tra tự động |
| TC05 | Submit thiếu điều kiện CTR-02; nhận `400`, lỗi được trả về, trạng thái vẫn `Draft` và không tạo submission | Unit `GetFailedSubmissionConditions_ReturnsErrorForInvalidRequirements`; API workflow | Pass | Không cần; điều kiện nghiệp vụ, HTTP, body, DB và GET đều tự động |
| TC06 | Submit Contract hợp lệ; nhận `200`, `PendingApproval`, GET lại thấy trạng thái mới | Unit `GetFailedSubmissionConditions_ReturnsNoErrorsForValidContract`, `CanSubmit_AllowsDraftAndRejectsOtherStatuses`; service test; API workflow | Pass | Không cần; response và trạng thái được kiểm tra tự động |
| TC07 | Yêu cầu PUT chuyển trực tiếp `Draft` sang `Approved`; bị từ chối, trạng thái không đổi | Unit `CanSetStatusDirectly_RejectsRequestedStatus`; service test `UpdateContract_WhenStatusIsProvided_ShouldRejectDirectTransition_TC07`; API workflow | Pass | Không cần; kiểm tra `400`, ProblemDetails và Contract vẫn `Draft` |
| TC08 | Submit lặp cùng key không tạo kết quả trùng; trả cùng `ApprovalId`/`isReplay`; lần submit mới bị từ chối | API workflow: replay cùng key, kiểm tra `409` cho submit lặp mới và đúng một row `ContractSubmission` | Pass | Không cần; kiểm tra response và số row trong DB tự động |

Unit tests CTR-01/02/03 gọi `ContractBusinessRules` trực tiếp, không dùng database. Service tests dùng SQLite in-memory; API integration tests gọi `WebApplicationFactory` với SQLite riêng biệt cho từng test. Bộ test không kết nối PostgreSQL hoặc database chứa dữ liệu quan trọng.
