# Kiểm chứng và minh chứng Tuần 05

## Kiểm tra tự động

Chạy tại root repository:

```powershell
dotnet build .\QTKD.slnx
dotnet test .\QTKD.slnx
```

Các unit test hiện có kiểm tra:

- `GET /api/customers` trả `200 OK` cùng danh sách khách hàng.
- `GET /api/contracts/{id}` trả `200 OK` khi tìm thấy hợp đồng.
- `GET /api/contracts/{id}` trả `404 Not Found` khi không tìm thấy.

Đây là kiểm tra controller với application service giả; các unit test này không chứng minh kết nối PostgreSQL.

## Kiểm tra PostgreSQL thật

1. Nếu chưa có `.env`, tạo từ `.env.example`, đặt mật khẩu cục bộ riêng; khởi động `docker compose up -d` và xác nhận `docker compose ps` báo healthy.
2. Cấu hình user-secrets theo hướng dẫn ở README. Project đã có `UserSecretsId`; không cần gọi lại `dotnet user-secrets init`.
3. Chạy `dotnet ef database update --project .\src\QTKD.Api --startup-project .\src\QTKD.Api`.
4. Chạy API, gọi hai GET ở [`api.md`](./api.md), rồi kiểm tra dữ liệu và schema:

   ```powershell
   @'
   \set ON_ERROR_STOP on
   \dt
   SELECT "Id", "Name", "Status" FROM public."Customers" ORDER BY "Id";
   SELECT "ContractNo", "Status" FROM public."Contracts";
   SELECT "PriceListId", "ServiceCode", "UnitPrice" FROM public."PriceItems" ORDER BY "ServiceCode";
   SELECT "MigrationId", "ProductVersion" FROM public."__EFMigrationsHistory";
   SELECT tc.table_name, tc.constraint_name, tc.constraint_type, kcu.column_name,
          ccu.table_name AS referenced_table, ccu.column_name AS referenced_column
   FROM information_schema.table_constraints tc
   LEFT JOIN information_schema.key_column_usage kcu
     ON tc.constraint_name = kcu.constraint_name AND tc.table_schema = kcu.table_schema
   LEFT JOIN information_schema.constraint_column_usage ccu
     ON tc.constraint_name = ccu.constraint_name AND tc.table_schema = ccu.table_schema
   WHERE tc.table_schema = 'public'
     AND tc.table_name IN ('Contracts', 'Customers', 'PriceItems', 'PriceLists')
     AND tc.constraint_type IN ('PRIMARY KEY', 'FOREIGN KEY')
   ORDER BY tc.table_name, tc.constraint_type, tc.constraint_name;
   '@ | docker compose exec -T postgres psql -U postgres -d qtkd
   if ($LASTEXITCODE -ne 0) { throw "PostgreSQL verification failed." }
   ```

5. Đối chiếu kết quả SQL với response API; thử `/api/contracts/999` để xác nhận `404`.

Swagger UI chỉ được bật trong Development, tại `https://localhost:7090/swagger`; OpenAPI JSON ở `https://localhost:7090/openapi/v1.json`.

### Kiểm tra Swagger UI

Đã chạy API ở môi trường Development và xác nhận `/swagger` trả HTTP 200 sau chuyển hướng tới `/swagger/index.html`. OpenAPI document trả về cả hai route `/api/customers` và `/api/contracts/{id}`.
