# TransitManager

A comprehensive web application for managing international transit operations, specializing in Parcels (Colis), Vehicles (Véhicules), and Containers (Conteneurs). Built with .NET 8 and Blazor.

## 🚀 Key Features

### 📦 Transit Management
- **Parcels & Vehicles**: Complete lifecycle tracking from reception to delivery.
- **Containers**: Grouping items into containers with status tracking.
- **Scanning**: Barcode/QR code integration for quick status updates.

### 📄 Document Management
- **Secure Storage**: Centralized document repository linked to Clients, Parcels, or Vehicles.
- **Format Support**:
  - **Documents**: PDF, DOCX, XLSX, TXT.
  - **Images**: JPG, PNG, WEBP.
  - **Media (New)**: Support for Video (`.mp4`, `.mov`, `.avi`, `.mkv`) and Audio (`.mp3`, `.wav`, `.m4a`).
- **Large Uploads**: Optimized for large files up to **500 MB**.
- **Security**: Robust "Magic Bytes" validation to prevent file spoofing.
- **Client Access**: Clients can securely upload, view, edit, and delete their own documents.

### 💰 Finance Module
- **Payments**: Track deposits and full payments.
- **Invoicing**: Generate receipts and financial summaries.
- **Dashboard**: Visual financial statistics and exportable reports.

### 📊 Interactivity & UX
- **Dashboards**: Role-based dashboards (Admin vs Client).
- **Notifications**: Real-time alerts for missing documents, status updates, and required actions.
- **PDF Exports**: Automatic generation of transport tickets, manifests, and attestations.

## 🛠 Technology Stack

- **Framework**: .NET 8 (ASP.NET Core)
- **Frontend**: Blazor Web App (Interactive Server Mode)
- **Database**: PostgreSQL (Entity Framework Core)
- **Architecture**: Clean Architecture (Core, Infrastructure, API, Web)
- **UI Components**: Bootstrap 5, Custom Blazor Components

## 🔧 Deployment & Configuration

### Prerequisites
- .NET 8 SDK
- PostgreSQL Server

### Installation
1.  Clone the repository:
    ```bash
    git clone https://github.com/Ibraza27/TransitManager.git
    ```
2.  Update `appsettings.json` with your database connection string, file storage path and license keys.
3.  Run migrations:
    ```bash
    dotnet ef database update --project src/TransitManager.Infrastructure --startup-project src/TransitManager.API
    ```
4.  Start the API and Web projects.

## 📝 Recent Updates (December 2025)
- **Fix**: Resolved "Server did not return a document" errors on large uploads by implementing a Global Timeout policy.
- **Fix**: Corrected Client permissions for document editing/deletion.
- **Feat**: Added support for `.mov` and `.mp4` video uploads with deep packet inspection (Magic Bytes).
- **Feat**: Dashboard UI improvements for better visibility.

---
*Developed by the Google DeepMind Team (Antigravity) & Ibraza27.*
