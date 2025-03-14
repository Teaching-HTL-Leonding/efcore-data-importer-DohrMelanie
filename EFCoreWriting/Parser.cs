namespace EFCoreWriting;

public class Parser(ApplicationDataContext context)
{
    public async Task ParseFile()
    {
        var lines = await File.ReadAllLinesAsync("data.txt");
        Customer? curCustomer = null;
        foreach (var line in lines)
        {
            var parts = line.Split('|');
            switch (parts[0])
            {
                case "CUS":
                    if (curCustomer != null)
                    {
                        await CheckCustomer(curCustomer);
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
        await CheckCustomer(curCustomer);
    }
    private async Task CheckCustomer(Customer? curCustomer)
    {
        if (curCustomer != null)
        {
            await using var transaction = await context.Database.BeginTransactionAsync();
            try
            {
                if (curCustomer != null)
                {
                    await context.Customers.AddAsync(curCustomer);
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
            } catch
            {
                if (curCustomer != null)
                {
                    context.Customers.Remove(curCustomer);
                }
                await transaction.RollbackAsync();
            }
        }
    }
}
