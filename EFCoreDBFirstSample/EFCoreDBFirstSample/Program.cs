using EFCoreDBFirstSample.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using static EFCoreDBFirstSample.Models.EmployeeContext;

var builder = WebApplication.CreateBuilder(args);

DbContextOptionsBuilder optionsBuilder = new DbContextOptionsBuilder();

if (!optionsBuilder.IsConfigured)
{
    builder.Services.AddDbContext<EmployeeContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("myConn"), build =>
    {
        build.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    }));
}

builder.Services.AddControllers();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapPost("/employees/create", async (Employee employee, EmployeeContext myEmployeeDBCtxt) => {
    string storedProc = "exec uspAddEmpDetail @FirstName, @LastName, @PhoneNumber";

    var FirstName = new SqlParameter("@FirstName", employee.FirstName);
    var LastName = new SqlParameter("@LastName", employee.LastName);
    var PhoneNumber = new SqlParameter("@PhoneNumber", employee.PhoneNumber);

    var result = await myEmployeeDBCtxt.employees.FromSqlRaw
                                            (storedProc, FirstName, LastName, PhoneNumber).ToListAsync();

    return Results.Ok();
});

app.MapGet("/employees", async (EmployeeContext myEmployeeDBCtxt) =>
{
    var dbEmployees = await myEmployeeDBCtxt.employees.FromSqlRaw<Employee>("[dbo].[uspEmpDetails]").ToListAsync();
    return Results.Ok(dbEmployees);
});


app.MapPut("/employees/update", async (Employee employeeToUpdate, EmployeeContext myEmployeeDBCtxt) =>
{
    var query =
    from dbEmployee in myEmployeeDBCtxt.employees
    where dbEmployee.EmployeeId == employeeToUpdate.EmployeeId
    select dbEmployee;

    foreach (Employee dbEmployee in query)
    {
        dbEmployee.FirstName = employeeToUpdate.FirstName;
        dbEmployee.LastName = employeeToUpdate.LastName;
        dbEmployee.PhoneNumber = employeeToUpdate.PhoneNumber;
        // Insert any additional changes to column values.
    }

    // Submit the changes to the database.
    try
    {
        await myEmployeeDBCtxt.SaveChangesAsync();
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        // Provide for exceptions.
    }
    return Results.NoContent();
});

app.MapDelete("/employees/delete/{employeeId}", async (long employeeId, EmployeeContext myEmployeeDBCtxt) =>
{
    var dbEmployee = await myEmployeeDBCtxt.employees.FindAsync(employeeId);
    if (dbEmployee == null)
    {
        return Results.NoContent();
    }
    myEmployeeDBCtxt.employees.Remove(dbEmployee);
    await myEmployeeDBCtxt.SaveChangesAsync();
    return Results.Ok();
});

app.Run();


