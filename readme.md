
📚 프로젝트 개요
간단한 프로젝트 소개를 여기에 작성하세요.



## 🗂️ Architecture
```
webserver
 ┣ Controllers
 ┃ ┣ RoomConstroller.cs
 ┃ ┗ UserController.cs
 ┣ Data
 ┃ ┣ ApplicationDbContext.cs
 ┃ ┗ DB_Initializer.cs
 ┣ DTOs
 ┃ ┣ GameDTO.cs
 ┃ ┣ RoomDTO.cs
 ┃ ┗ UserDTO.cs
 ┣ Enums
 ┃ ┗ Enums.cs
 ┣ Extensions
 ┃ ┗ ServiceCollectionExtensions.cs
 ┣ Game
 ┃ ┣ Card.cs
 ┃ ┣ Deck.cs
 ┃ ┣ GamePlayer.cs
 ┃ ┗ GameState.cs
 ┣ Hubs
 ┃ ┗ GameHubs.cs
 ┣ Migrations
 ┃ ┣ 20250419132025_InitialCreate.cs
 ┣ Models
 ┃ ┣ Room.cs
 ┃ ┗ User.cs
 ┣ Repositories
 ┃ ┣ RoomRepository
 ┃ ┃ ┣ IRoomRepository.cs
 ┃ ┃ ┗ RoomRepository.cs
 ┃ ┗ UserRepository
 ┃ ┃ ┣ IUserRepository.cs
 ┃ ┃ ┗ UserRepository.cs
 ┣ Services
 ┃ ┣ GameService
 ┃ ┃ ┗ GameService.cs
 ┃ ┣ JwtService
 ┃ ┃ ┣ IJwtService.cs
 ┃ ┃ ┗ JwtServices.cs
 ┃ ┣ RoomService
 ┃ ┃ ┣ IRoomService.cs
 ┃ ┃ ┗ RoomService.cs
 ┃ ┗ UserService
 ┃ ┃ ┣ IUserService.cs
 ┃ ┃ ┗ UserService.cs
 ┣ Utils
 ┃ ┣ DB_Result.cs
 ┃ ┣ JwtSettings.cs
 ┃ ┣ RedisHelper.cs
 ┃ ┗ ResponseWrapper.cs
 ┣ index.html
 ┣ Program.cs
 ┣ readme.md
```