using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

//Задание 1
public interface IMovable
{
    void Move(int x, int y);
}

public class Point : IMovable
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public Point(int x, int y) { X = x; Y = y; }
    public void Move(int x, int y) { X = x; Y = y; }
}

public interface IDrawable
{
    void Draw();
}

public class Circle : IDrawable
{
    public void Draw() => Console.WriteLine("Рисую круг.");
}

public class Rectangle : IDrawable
{
    public void Draw() => Console.WriteLine("Рисую прямоугольник.");
}

public class DrawingService
{
    public void DrawAll(List<IDrawable> shapes)
    {
        foreach (var shape in shapes) shape.Draw();
    }
}

//Задание 2
public interface IShape
{
    double GetArea();
    double GetPerimeter();
}

public class ShapeCircle : IShape
{
    public double Radius { get; }
    public ShapeCircle(double radius) => Radius = radius;
    public double GetArea() => Math.PI * Radius * Radius;
    public double GetPerimeter() => 2 * Math.PI * Radius;
}

public class ShapeRectangle : IShape
{
    public double Width { get; }
    public double Height { get; }
    public ShapeRectangle(double width, double height) { Width = width; Height = height; }
    public double GetArea() => Width * Height;
    public double GetPerimeter() => 2 * (Width + Height);
}

public interface I3DShape : IShape
{
    double GetVolume();
}

public class Cube : I3DShape
{
    public double Side { get; }
    public Cube(double side) => Side = side;
    public double GetArea() => 6 * Side * Side;
    public double GetPerimeter() => 12 * Side;
    public double GetVolume() => Math.Pow(Side, 3);
}

public class ShapeInfoPrinter
{
    public void PrintShapeInfo(IShape shape)
    {
        Console.WriteLine($"Площадь: {shape.GetArea():F2}, Периметр: {shape.GetPerimeter():F2}");
    }
}

//Задание 3
public interface IDevice
{
    void Print();
    void Scan();
    void Fax();
}

public class Printer : IDevice
{
    public void Print() => Console.WriteLine("Печать");
    public void Scan() => throw new NotImplementedException();
    public void Fax() => throw new NotImplementedException();
}

public class Scanner : IDevice
{
    public void Print() => throw new NotImplementedException();
    public void Scan() => Console.WriteLine("Сканирование");
    public void Fax() => throw new NotImplementedException();
}

// Разделенные интерфейсы
public interface IPrinter { void Print(); }
public interface IScanner { void Scan(); }
public interface IFax { void Fax(); }

public class GoodPrinter : IPrinter
{
    public void Print() => Console.WriteLine("Печать (ISP)");
}

public class GoodScanner : IScanner
{
    public void Scan() => Console.WriteLine("Сканирование (ISP)");
}

public class MultifunctionDevice : IPrinter, IScanner, IFax
{
    public void Print() => Console.WriteLine("МФУ: Печать");
    public void Scan() => Console.WriteLine("МФУ: Сканирование");
    public void Fax() => Console.WriteLine("МФУ: Факс");
}

//Задание 4

public interface IPayable
{
    void Pay(decimal amount);
}

public class CreditCard : IPayable
{
    public void Pay(decimal amount) => Console.WriteLine($"Оплата картой: {amount} руб.");
}

public class Cash : IPayable
{
    public void Pay(decimal amount) => Console.WriteLine($"Оплата наличными: {amount} руб.");
}

public class PaymentService
{
    public void ProcessPayment(IPayable method, decimal amount) => method.Pay(amount);
}

public interface ILogger
{
    Task LogAsync(string message);
}

public class ConsoleLogger : ILogger
{
    public Task LogAsync(string message)
    {
        Console.WriteLine($"[LOG]: {message}");
        return Task.CompletedTask;
    }
}

public class FileLogger : ILogger
{
    public async Task LogAsync(string message)
    {
        await File.AppendAllTextAsync("log.txt", message + Environment.NewLine);
    }
}

public class Worker
{
    public async Task DoWorkAsync(ILogger logger)
    {
        await logger.LogAsync("Начало работы");
        
        await Task.Delay(100); 
        
        await logger.LogAsync("Работа завершена");
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("Задание 1");
        var point = new Point(0, 0);
        point.Move(5, 10);
        new DrawingService().DrawAll(new List<IDrawable> { new Circle(), new Rectangle() });

        Console.WriteLine("\nЗадание 2");
        var cube = new Cube(3);
        new ShapeInfoPrinter().PrintShapeInfo(cube);
        Console.WriteLine($"Объем куба: {cube.GetVolume()}");

        Console.WriteLine("\nЗадание 3");
        var mfd = new MultifunctionDevice();
        ((IPrinter)mfd).Print();

        Console.WriteLine("\nЗадание 4");
        var paymentService = new PaymentService();
        paymentService.ProcessPayment(new CreditCard(), 1500.50m);
        
        var worker = new Worker();
        
        Console.WriteLine("\nТест ConsoleLogger:");
        await worker.DoWorkAsync(new ConsoleLogger());
        
        Console.WriteLine("\nТест FileLogger (log.txt в папке):");
        await worker.DoWorkAsync(new FileLogger());
    }
}