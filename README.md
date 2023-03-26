# Oyster

## About the project

This project served as a way of learning various new technologies and tools in practice, most of them from the ground up. At this point, a significant amount of code would end up changed, were it to be developed further and refactored.
Nonetheless, it helped me grasp many concepts related to web applications, their security (e.g. CSRF and antiforgery tokens, preventing mass assignment, HTML and URL encoding), database performance tuning with EF Core (ways of loading related data, tracking entities, split queries, non-clustered indexes, pagination) as well as a few ASP.NET Core concepts and implementations like HTTP request processing, the middleware pipeline, authorization etc.

All in all, the utilized technology stack turned out to be rather comfortable for small to mid-sized projects using Razor Pages + AJAX requests instead of a SPA developed with a front-end framework.

### Built with

- C#
- ASP.NET Core
- Entity Framework Core
- Razor Pages
- ASP.NET Core Identity
- SQL Server (using Docker containers and Azure RHEL VM)
- Tailwind CSS + Flowbite

---

## Run

1. Clone the repo

```bash
git clone https://github.com/anxsha/Oyster.git
```

2. Get a fake SMTP server. I used and recommend [smtp4dev](https://github.com/rnwood/smtp4dev). You can install it as a global tool with .NET SDK

```bash
dotnet tool install -g Rnwood.Smtp4dev
```

  3. Run it. The server's web UI runs by default on port 5000 that is used by Oyster, thus change it to 5002 or higher with `urls` parameter

```bash
smtp4dev —urls "http://0.0.0.0:5002/"
```

4. If having problems with SMTP not starting (port permissions), use the `smtpport` parameter (optional)

```bash
smtp4dev —urls "http://0.0.0.0:5002/" —smtpport=5003
```

5. Then, modify code in `Oyster/EmailService/SmtpEmailSender.cs` to change the port (only if using the `smtpport` parameter)

```csharp
var smtpClient = new SmtpClient {
  Port = 5003, 
…
};
```

6. Get EF Core tools
```bash
dotnet tool install --global dotnet-ef
```

7. If you don't have already, install [Node.js](https://nodejs.org/en/download)

8. Get node packages inside the project directory

```bash
npm install
```
9. Verify the project builds successfully

```
dotnet build
```

10.  Fill the `OysterContextConnection` connection string in `appsettings.json` with database path. For example, using Docker SQL Server Developer Edition image, it looks like below

```json
  "ConnectionStrings": {
    "OysterContextConnection": "Server=localhost,1433\\\\Catalog=OysterDb;Database=OysterDb;User=sa;Password=<YourPassword>;"
  }
```

11. Now, you can run the initial migration and apply it to the database

```bash
dotnet ef migrations add Initial
```

```bash
dotnet ef database update
```

12. Launch the app
```bash
dotnet run
```

---

## Screenshots

<img src="https://user-images.githubusercontent.com/30478993/227748113-a01d77a1-62a8-4bc6-8902-7bd10315c317.png">

<img src="https://user-images.githubusercontent.com/30478993/227748126-98efbb0a-f019-4532-bb47-5f9c83a5a962.png">

<img src="https://user-images.githubusercontent.com/30478993/227748130-bd1b41c7-f0fc-4055-800e-2c57e4009d69.png" width="49.6%"> <img src="https://user-images.githubusercontent.com/30478993/227748144-3ef87d80-26f3-44dc-ab70-9d2527c7c9a3.png" width="49.6%">

---

## License

[MIT](https://choosealicense.com/licenses/mit/)

---
