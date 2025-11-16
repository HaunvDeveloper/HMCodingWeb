# HMCodingWeb

## 🎯 Giới thiệu

**HMCodingWeb** là một ứng dụng web nền tảng .NET 8, được xây dựng để phục vụ như một nền tảng lập trình trực tuyến (Online Coding Platform). Dự án sử dụng Entity Framework Core để tương tác với cơ sở dữ liệu SQL Server.

Dự án bao gồm các tính năng chính như quản lý người dùng, quản lý bài tập, chấm bài tự động, một không gian lập trình (codepad), và các tính năng tương tác thời gian thực như chat và thông báo.

## 🛠️ Công nghệ sử dụng

* **Framework:** .NET 8.0
* **Database:** SQL Server
* **ORM:** Entity Framework Core 8.0.8
* **Real-time:** ASP.NET Core SignalR (với các Hub: `MarkingHub`, `OnlineUsersHub`, `ChatHub`)
* **Frontend:** ASP.NET Core Razor Pages (với tính năng Runtime Compilation)
* **Thư viện chính:**
    * `DinkToPdf`: Để tạo file PDF.
    * `EPPlus`: Để xử lý file Excel.
    * `HtmlAgilityPack`: Để phân tích cú pháp HTML.
    * `System.Linq.Dynamic.Core`: Để truy vấn LINQ động.

## 🏗️ Kiến trúc & Dịch vụ

Ứng dụng được xây dựng theo kiến trúc services, đăng ký các dịch vụ cốt lõi trong `Program.cs`:

* `OnlineCodingWebContext`: DbContext chính của Entity Framework.
* `RunProcessService`: Dịch vụ để chạy các tiến trình (ví dụ: biên dịch và thực thi mã).
* `EmailSendService`: Dịch vụ gửi email.
* `MarkingService`: Dịch vụ xử lý logic chấm bài.
* `UserPointService`: Dịch vụ quản lý điểm số của người dùng.
* `UserListService`: Dịch vụ quản lý danh sách người dùng.
* `RankingService`: Dịch vụ xử lý logic xếp hạng.
* `GenerateSampleOutputService`: Dịch vụ tạo output mẫu.
* `OnlineUsersService`: Dịch vụ theo dõi người dùng trực tuyến.
* `UserCleanupService`: Dịch vụ chạy nền để dọn dẹp dữ liệu người dùng.

## 🗃️ Mô hình Cơ sở dữ liệu (Models)

Cơ sở dữ liệu `OnlineCodingWebContext` quản lý các thực thể chính sau:

* `User`: Quản lý thông tin người dùng.
* `Exercise`: Quản lý thông tin bài tập.
* `Chapter`: Phân loại bài tập theo chương.
* `DifficultyLevel`: Quản lý các mức độ khó của bài tập.
* `TestCase`: Quản lý các ca kiểm thử (test case) cho bài tập.
* `Marking`: Lưu trữ lịch sử các lần chấm bài.
* `MarkingDetail`: Lưu trữ chi tiết kết quả của từng test case trong một lần chấm.
* `Codepad`: Quản lý các file code trong không gian lập trình.
* `ProgramLanguage`: Quản lý các ngôn ngữ lập trình được hỗ trợ.
* `Rank`: Quản lý bậc xếp hạng của người dùng.
* `BoxChat`, `BoxChatMember`, `Message`: Quản lý tính năng chat.
* `Notification`, `NotificationSeenStatus`: Quản lý hệ thống thông báo.
* `CommentToExercise`: Quản lý bình luận trên các bài tập.
* `AccessRole`, `Authority`: Quản lý quyền truy cập và phân quyền.

## 🚀 Cài đặt và Khởi chạy

1.  **Clone repository:**
    ```bash
    git clone <your-repository-url>
    cd HMCodingWeb
    ```

2.  **Cấu hình Connection String:**
    Mở file `appsettings.json` (hoặc `appsettings.Development.json`) và cập nhật chuỗi kết nối `OnlineCoding` trỏ đến cơ sở dữ liệu SQL Server của bạn.

3.  **Tạo cơ sở dữ liệu (Database First):**
    Dự án này sử dụng phương pháp Database First. Bạn có thể sử dụng các lệnh sau để scaffold lại models nếu có thay đổi từ CSDL:

    * **Trên Visual Studio 2022 (Package Manager Console):**
        ```powershell
        Scaffold-DbContext "Data Source=<IP>;Initial Catalog=OnlineCodingWeb;Persist Security Info=True;User ID=<user>;Password=<pass>;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -OutputDir Models -Force
        ```

    * **Trên VS Code (Terminal):**
        ```bash
        dotnet ef dbcontext scaffold "Data Source=<IP>;Initial Catalog=OnlineCodingWeb;Persist Security Info=True;User ID=<user>;Password=<pass>;Trust Server Certificate=True" Microsoft.EntityFrameworkCore.SqlServer -o Models --force
        ```

4.  **Chạy ứng dụng:**
    ```bash
    dotnet run
    ```
