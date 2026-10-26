using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

// Задание 1.
public class User
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Login { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(128)]
    public string PassHash { get; set; } = string.Empty;
}

public class LibraryDbContext : DbContext
{
    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
}

public class CreateUserDto
{
    [Required(ErrorMessage = "Логин обязателен")]
    public string Login { get; set; } = string.Empty;

    [Required(ErrorMessage = "Хеш пароля обязателен")]
    public string PassHash { get; set; } = string.Empty;
}

//Задание 2
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly LibraryDbContext _context;

    public UserController(LibraryDbContext context)
    {
        _context = context;
    }

    // POST /user
    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        if (await _context.Users.AnyAsync(u => u.Login == dto.Login))
        {
            return Conflict(new { message = "Пользователь с таким логином уже существует" });
        }

        if (dto.PassHash.Length < 32)
        {
            return BadRequest(new { message = "Пароль должен быть передан в виде хешированной строки" });
        }

        var user = new User { Login = dto.Login, PassHash = dto.PassHash };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUserById), new { id = user.Id }, new { user.Id, user.Login });
    }

    // GET /user/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Пользователь не найден" });

        return Ok(new { user.Id, user.Login });
    }

    // PUT /user/{id}
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] CreateUserDto dto)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Пользователь не найден" });

        if (user.Login != dto.Login && await _context.Users.AnyAsync(u => u.Login == dto.Login))
        {
            return Conflict(new { message = "Новый логин уже занят" });
        }

        user.Login = dto.Login;
        user.PassHash = dto.PassHash;
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "Данные пользователя успешно обновлены" });
    }

    // DELETE /user/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null) return NotFound(new { message = "Пользователь не найден" });

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Пользователь успешно удален" });
    }
}
