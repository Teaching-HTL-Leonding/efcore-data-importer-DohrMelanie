namespace EFCoreWriting;

public class Importer(ApplicationDataContext context)
{
    public async Task ImportData()
    {
        context.OrderLines.RemoveRange(context.OrderLines);
        context.OrderHeaders.RemoveRange(context.OrderHeaders);
        context.Customers.RemoveRange(context.Customers);
        await context.SaveChangesAsync();
        var customers = await ParseFile();
        customers.ForEach(async c => await CheckCustomer(c));
    }

    private async Task<List<Customer>> ParseFile()
    {
        var lines = await File.ReadAllLinesAsync("data.txt");
        List<Customer> customers = [];
        Customer? curCustomer = null;
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            switch (parts[0])
            {
                case "CUS":
                    if (curCustomer != null)
                    {
                        customers.Add(curCustomer);
                    }
                    curCustomer = new Customer(0, parts[1], parts[2], parts[3]);
                    break;
                case "OH":
                    curCustomer!.Orders.Add(new OrderHeader(0, curCustomer.ID, curCustomer, DateOnly.Parse(parts[1]), parts[2], parts[3], parts[4]));
                    break;
                case "OL":
                    curCustomer!.Orders.Last().OrderLines.Add(new OrderLine(0, 0, curCustomer.Orders.Last(), parts[1], int.Parse(parts[2]), decimal.Parse(parts[3])));
                    break;
            }
        }
        if (curCustomer != null)
        {
            customers.Add(curCustomer);
        }
        return customers;
    }
    private async Task CheckCustomer(Customer curCustomer)
    {
        await using var transaction = await context.Database.BeginTransactionAsync();
        try
        {
            await context.Customers.AddAsync(curCustomer);
            await context.SaveChangesAsync();
            await transaction.CommitAsync();
        } catch
        {
            context.Customers.Remove(curCustomer);
            await transaction.RollbackAsync();
        }
    }
}
