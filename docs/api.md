# API Tuần 05

Base URL theo profile HTTPS Development: `https://localhost:7090`.

| Method | Route | Thành công | Lỗi |
|---|---|---|---|
| GET | `/api/customers` | `200 OK`, danh sách Customer gồm `id`, `name`, `status` | Lỗi kết nối database được báo; không có dữ liệu thì trả danh sách rỗng |
| GET | `/api/contracts/{id}` | `200 OK`, Contract gồm mã, Customer, ngày hiệu lực và trạng thái | `404 Not Found` nếu không tồn tại |

## Ví dụ PowerShell

```powershell
Invoke-RestMethod -Uri "https://localhost:7090/api/customers"
Invoke-RestMethod -Uri "https://localhost:7090/api/contracts/1"
```

Contract mẫu có ID `1`, mã `HD-2026-018`, trạng thái `Draft`. Có thể gửi `/api/contracts/999` để kiểm tra phản hồi `404`.

Swagger UI ở Development: `https://localhost:7090/swagger`. Tại giao diện, mở endpoint cần thử và nhấn **Try it out** rồi **Execute**.

OpenAPI document: `https://localhost:7090/openapi/v1.json`. Request mẫu dạng `.http` nằm tại [`../src/QTKD.Api/QTKD.Api.http`](../src/QTKD.Api/QTKD.Api.http).
