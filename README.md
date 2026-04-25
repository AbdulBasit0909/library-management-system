# Library Management System - A Full-Stack .NET & Blazor Application

![.NET](https://img.shields.io/badge/.NET-8-blueviolet) ![C#](https://img.shields.io/badge/C%23-11-green) ![Blazor](https://img.shields.io/badge/Blazor-WebAssembly-blue) ![Entity Framework](https://img.shields.io/badge/Entity%20Framework-Core-orange)

A modern, feature-rich, and interactive web application designed to manage the core operations of a university library. This project provides distinct, role-based experiences for three user types: **Librarians (Administrators)**, **Teachers**, and **Students**.

The system is built on a robust, industry-standard technology stack, featuring a secure .NET Web API backend and a dynamic, responsive Blazor WebAssembly frontend. It incorporates advanced features such as real-time notifications, a persistent notification center, and an integrated AI assistant to create a truly modern user experience.

---



## ✨ Key Features

### 👤 General & Public Features
- **Secure User Registration:** Open registration system allowing users to sign up as a Student, Teacher, or Librarian.
- **JWT-Based Authentication:** Secure login system using JSON Web Tokens.
- **Forgot Password Flow:** A complete, email-based password reset functionality using SendGrid for email delivery.

### 👥 Authenticated User Features (All Roles)
- **Comprehensive Profile Page:** A personal dashboard to view account details (username, email, role) and a summary of borrowing activity.
- **Change Password:** A secure form for users to update their own passwords.
- **Real-Time Notification Center:**
    - A functional "bell" icon in the main navigation bar with an unread notification count.
    - A dropdown list showing a history of recent, persistent notifications.
    - Real-time toast pop-ups for immediate alerts (e.g., reservation approved).
- **AI Librarian Assistant:**
    - An interactive chatbot widget available on all pages.
    - Users can ask natural language questions (e.g., "Suggest a book about space") and receive intelligent answers.
    - Book titles mentioned by the AI are automatically converted into clickable links that search the library catalog.
- **Personal Reading List (Wishlist):**
    - Ability to add or remove any book from the catalog to a personal "My Reading List" page.
    - The list provides a quick overview of desired books and their current availability.

### 👨‍🏫 Student & Teacher Features
- **Role-Specific Dashboards:** Clean dashboards providing quick access to relevant features.
- **Rich Book Catalog:**
    - A visually appealing, paginated catalog displaying books with their cover images.
    - Advanced searching (by title/author) and filtering (by category).
    - A detailed modal view for each book showing its description, availability, average user rating, and all user reviews.
- **Book Reservation System:** Users can request to borrow a book, which notifies the librarian for approval.
- **"My Books" Page:** A personal dashboard for borrowing activity with two tabs:
    - **Currently Borrowed:** Shows active loans, due dates, and an "Overdue" status.
    - **Loan Renewal:** Allows users to extend the due date of a loan directly from the page.
    - **Borrowing History:** A complete history of all previously returned books.
- **Book Ratings and Reviews:** From the "Borrowing History" tab, users can submit a 1-5 star rating and a text review for books they have read.

### 👩‍🏫 Teacher-Specific Features
- **Exclusive E-Resource Access:** Teachers have privileged access to a curated collection of digital materials (PDFs like academic journals, e-books, etc.) uploaded by the librarian. This includes a dedicated, searchable "Browse E-Resources" page with direct download functionality.

### 📚 Librarian / Administrator Features
The Librarian has full administrative control over the entire system.
- **Comprehensive Dashboard:** An overview of key library statistics (total books, users on loan, etc.).
- **Full User Management:** View all users, edit user roles, and delete accounts.
- **Full Book Catalog Management (CRUD):** Add, edit, and delete books and all their details (including description and cover image URL).
- **Category Management:** A dedicated page to create and delete book categories.
- **Circulation Desk (Issue & Return):** A centralized page to issue new loans and process returns. Overdue fines are automatically calculated and applied upon return.
- **Fine Management:** A view of all outstanding fines, with the ability to mark them as "Paid."
-   **Reservation Management:** A queue of all pending book requests with one-click "Approve" or "Reject" actions.
- **Digital Resource Management:** A dedicated interface to upload new PDF resources and manage the existing collection.
- **Data Visualization:** A reports page with interactive charts (Bar and Pie charts) displaying data for "Top 10 Most Popular Books" and "Books by Category".

---

## 🛠️ Technology Stack

| Category      | Technology / Tool                                         |
|---------------|-----------------------------------------------------------|
| **Backend**   | .NET 8, C#, ASP.NET Core Web API, Entity Framework Core 8 |
| **Frontend**  | Blazor WebAssembly, C#, HTML/CSS, Bootstrap 5             |
| **Database**  | MS SQL Server (Local), MySQL (Production)                 |
| **Security**  | ASP.NET Core Identity, JWT Authentication               |
| **Real-time** | SignalR                                                   |
| **AI**        | OpenRouter API (GPT, Mistral, Llama models)               |
| **Services**  | SendGrid (for email delivery)                             |
| **Deployment**| Docker, Railway (Backend & DB), Netlify (Frontend)        |
| **Dev Tools** | Visual Studio 2022, Git, GitHub                           |

---

## ⚙️ Local Setup and Installation

To run this project on a local development machine:

1.  **Prerequisites:**
    *   [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
    *   Visual Studio 2022 (or a code editor like VS Code)
    *   SQL Server (LocalDB is included with Visual Studio)

2.  **Clone the Repository:**
    ```sh
    git clone https://github.com/AbdulBasit0909/library-management-system.git
    cd library-management-system
    ```

3.  **Configure Secrets:**
    *   In the `LibraryManagement.API` project, open `appsettings.json`.
    *   Set your local database connection string in `ConnectionStrings:DefaultConnection`.
    *   Fill in the placeholder values for `JWT:Secret`, `OpenAI:ApiKey`, and `SendGrid:ApiKey`.

4.  **Run Database Migrations:**
    *   In Visual Studio, open the **Package Manager Console**.
    *   Set the Default Project to `LibraryManagement.API`.
    *   Run the command: `Update-Database`

5.  **Run the Application:**
    *   Right-click the solution in Visual Studio and select "Set Startup Projects...".
    *   Choose "Multiple startup projects" and set the Action for both `LibraryManagement.API` and `LibraryManagement.Web` to "Start".
    *   Press F5 to run both projects. The application will open in your browser.
