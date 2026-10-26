using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

// Задание 1
public class Author
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ICollection<Book> Books { get; set; } = new List<Book>();
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int AuthorId { get; set; }
    public Author? Author { get; set; }
}

//Задание 2
public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<IEnumerable<T>> GetAllAsync();
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(int id);
}

public interface IBookRepository : IRepository<Book>
{
    Task<IEnumerable<Book>> GetByAuthorNameAsync(string authorName);
    Task<IEnumerable<Book>> GetByPriceRangeAsync(decimal min, decimal max);
}

public interface IUnitOfWork : IDisposable
{
    IBookRepository Books { get; }
    IRepository<Author> Authors { get; }
    Task<int> SaveChangesAsync();
}

public class LibraryDbContext : DbContext
{
    public DbSet<Book> Books { get; set; } = null!;
    public DbSet<Author> Authors { get; set; } = null!;

    public LibraryDbContext(DbContextOptions<LibraryDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasOne(b => b.Author)
            .WithMany(a => a.Books)
            .HasForeignKey(b => b.AuthorId);
    }
}

//Задание 3 и 4
public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly DbContext _context;
    protected readonly DbSet<T> _dbSet;

    public GenericRepository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id) => await _dbSet.FindAsync(id);
    public virtual async Task<IEnumerable<T>> GetAllAsync() => await _dbSet.ToListAsync();
    public virtual async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);
    
    public virtual Task UpdateAsync(T entity) 
    { 
        _dbSet.Update(entity); 
        return Task.CompletedTask; 
    }
    
    public virtual async Task DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity != null) _dbSet.Remove(entity);
    }
}

public class BookRepository : GenericRepository<Book>, IBookRepository
{
    public BookRepository(DbContext context) : base(context) { }

    public async Task<IEnumerable<Book>> GetByAuthorNameAsync(string authorName)
    {
        return await _dbSet.Include(b => b.Author)
                            .Where(b => b.Author != null && b.Author.Name.Contains(authorName))
                            .ToListAsync();
    }

    public async Task<IEnumerable<Book>> GetByPriceRangeAsync(decimal min, decimal max)
    {
        return await _dbSet.Where(b => b.Price >= min && b.Price <= max).ToListAsync();
    }
}

public class UnitOfWork : IUnitOfWork
{
    private readonly LibraryDbContext _context;
    private IBookRepository? _books;
    private IRepository<Author>? _authors;

    public UnitOfWork(LibraryDbContext context)
    {
        _context = context;
    }

    public IBookRepository Books => _books ??= new BookRepository(_context);
    public IRepository<Author> Authors => _authors ??= new GenericRepository<Author>(_context);

    public async Task<int> SaveChangesAsync() => await _context.SaveChangesAsync();
    public void Dispose() => _context.Dispose();
}

class Program
{
    static async Task Main(string[] args)
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseSqlite("Data Source=library_demo.db")
            .Options;

        using (var context = new LibraryDbContext(options))
        {
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();
            Console.WriteLine("База данных создана.\n");
        }

        using var uow = new UnitOfWork(new LibraryDbContext(options));

        Console.WriteLine("Добавление данных");
        var author1 = new Author { Name = "Лев Толстой" };
        var author2 = new Author { Name = "Фёдор Достоевский" };

        await uow.Authors.AddAsync(author1);
        await uow.Authors.AddAsync(author2);

        await uow.Books.AddAsync(new Book { Title = "Война и мир", Price = 800m, Author = author1 });
        await uow.Books.AddAsync(new Book { Title = "Анна Каренина", Price = 500m, Author = author1 });
        await uow.Books.AddAsync(new Book { Title = "Преступление и наказание", Price = 450m, Author = author2 });
        await uow.Books.AddAsync(new Book { Title = "Идиот", Price = 300m, Author = author2 });

        await uow.SaveChangesAsync();
        Console.WriteLine("Добавлены авторы и(или) книги\n");

        Console.WriteLine("Получение всех книг");
        var allBooks = await uow.Books.GetAllAsync();
        foreach (var book in allBooks)
        {
            Console.WriteLine($"- {book.Title} ({book.Author?.Name}) | Цена: {book.Price} руб.");
        }
        Console.WriteLine();

        Console.WriteLine("Поиск по автору(Толстой)");
        var tolstoyBooks = await uow.Books.GetByAuthorNameAsync("Толстой");
        foreach (var book in tolstoyBooks)
        {
            Console.WriteLine($"- {book.Title} / Цена: {book.Price} руб.");
        }
        Console.WriteLine();

        Console.WriteLine("Поиск по цене(400-600)");
        var midPriceBooks = await uow.Books.GetByPriceRangeAsync(400m, 600m);
        foreach (var book in midPriceBooks)
        {
            Console.WriteLine($"- {book.Title} / Цена: {book.Price} руб.");
        }
        Console.WriteLine();

        Console.WriteLine("Обновление данных");
        var bookToUpdate = allBooks.First(b => b.Title == "Идиот");
        bookToUpdate.Price = 350m;
        await uow.Books.UpdateAsync(bookToUpdate);
        await uow.SaveChangesAsync();
        Console.WriteLine($"Цена книги 'Идиот' обновлена на {bookToUpdate.Price} руб.\n");

        Console.WriteLine("Удаление данных(Название книги)");
        var bookToDelete = allBooks.First(b => b.Title == "Анна Каренина");
        await uow.Books.DeleteAsync(bookToDelete.Id);
        await uow.SaveChangesAsync();
        Console.WriteLine("Книга 'Анна Каренина' удалена\n");

        Console.WriteLine("Cостояния БД");
        var finalBooks = await uow.Books.GetAllAsync();
        foreach (var book in finalBooks)
        {
            Console.WriteLine($"- {book.Title} ({book.Author?.Name}) / Цена: {book.Price} руб.");
        }

    }
}