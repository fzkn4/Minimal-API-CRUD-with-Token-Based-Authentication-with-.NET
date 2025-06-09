# Minimal API CRUD with Token-Based Authentication (.NET 9)

This project is a simple yet functional **CRUD API built with .NET 9**, demonstrating:

- Minimal API structure  
- Basic token-based authentication  
- In-memory user and token store  
- Endpoint filtering using a custom `DummyAuthFilter`  
- Admin-only protected operations


## 🚀 Features

- **Login system**: Issues a dummy token (GUID) on successful login.
- **Token-based access control**: All `/users` endpoints require a valid token.
- **Admin authorization**: Only admin users can create or delete users.
- **Logout endpoint**: Invalidates token from the in-memory store.
- **In-memory data**: No database required — simple and fast for prototyping or teaching.


## 🔧 Technologies Used

- [.NET 9 Minimal API](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
- `EndpointFilter` for middleware-style auth logic
- SHA256 hashing for password checking


## 📦 Setup Instructions

> Make sure you have [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed.

1. **Clone the repository**:
   ```bash
   git clone https://github.com/fzkn4/Minimal-API-CRUD-with-Token-Based-Authentication-with-.NET.git
   cd Minimal-API-CRUD-with-Token-Based-Authentication-with-.NET
   ```
2. **Run the app:**
  ```bash
  dotnet run
  ```
3. **The API will be available at:**
  ```bash
  # replace the port with the actual provided port on your terminal
  http://localhost:5024
  ```


## 📬 API Endpoints
> All `/users` endpoints require a valid `Authorization: Bearer <token>` header.

**🔐 Authentication**
  
  Login:
  ```http
  POST /login
  Content-Type: application/json
  
  {
    "username": "admin",
    "password": "adminpass"
  }
  ```
  Returns a token if credentials are valid.

  Logout:
  ```http
  POST /logout
  Authorization: Bearer <your_token>
  ```

---

  **👥 Users CRUD (Requires Token)**

  Get all users:
  ```http
  ### Preferred request (directly access protected endpoint)
  GET /users
  Authorization: Bearer <your_token>
  
  ### Note:
  # You can try GET /, but it redirects to /users (HTTP 301),
  # and not all clients resend the Authorization header after a redirect.
  GET /
  Authorization: Bearer <your_token>
  ```

  Get a spcific user: 
  ```http
  GET /users/{id}
  Authorization: Bearer <your_token>
  ```
  Create User (Admins only): 
  ```http
  POST /users
  Authorization: Bearer <admin_token>
  Content-Type: application/json
  
  {
    "Id": 5,
    "Username": "newuser",
    "Password": "password123",
    "Fullname": "New User",
    "Email": "user@example.com",
    "Address": "City",
    "AddedBy": "admin",
    "IsAdmin": false
  }
  ```

  Delete User (Admins only):
  ```http
  DELETE /users/{id}
  Authorization: Bearer <admin_token>
  ```



## 🧪 Testing with .http File
You can test the endpoints using a .http file in VS Code (with the [REST Client extension](https://marketplace.visualstudio.com/items?itemName=humao.rest-client)). Example:
```http
### Login
POST http://localhost:5024/login
Content-Type: application/json

{
  "username": "admin",
  "password": "adminpass"
}

### Use token returned above in the next requests:
GET http://localhost:5024/users
Authorization: Bearer <paste_token_here>
```
Or you can just use my `request.http` for testing.


## 📁 Project Structure Highlights
- `Program.cs`: All logic is implemented inline using minimal API style.
- `DummyAuthFilter`: Custom `IEndpointFilter` that validates bearer tokens.
- `User` / `LoginRequest`: Simple C# `record` types representing request and model data.



  


