# API Tuần 06

Base URL theo profile HTTPS Development: `https://localhost:7090`.

## Customer

| Method | Route | Thành công | Lỗi |
|---|---|---|---|
| GET | `/api/customers` | `200 OK`, danh sách tóm tắt Customer | — |
| GET | `/api/customers/{id}` | `200 OK`, thông tin Customer | `404` nếu không tồn tại |
| POST | `/api/customers` | `201 Created`, có `Location` | `400` dữ liệu không hợp lệ; `409` trùng mã hoặc mã số thuế |
| PUT | `/api/customers/{id}` | `200 OK`, Customer đã cập nhật | `404` không tồn tại; `409` trùng mã hoặc mã số thuế |

Customer có mã, tên, mã số thuế, địa chỉ, người đại diện, thông tin liên hệ và trạng thái `Active` hoặc `Suspended`. Contract chỉ nhận Customer `Active`.

Request tạo mẫu:

```json
{
  "code": "CUS-DEMO-001",
  "name": "Logistics Demo",
  "taxCode": "0319999001",
  "address": "Hai Phong",
  "representativeName": "Le Hai",
  "contactInfo": "demo@example.test",
  "status": "Active"
}
```

Response `201 Created` (cũng là shape response của GET chi tiết/PUT):

```json
{
  "id": 3,
  "code": "CUS-DEMO-001",
  "name": "Logistics Demo",
  "taxCode": "0319999001",
  "address": "Hai Phong",
  "representativeName": "Le Hai",
  "contactInfo": "demo@example.test",
  "status": "Active"
}
```

GET danh sách trả `200 OK`, ví dụ:

```json
[
  { "id": 3, "name": "Logistics Demo", "status": "Active" }
]
```

## Contract

| Method | Route | Thành công | Lỗi |
|---|---|---|---|
| GET | `/api/contracts/{id}` | `200 OK`, Contract và Customer | `404` nếu không tồn tại |
| POST | `/api/contracts` | `201 Created`, Contract mới ở `Draft` | `400` Customer không hợp lệ; `409` trùng số hợp đồng |
| PUT | `/api/contracts/{id}` | `200 OK`, Contract đã cập nhật | `400` Customer không hợp lệ; `404` không tồn tại; `409` trạng thái không cho sửa |
| POST | `/api/contracts/{id}/submit` | `200 OK`, `PendingApproval` và `approvalId` mô phỏng | `400` không đạt CTR-02; `404` không tồn tại; `409` sai trạng thái/xung đột |

Request tạo Contract (trạng thái được service khởi tạo là `Draft`):

```json
{
  "contractNo": "HD-DEMO-001",
  "customerId": 3,
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "contractValue": 10000000,
  "paymentTerms": "Monthly",
  "serviceTerms": "Port services"
}
```

Response `201 Created` (cùng shape với GET chi tiết/PUT):

```json
{
  "id": 2,
  "contractNo": "HD-DEMO-001",
  "customerId": 3,
  "customerName": "Logistics Demo",
  "startDate": "2026-11-01",
  "endDate": "2027-10-31",
  "contractValue": 10000000,
  "paymentTerms": "Monthly",
  "serviceTerms": "Port services",
  "status": "Draft"
}
```

### Submit US03

Request:

```http
POST /api/contracts/1/submit
Idempotency-Key: demo-contract-1-submit
```

Khi CTR-02 chưa đạt:

```json
{
  "code": "ContractDoesNotMeetCtr02",
  "message": "Contract does not meet the submission requirements.",
  "failedConditions": [
    "At least one required content item must be provided (demo assumption: ServiceTerms)."
  ]
}
```

Khi hợp lệ, response `200 OK`:

```json
{
  "contractId": 2,
  "status": "PendingApproval",
  "approvalId": "c0b9de1b-6e59-4c7f-a1ec-a74ea55d4914",
  "submittedAt": "2026-10-31T16:50:00+00:00",
  "isReplay": false
}
```

Gửi lại cùng key và cùng Contract trả cùng `approvalId` với `"isReplay": true`; key đã dùng cho Contract khác trả `409 Conflict`.

Quy tắc CTR-02 được cụ thể hóa cho demo như sau:

- Customer tồn tại và `Active`.
- `StartDate` không sau `EndDate`; hợp đồng chưa hết hạn tại thời điểm submit.
- `ServiceTerms` không rỗng, được dùng làm nội dung bắt buộc đại diện cho hồ sơ.

Phase 1 và đồ án chưa chỉ rõ danh sách trường/tệp bắt buộc theo từng loại hợp đồng. Vì vậy `ServiceTerms` là **giả định demo**, không phải quy tắc nghiệp vụ đã được xác nhận. Upload/lưu tệp thực tế chưa được triển khai.

Khi submit thành công, API chỉ đổi trạng thái sang `PendingApproval`, đồng thời tạo một bản ghi submit mô phỏng. Không có các bước phê duyệt, phân công người duyệt, ký điện tử hay chuyển sang `Approved`. `Idempotency-Key` được lưu để request lặp cùng key nhận lại kết quả; unique constraint theo Contract bảo đảm không tạo submit thứ hai cho cùng Contract trong phiên bản này. Request cạnh tranh bị từ chối nếu một request khác đã đổi trạng thái.

### Phạm vi quyền

Ứng dụng hiện chưa có authentication/authorization hoặc danh tính người dùng. Do đó AC01 (403 cho người không có quyền) **chưa được triển khai/chưa đạt**; không dùng header giả làm cơ chế phân quyền. Cần xác định nhà cung cấp identity và chính sách quyền trước khi bổ sung.

## Pricing

| Method | Route | Thành công | Lỗi |
|---|---|---|---|
| GET | `/api/pricelists` | `200 OK`, danh sách PriceList | — |
| GET | `/api/pricelists/{id}` | `200 OK`, PriceList | `404` nếu không tồn tại |
| POST | `/api/pricelists` | `201 Created` | `400` Contract không tồn tại hoặc kỳ hiệu lực không hợp lệ |
| GET | `/api/pricelists/{id}/items` | `200 OK`, PriceItem list | `404` nếu PriceList không tồn tại |
| GET | `/api/pricelists/{id}/items/{serviceCode}` | `200 OK`, PriceItem | `404` nếu PriceItem không tồn tại |
| POST | `/api/pricelists/{id}/items` | `201 Created` | `400` đơn giá không dương; `404` PriceList không tồn tại; `409` trùng ServiceCode trong bảng giá |

PriceList gắn với một Contract và có ngày hiệu lực bắt đầu/kết thúc. Đơn giá phải lớn hơn 0. Kiểm soát chồng lấn kỳ hiệu lực và versioning (PRC-03/04) chưa thuộc phần đã triển khai.

Request tạo PriceList:

```json
{
  "name": "Bảng giá demo 2026",
  "contractId": 2,
  "effectiveDate": "2026-11-01",
  "endDate": "2027-10-31"
}
```

Response `201 Created` (cũng là shape của GET):

```json
{
  "id": 2,
  "name": "Bảng giá demo 2026",
  "contractId": 2,
  "effectiveDate": "2026-11-01",
  "endDate": "2027-10-31"
}
```

Request tạo PriceItem `POST /api/pricelists/2/items`:

```json
{ "serviceCode": "PORT-DEMO", "unitPrice": 1500000 }
```

Response `201 Created` (GET chi tiết trả cùng shape; GET danh sách trả mảng):

```json
{ "priceListId": 2, "serviceCode": "PORT-DEMO", "unitPrice": 1500000 }
```

## Kiểm tra API

Ví dụ PowerShell:

```powershell
Invoke-RestMethod -Uri "https://localhost:7090/api/customers"
Invoke-RestMethod -Uri "https://localhost:7090/api/customers/1"
Invoke-RestMethod -Uri "https://localhost:7090/api/contracts/1"
```

Swagger UI ở Development: `https://localhost:7090/swagger`; OpenAPI document: `https://localhost:7090/openapi/v1.json`. Request mẫu và demo theo thứ tự nằm tại [`../src/QTKD.Api/QTKD.Api.http`](../src/QTKD.Api/QTKD.Api.http).
