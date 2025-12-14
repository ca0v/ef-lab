namespace EFLab.Models;

/// <summary>
/// Simple entity for demonstrating EF Core concepts
/// </summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
}

/// <summary>
/// Entity for demonstrating relationships
/// </summary>
public class Order
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal PriceAtOrder { get; set; }
}

/// <summary>
/// Student entity for demonstrating IMPLICIT many-to-many relationships
/// EF Core automatically creates and manages the join table
/// </summary>
public class Student
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    
    // Implicit many-to-many: EF creates StudentCourse join table automatically
    public List<Course> Courses { get; set; } = new();
}

/// <summary>
/// Course entity for demonstrating IMPLICIT many-to-many relationships
/// </summary>
public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int Credits { get; set; }
    
    // Implicit many-to-many
    public List<Student> Students { get; set; } = new();
}

/// <summary>
/// Employee entity for demonstrating EXPLICIT many-to-many relationships
/// Navigate through the join entity to access additional properties
/// </summary>
public class Employee
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    
    // Explicit many-to-many: Navigate through join entity
    public List<EmployeeProject> EmployeeProjects { get; set; } = new();
}

/// <summary>
/// Project entity for demonstrating EXPLICIT many-to-many relationships
/// </summary>
public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Explicit many-to-many
    public List<EmployeeProject> EmployeeProjects { get; set; } = new();
}

/// <summary>
/// Explicit join entity for Employee-Project with additional properties
/// Use this pattern when you need data on the relationship itself
/// </summary>
public class EmployeeProject
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; } = null!;
    
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;
    
    // Additional properties on the relationship
    public DateTime AssignedDate { get; set; }
    public string Role { get; set; } = string.Empty; // e.g., "Lead", "Developer", "Tester"
    public int HoursAllocated { get; set; }
}
