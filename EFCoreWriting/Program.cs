using EFCoreWriting;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace EFCoreWriting;

public class Program
{
    public static void Main()
    {
        var dbContextFactory = new ApplicationDataContextFactory();
        using var context = dbContextFactory.CreateDbContext([]);
        var importer = new Importer(context);
        importer.ImportData().Wait();
    }

    
    private static async Task fromLesson()
    {
        var factory = new ApplicationDataContextFactory();
        var context = factory.CreateDbContext(args: []);

        var customer = new Customer(0, "Hogwarts", "UK", "Scotland");
        Console.WriteLine($"Customer ID: {customer.ID}");
        Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
        Console.WriteLine($"Customer Tracking State: {context.Entry(customer).State}");

        context.Customers.Add(customer);
        Console.WriteLine($"Customer ID (after Add): {customer.ID}");
        Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
        Console.WriteLine($"Customer Tracking State: {context.Entry(customer).State}");

        await context.SaveChangesAsync();
        Console.WriteLine($"Customer ID (after SaveChanges): {customer.ID}");
        Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(customer)}");
        Console.WriteLine($"Customer Tracking State: {context.Entry(customer).State}");

        var reReadCustomer = await context.Customers.FindAsync(customer.ID);
        Console.WriteLine($"Customer ID (after re-read): {reReadCustomer!.ID}");
        Console.WriteLine($"Customer Hashcode: {RuntimeHelpers.GetHashCode(reReadCustomer)}");

        customer.CompanyName = "Foo"; // Update
        Console.WriteLine($"Customer Companyname: {reReadCustomer!.CompanyName}"); // will be foo
        Console.WriteLine($"Customer Tracking State: {context.Entry(customer).State}");

        using (var connection = context.Database.GetDbConnection()) // read mit sql statement kommt bei Test nicht, nur dass mans weiß wies geht
        {
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM Customers";
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                Console.WriteLine(reader["CompanyName"]);
            }
        }

        context.Customers.Remove(customer); // Delete
        await context.SaveChangesAsync(); // good practice to call once after all changes

        // context.Database.ExecuteSqlAsync() um sql auszuführen, das ef core nicht unterstützt

        /*
        var customer2 = new Customer(1, "HTL Leonding", "AT", "Upper Austria");
        customer2.CompanyName += " School of Hacking"; // no update will be done

        customer.CompanyName += " School of Witchcraft and Wizardry";
        await context.SaveChangesAsync();
        */
    
        customer = new Customer(0, "Hogwarts", "UK", "Scotland");
        customer.Orders.Add(new OrderHeader(0, 0, customer, new DateOnly(2022, 1, 1), "UK", "EXW", "Net 30")); // with Add and not async because Orders is a List
        await context.SaveChangesAsync();
        Console.WriteLine($"Customer ID: {customer.ID}");
        Console.WriteLine($"Customer ID from Order Header: {customer.Orders.First().CustomerID}");

        //context.OrderHeaders.Add(customer.Orders.First()); // doesn't make sense because it is already in the database
        /*
        var customers = await context.Customers.ToListAsync(); // does only load customers, not orders
        customers = await context.Customers.Include(c => c.Orders).ToListAsync(); // loads customers and orders
        foreach (var c in customers)
        {
            Console.WriteLine($"Customer {c.ID} has {c.Orders[0].OrderDate}");
        }
        */
    
        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            Customer? c1;
            try
            {
                await context.OrderHeaders.ExecuteDeleteAsync();
                await context.Customers.ExecuteDeleteAsync(); // data will still be gone even if there is an exception later on

                c1 = new Customer(0, "Hogwarts", "UK", "Scotland");
                var c2 = new Customer(0, "Hogwarts", "UKZ", "Scotland"); // check constraint will fail
                context.Customers.AddRange(c1, c2);
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
