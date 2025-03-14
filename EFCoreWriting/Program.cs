using EFCoreWriting;

var factory = new ApplicationDataContextFactory();
var context = factory.CreateDbContext(args: []);

var customer = new Customer(0, "Hogwarts", "UK", "Scotland");

context.Customers.Add(customer);
await context.SaveChangesAsync();

customer.CompanyName += " School of Witchcraft and Wizardry";
await context.SaveChangesAsync();
