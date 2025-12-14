using Microsoft.EntityFrameworkCore;
using EFLab.Testing;
using EFLab.Models;
using EFLab.Data;

namespace EFLab.Tests;

/// <summary>
/// Many-to-Many Relationships: Implicit and explicit join tables, common pitfalls
/// </summary>
public static class ManyToManyTests
{
    [Tutorial(
        title: "Implicit Many-to-Many: Adding Duplicates Silently",
        category: "Many-to-Many Relationships",
        concept: @"EF Core 5.0+ supports implicit many-to-many relationships where EF automatically creates and manages the join table.

Key points:
- Define collection navigation properties on both entities
- No explicit join entity class needed
- EF creates the join table automatically
- Convention-based naming: {Entity1}{Entity2} (e.g., StudentCourse)
- Simpler code, less boilerplate
- Use for simple relationships without additional data

Example:
```csharp
class Student { public List<Course> Courses { get; set; } }
class Course { public List<Student> Students { get; set; } }
```

EF automatically creates StudentCourse join table with StudentId and CourseId.",
        pitfall: @"**Common Mistake:** Adding the same relationship twice creates duplicate join entries.

This test demonstrates:
1. Create a Student enrolled in a Course
2. Later, add the same Course to the Student again
3. EF doesn't prevent duplicate entries in the join table
4. SaveChanges succeeds but creates duplicate relationship

The test FAILS because we end up with duplicate Student-Course entries.",
        fix: @"**Solution:** Check if the relationship already exists before adding:

```csharp
// Bad: Blindly adding
student.Courses.Add(courseFromDb);
context.SaveChanges(); // Might create duplicate

// Good: Check first
if (!student.Courses.Any(c => c.Id == courseId))
{
    student.Courses.Add(courseFromDb);
}

// Or use HashSet instead of List
public HashSet<Course> Courses { get; set; } = new();

// Or query to check
var isEnrolled = context.Students
    .Where(s => s.Id == studentId)
    .SelectMany(s => s.Courses)
    .Any(c => c.Id == courseId);
if (!isEnrolled)
{
    student.Courses.Add(course);
}
```

Always verify relationships before adding to prevent duplicates.",
        order: 35
    )]
    public static void Test_Implicit_ManyToMany_Duplicate_Relationships()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Implicit_ManyToMany_Duplicates")
            .Options;

        int studentId, courseId;

        // Setup: Create student and course with relationship
        using (var context = new AppDbContext(options))
        {
            var student = new Student { Name = "John Doe", Email = "john@university.edu" };
            var course = new Course { Name = "Entity Framework 101", Code = "CS401", Credits = 3 };
            
            student.Courses.Add(course);
            
            context.Students.Add(student);
            context.SaveChanges();
            
            studentId = student.Id;
            courseId = course.Id;
        }

        // BUG: Add the same course again (creates duplicate)
        using (var context = new AppDbContext(options))
        {
            var student = context.Students
                .Include(s => s.Courses)
                .First(s => s.Id == studentId);
            
            var course = context.Courses.Find(courseId);
            
            // This will add a duplicate relationship
            student.Courses.Add(course!);
            context.SaveChanges();
        }

        // Verify: Should have only 1 course, but might have duplicates
        using (var context = new AppDbContext(options))
        {
            var student = context.Students
                .Include(s => s.Courses)
                .First(s => s.Id == studentId);
            
            var courseCount = student.Courses.Count;
            
            // With In-Memory, this might not create duplicates (limitation)
            // But with real databases, you could get duplicate join entries
            Assert.AreEqual(1, courseCount,
                $"Student should have 1 unique course, but has {courseCount}. " +
                "Always check if relationship exists before adding! " +
                "Use HashSet or check with Any() to prevent duplicates.");
        }
    }

    [Tutorial(
        title: "Explicit Many-to-Many: Required for Custom Join Data",
        category: "Many-to-Many Relationships",
        concept: @"Explicit many-to-many relationships require a join entity class when you need additional data on the relationship itself.

Common scenarios:
- DateAdded/CreatedDate on the relationship
- Status/Role information
- Priority/Order values
- Hours allocated or other metrics
- Additional metadata

Example with EmployeeProject join entity:
```csharp
class Employee { public List<EmployeeProject> EmployeeProjects { get; set; } }
class Project { public List<EmployeeProject> EmployeeProjects { get; set; } }
class EmployeeProject 
{
    public int EmployeeId { get; set; }
    public Employee Employee { get; set; }
    public int ProjectId { get; set; }
    public Project Project { get; set; }
    public DateTime AssignedDate { get; set; }
    public string Role { get; set; } // e.g., ""Lead"", ""Developer""
    public int HoursAllocated { get; set; }
}
```

You navigate through the join entity: employee.EmployeeProjects[0].Project",
        pitfall: @"**Common Mistake:** Forgetting to set composite key or missing required join entity properties.

This test demonstrates:
1. Define explicit join entity but forget composite key
2. Or forget to set required properties on the join entity
3. Relationship doesn't work as expected

The test FAILS to show proper explicit relationship setup.",
        fix: @"**Solution:** Properly configure the join entity with composite key:

```csharp
// In DbContext.OnModelCreating:
modelBuilder.Entity<EmployeeProject>()
    .HasKey(ep => new { ep.EmployeeId, ep.ProjectId }); // Composite key

modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Employee)
    .WithMany(e => e.EmployeeProjects)
    .HasForeignKey(ep => ep.EmployeeId);

modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Project)
    .WithMany(p => p.EmployeeProjects)
    .HasForeignKey(ep => ep.ProjectId);
```

When adding relationships, create the join entity:
```csharp
var employeeProject = new EmployeeProject
{
    EmployeeId = employeeId,
    ProjectId = projectId,
    AssignedDate = DateTime.Now,
    Role = ""Lead Developer"",
    HoursAllocated = 40
};
context.EmployeeProjects.Add(employeeProject);
```",
        order: 36
    )]
    public static void Test_Explicit_ManyToMany_Missing_Join_Entity_Properties()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Explicit_ManyToMany")
            .Options;

        int employeeId, projectId;

        // Setup: Create employee and project
        using (var context = new AppDbContext(options))
        {
            var employee = new Employee { Name = "Jane Smith", Department = "Engineering" };
            var project = new Project { Name = "EF Tutorial", Description = "Entity Framework Core Tutorial" };
            
            context.Employees.Add(employee);
            context.Projects.Add(project);
            context.SaveChanges();
            
            employeeId = employee.Id;
            projectId = project.Id;
        }

        // BUG: Try to add relationship without creating join entity properly
        using (var context = new AppDbContext(options))
        {
            var employee = context.Employees.Find(employeeId);
            var project = context.Projects.Find(projectId);
            
            // With explicit join entities, you can't just add to navigation collections
            // You must create the join entity with its additional properties
            
            // This WRONG approach might work with implicit, but not with explicit:
            // employee.Projects.Add(project); // Won't compile! No direct navigation
            
            // Must create EmployeeProject explicitly:
            var employeeProject = new EmployeeProject
            {
                EmployeeId = employeeId,
                ProjectId = projectId,
                AssignedDate = DateTime.Now,
                Role = "Lead Developer",
                HoursAllocated = 40
            };
            context.EmployeeProjects.Add(employeeProject);
            context.SaveChanges();
        }

        // Verify: Check if relationship and metadata exist
        using (var context = new AppDbContext(options))
        {
            var employeeProject = context.EmployeeProjects
                .Include(ep => ep.Employee)
                .Include(ep => ep.Project)
                .FirstOrDefault(ep => ep.EmployeeId == employeeId && ep.ProjectId == projectId);
            
            Assert.IsNotNull(employeeProject, "EmployeeProject join entity should exist");
            Assert.IsTrue(!string.IsNullOrEmpty(employeeProject.Role), "Role should be set");
            
            // Test 'fails' to teach proper explicit relationship setup
            Assert.IsTrue(employeeProject.AssignedDate == default(DateTime),
                "Explicit many-to-many requires creating join entity with ALL properties! " +
                "Cannot use collection navigation directly like implicit relationships. " +
                "Always create EmployeeProject with EmployeeId, ProjectId, AssignedDate, Role, HoursAllocated, etc.");
        }
    }

    [Tutorial(
        title: "Removing Many-to-Many Relationships Without Deleting Entities",
        category: "Many-to-Many Relationships",
        concept: @"Removing a many-to-many relationship means deleting the join table entry, NOT the related entities.

Key points:
- Remove from collection navigation (implicit)
- Delete join entity (explicit)
- Original entities remain in the database
- Only the relationship is removed

Common confusion: Remove vs. Delete
- Remove(entity): Removes from collection
- Remove(joinEntity): Deletes join table row
- Both entities (Student, Course) stay in database",
        pitfall: @"**Common Mistake:** Trying to remove relationship but accidentally deleting the entity itself.

This test demonstrates:
1. Student has multiple Courses
2. Try to remove one Course from Student
3. But the approach might delete the Course entity instead of just the relationship

The test FAILS to show proper relationship removal.",
        fix: @"**Solution - Implicit Many-to-Many:**
```csharp
// Load student with courses
var student = context.Students
    .Include(s => s.Courses)
    .First(s => s.Id == studentId);

// Find and remove the course from collection
var courseToRemove = student.Courses.First(c => c.Code == ""CS402"");
student.Courses.Remove(courseToRemove);
context.SaveChanges(); // Only join entry deleted
```

**Solution - Explicit Many-to-Many:**
```csharp
// Find the join entity
var employeeProject = context.EmployeeProjects
    .First(ep => ep.EmployeeId == employeeId && ep.ProjectId == projectId);

// Remove the join entity (not the Project itself)
context.EmployeeProjects.Remove(employeeProject);
context.SaveChanges();

// Project entity still exists in Projects table
```

Never use context.Courses.Remove() unless you want to delete the Course entity!",
        order: 37
    )]
    public static void Test_Removing_ManyToMany_Relationship_Without_Deleting_Entity()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Remove_Relationship")
            .Options;

        int studentId, mathCourseId, csCourseId;

        // Setup: Student with multiple courses
        using (var context = new AppDbContext(options))
        {
            var student = new Student { Name = "Bob Johnson", Email = "bob@university.edu" };
            var mathCourse = new Course { Name = "Calculus I", Code = "MATH201", Credits = 4 };
            var csCourse = new Course { Name = "Data Structures", Code = "CS402", Credits = 3 };
            
            student.Courses.Add(mathCourse);
            student.Courses.Add(csCourse);
            
            context.Students.Add(student);
            context.SaveChanges();
            
            studentId = student.Id;
            mathCourseId = mathCourse.Id;
            csCourseId = csCourse.Id;
        }

        // Test: Remove one course relationship
        using (var context = new AppDbContext(options))
        {
            var student = context.Students
                .Include(s => s.Courses)
                .First(s => s.Id == studentId);
            
            // Remove CS course relationship (NOT the course entity)
            var csCourse = student.Courses.First(c => c.Id == csCourseId);
            student.Courses.Remove(csCourse);
            context.SaveChanges();
        }

        // Verify: Student should have 1 course, and CS course should still exist
        using (var context = new AppDbContext(options))
        {
            var student = context.Students
                .Include(s => s.Courses)
                .First(s => s.Id == studentId);
            
            Assert.AreEqual(1, student.Courses.Count, 
                "Student should have 1 course after removing CS course");
            Assert.AreEqual("MATH201", student.Courses.First().Code, 
                "Remaining course should be MATH201");
            
            // The CS course entity should still exist in the database
            var csCourseStillExists = context.Courses.Any(c => c.Id == csCourseId);
            
            Assert.IsTrue(csCourseStillExists,
                "Removing relationship should NOT delete the Course entity! " +
                "Use collection.Remove() for implicit relationships, " +
                "or context.JoinEntity.Remove() for explicit relationships. " +
                "The related entities (Student, Course) remain in database.");
        }
    }

    [Tutorial(
        title: "Cascading Many-to-Many: Deleting Entity Removes Relationships",
        category: "Many-to-Many Relationships",
        concept: @"When you delete an entity involved in a many-to-many relationship, EF should automatically delete the join table entries.

Default behavior:
- Delete Employee: All EmployeeProject entries deleted (cascade)
- Delete Project: All EmployeeProject entries deleted (cascade)
- Join table entries are dependent data

Configure cascade delete:
```csharp
modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Employee)
    .WithMany(e => e.EmployeeProjects)
    .OnDelete(DeleteBehavior.Cascade); // Default
```

This prevents orphaned join table entries.",
        pitfall: @"**Common Mistake:** Deleting entity without loading relationships, leaving orphaned join entries.

This test demonstrates:
1. Employee has Projects (join entries exist)
2. Delete Employee without loading Projects
3. Join entries might become orphaned (depending on cascade delete configuration)

The test shows what happens with improper deletion.",
        fix: @"**Solution:** EF Core handles cascade delete automatically for properly configured relationships:

```csharp
// Explicit many-to-many: EF handles it
var employee = context.Employees.Find(employeeId);
context.Employees.Remove(employee);
context.SaveChanges(); // Join entries deleted automatically

// Configure cascade properly
modelBuilder.Entity<EmployeeProject>()
    .HasOne(ep => ep.Employee)
    .WithMany(e => e.EmployeeProjects)
    .OnDelete(DeleteBehavior.Cascade);
```

No need to manually delete join entities if cascade is configured correctly.

With In-Memory database, cascade delete might not work as expected (limitation). Use SQLite for realistic testing.",
        order: 38
    )]
    public static void Test_Deleting_Entity_Should_Remove_All_Relationships()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Cascade_ManyToMany")
            .Options;

        int employeeId;

        // Setup: Employee with projects
        using (var context = new AppDbContext(options))
        {
            var employee = new Employee { Name = "Temp Employee", Department = "IT" };
            var project1 = new Project { Name = "Project Alpha", Description = "First Project" };
            var project2 = new Project { Name = "Project Beta", Description = "Second Project" };
            
            var ep1 = new EmployeeProject 
            { 
                Employee = employee, 
                Project = project1, 
                AssignedDate = DateTime.Now,
                Role = "Developer",
                HoursAllocated = 20
            };
            var ep2 = new EmployeeProject 
            { 
                Employee = employee, 
                Project = project2, 
                AssignedDate = DateTime.Now,
                Role = "Tester",
                HoursAllocated = 10
            };
            
            context.EmployeeProjects.AddRange(ep1, ep2);
            context.SaveChanges();
            
            employeeId = employee.Id;
        }

        // Delete the employee
        using (var context = new AppDbContext(options))
        {
            var employee = context.Employees.Find(employeeId);
            context.Employees.Remove(employee!);
            context.SaveChanges();
        }

        // Verify: Employee deleted and join entries should be removed
        using (var context = new AppDbContext(options))
        {
            var employeeExists = context.Employees.Any(e => e.Id == employeeId);
            Assert.IsTrue(!employeeExists, "Employee should be deleted");
            
            // Check if join entries were removed
            var orphanedJoinEntries = context.EmployeeProjects
                .Where(ep => ep.EmployeeId == employeeId)
                .ToList();
            
            // With In-Memory, cascade delete might not work
            // With real databases (SQLite, SQL Server), join entries should be deleted
            Assert.AreEqual(0, orphanedJoinEntries.Count,
                "Join table entries should be deleted when entity is deleted! " +
                "Configure cascade delete: OnDelete(DeleteBehavior.Cascade). " +
                "In-Memory database may not enforce cascade delete (limitation). " +
                $"Found {orphanedJoinEntries.Count} orphaned entries.");
        }
    }

    [Tutorial(
        title: "Querying Many-to-Many: Include vs. ThenInclude",
        category: "Many-to-Many Relationships",
        concept: @"Querying many-to-many relationships requires understanding Include() and ThenInclude():

**Implicit Many-to-Many (Student-Course):**
```csharp
// Direct navigation
var students = context.Students
    .Include(s => s.Courses)
    .ToList();
// students[0].Courses is populated
```

**Explicit Many-to-Many (Employee-Project):**
```csharp
// Must navigate through join entity
var employees = context.Employees
    .Include(e => e.EmployeeProjects)
        .ThenInclude(ep => ep.Project)
    .ToList();
// employees[0].EmployeeProjects[0].Project is populated
```

ThenInclude chains navigation from the included collection.",
        pitfall: @"**Common Mistake:** Forgetting Include() or using wrong navigation path for explicit relationships.

This test demonstrates:
1. Query students expecting courses
2. Forget to Include relationships
3. Navigation properties are null/empty

The test FAILS to show proper eager loading.",
        fix: @"**Solution - Implicit Relationships:**
```csharp
var students = context.Students
    .Include(s => s.Courses)
    .ToList();
// students[0].Courses is loaded
```

**Solution - Explicit Relationships:**
```csharp
// Wrong: This loads EmployeeProjects but not Projects
var employees = context.Employees
    .Include(e => e.EmployeeProjects)
    .ToList();
// employees[0].EmployeeProjects[0].Project is NULL!

// Correct: Use ThenInclude
var employees = context.Employees
    .Include(e => e.EmployeeProjects)
        .ThenInclude(ep => ep.Project)
    .ToList();
// employees[0].EmployeeProjects[0].Project is loaded

// Can chain further
var employees = context.Employees
    .Include(e => e.EmployeeProjects)
        .ThenInclude(ep => ep.Project)
    .ToList();
```",
        order: 39
    )]
    public static void Test_Querying_ManyToMany_Requires_Proper_Include()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: "Test_Query_ManyToMany")
            .Options;

        // Setup: Students with courses
        using (var context = new AppDbContext(options))
        {
            var student1 = new Student { Name = "Alice Brown", Email = "alice@university.edu" };
            var student2 = new Student { Name = "Charlie Davis", Email = "charlie@university.edu" };
            
            var efCourse = new Course { Name = "Entity Framework", Code = "CS501", Credits = 3 };
            var dbCourse = new Course { Name = "Database Design", Code = "CS502", Credits = 3 };
            
            student1.Courses.Add(efCourse);
            student1.Courses.Add(dbCourse);
            student2.Courses.Add(dbCourse);
            
            context.Students.AddRange(student1, student2);
            context.SaveChanges();
        }

        // BUG: Query without Include
        using (var context = new AppDbContext(options))
        {
            var students = context.Students.ToList();
            
            var firstStudent = students.First();
            var courseCount = firstStudent.Courses?.Count ?? 0;
            
            // Courses collection is not loaded (lazy loading disabled by default)
            Assert.IsTrue(courseCount > 0,
                "Forgot to Include() related entities! " +
                "Implicit: .Include(s => s.Courses) " +
                "Explicit: .Include(e => e.EmployeeProjects).ThenInclude(ep => ep.Project) " +
                $"Courses collection is empty (found {courseCount} courses).");
        }
    }
}
