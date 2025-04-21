using Microsoft.EntityFrameworkCore;
using Client_Invoice_System.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using Client_Invoice_System.Models;

namespace Client_Invoice_System.Services
{
    public class InvoiceService
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public InvoiceService(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Retrieves all invoices with related client and invoice item details.
        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Client)
                    .ThenInclude(r=>r.CountryCurrency)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.Employee)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching invoices: {ex.Message}");
                return new List<Invoice>();
            }
        }

        // Marks an invoice as paid (or partially paid) by updating PaidAmount, RemainingAmount, and InvoiceStatus.
        public async Task MarkInvoiceAsPaidAsync(int invoiceId, decimal paidAmount)
        {
            try
            {
                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
                if (invoice == null)
                {
                    Console.WriteLine($"⚠️ Invoice with ID {invoiceId} not found.");
                    return;
                }

                invoice.PaidAmount += paidAmount;
                invoice.RemainingAmount = invoice.TotalAmount - invoice.PaidAmount;
                invoice.InvoiceStatuses = invoice.PaidAmount >= invoice.TotalAmount
                    ? InvoiceStatus.Paid
                    : InvoiceStatus.PartiallyPaid;

                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Invoice {invoiceId} updated with additional payment of {paidAmount:C}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error marking invoice as paid: {ex.Message}");
            }
        }

        // Updates an existing invoice.
        public async Task<bool> UpdateInvoiceAsync(Invoice updatedInvoice)
        {
            try
            {
                var existingInvoice = await _context.Invoices
                    .AsNoTracking()
                    .FirstOrDefaultAsync(i => i.InvoiceId == updatedInvoice.InvoiceId);

                if (existingInvoice == null)
                {
                    Console.WriteLine($"⚠️ Invoice with ID {updatedInvoice.InvoiceId} not found.");
                    return false;
                }

                existingInvoice.TotalAmount = updatedInvoice.TotalAmount;
                existingInvoice.PaidAmount = updatedInvoice.PaidAmount;
                existingInvoice.RemainingAmount = updatedInvoice.RemainingAmount;
                existingInvoice.InvoiceStatuses = updatedInvoice.InvoiceStatuses;
                existingInvoice.EmailStatus = updatedInvoice.EmailStatus;

                _context.Invoices.Update(existingInvoice);
                await _context.SaveChangesAsync();

                Console.WriteLine($"✅ Invoice {updatedInvoice.InvoiceId} updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating invoice: {ex.Message}");
                return false;
            }
        }

        // Saves an invoice for a client by updating an existing unpaid invoice or creating a new one for uninvoiced resources.
        public async Task<List<int>> SaveInvoiceAsync(int clientId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null)
                    throw new Exception("Client not found!");

                // Filter invoiced resources based on InvoiceDate
                var invoicedResourceIds = await _context.InvoiceItems
                    .Where(ii => ii.Invoice.ClientId == clientId &&
                                 (ii.Invoice.InvoiceStatuses == InvoiceStatus.Pending ||
                                  ii.Invoice.InvoiceStatuses == InvoiceStatus.PartiallyPaid) &&
                                 (!startDate.HasValue || ii.Invoice.InvoiceDate >= startDate) &&
                                 (!endDate.HasValue || ii.Invoice.InvoiceDate <= endDate))
                    .Select(ii => ii.ResourceId)
                    .ToListAsync();

                // Filter resources that are not already invoiced
                var newResources = client.Resources
                    .Where(r => !invoicedResourceIds.Contains(r.ResourceId) &&
                                (!startDate.HasValue || r.CreatedAt <= endDate))
                    .ToList();

                if (!newResources.Any())
                {
                    Console.WriteLine($"⚠️ No new resources to invoice for Client {clientId} within the selected date range.");
                    return new List<int>();
                }

                List<int> newInvoiceIds = new List<int>();

                foreach (var resource in newResources)
                {
                    var newInvoice = new Invoice
                    {
                        ClientId = clientId,
                        InvoiceDate = DateTime.UtcNow,
                        TotalAmount = resource.ConsumedTotalHours * resource.Employee.HourlyRate,
                        PaidAmount = 0,
                        RemainingAmount = resource.ConsumedTotalHours * resource.Employee.HourlyRate,
                        CountryCurrencyId = client.CountryCurrencyId,
                        InvoiceStatuses = InvoiceStatus.Pending,
                        EmailStatus = "Not Sent"
                    };

                    await _context.Invoices.AddAsync(newInvoice);
                    await _context.SaveChangesAsync();

                    var invoiceItem = new InvoiceItem
                    {
                        InvoiceId = newInvoice.InvoiceId,
                        ResourceId = resource.ResourceId,
                        ConsumedHours = resource.ConsumedTotalHours,
                        RatePerHour = resource.Employee.HourlyRate
                    };

                    newInvoice.InvoiceItems.Add(invoiceItem);

                    await _context.SaveChangesAsync();
                    newInvoiceIds.Add(newInvoice.InvoiceId);

                    Console.WriteLine($"✅ Created new Invoice {newInvoice.InvoiceId} for Resource {resource.ResourceId} with amount: {newInvoice.TotalAmount:C}.");
                }

                return newInvoiceIds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving invoices: {ex.Message}");
                throw;
            }
        }

        // Sends an invoice email and updates the EmailStatus.
        public async Task<bool> SendInvoiceToClientAsync(int clientId, DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var client = await _context.Clients
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.CountryCurrency)
                    .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found.");

                // Retrieve the most recent unpaid invoice (Pending or PartiallyPaid)
                var invoice = await _context.Invoices
                    .Where(i => i.ClientId == clientId && i.InvoiceStatuses != InvoiceStatus.Paid)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                    .OrderByDescending(i => i.InvoiceDate)
                    .FirstOrDefaultAsync();

                if (invoice == null)
                    throw new Exception("No unpaid invoice found.");

                // Generate invoice PDF with start and end date
                byte[] invoicePdf = await GenerateInvoicePdfAsync(clientId, startDate, endDate);
                string fileName = $"Invoice_{clientId}_{invoice.InvoiceDate:yyyyMMdd}.pdf";

                // Send invoice email
                await _emailService.SendInvoiceEmailAsync(client.Email, invoicePdf, fileName);

                // Update email status
                invoice.EmailStatus = "Sent";
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error sending invoice: {ex.Message}");
                return false;
            }
        }

        // Deletes an invoice.
        public async Task DeleteInvoiceAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice == null)
                {
                    return;
                }

                invoice.IsDeleted = true;
                await _context.SaveChangesAsync();

            }
            catch (Exception ex)
            {
                throw;
            }
        }



        // Generates a PDF for the given client using QuestPDF
        public async Task<byte[]> GenerateInvoicePdfAsync(int clientId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
               var client = await _context.Clients
                .Where(c => c.ClientId == clientId)
                .Include(c => c.CountryCurrency)
                .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found!");

                var unpaidInvoicesQuery = _context.Invoices
                    .Where(i => i.ClientId == clientId && i.InvoiceStatuses != InvoiceStatus.Paid)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.Employee)
                    .Include(i => i.InvoiceItems)
                        .ThenInclude(ii => ii.Resource)
                            .ThenInclude(r => r.OwnerProfile)
                    .Include(i => i.CountryCurrency)
                    .Include(i => i.Client).AsQueryable();

                // Apply date filter only if both startDate and endDate are provided
                if (startDate.HasValue && endDate.HasValue)
                {
                    unpaidInvoicesQuery = unpaidInvoicesQuery
                        .Where(i => i.InvoiceDate >= startDate.Value && i.InvoiceDate <= endDate.Value);
                }

                var unpaidInvoices = await unpaidInvoicesQuery
                    .OrderByDescending(i => i.InvoiceDate)
                    .ToListAsync();

                if (!unpaidInvoices.Any())
                    throw new Exception("No unpaid invoice found!");

                // Flatten invoice items from all unpaid invoices
                List<InvoiceItem> invoiceItems;
                if (startDate.HasValue && endDate.HasValue)
                {
                    invoiceItems = unpaidInvoices
                        .SelectMany(i => i.InvoiceItems)
                        .Where(ii => ii.Resource.CreatedAt >= startDate.Value && ii.Resource.CreatedAt <= endDate.Value)
                        .ToList();
                }
                else
                {
                    invoiceItems = unpaidInvoices.SelectMany(i => i.InvoiceItems).ToList();
                }

                if (!invoiceItems.Any())
                    throw new Exception("No resource consumption found for the selected date range.");

                // Recalculate totalAmount based on all unpaid invoices
                decimal totalAmount = invoiceItems.Sum(ii => ii.TotalAmount);
                decimal paidAmount = unpaidInvoices.Sum(i => i.PaidAmount);
                decimal remainingAmount = totalAmount - paidAmount;
                bool showRemainingColumn = remainingAmount > 0;




                var ownerProfile = invoiceItems.FirstOrDefault()?.Resource.OwnerProfile ?? new OwnerProfile
                {
                    OwnerName = "Default Owner",
                    BillingEmail = "default@email.com",
                    PhoneNumber = "+923000000000",
                    BillingAddress = "Default Billing Address",
                    CountryCurrency = new CountryCurrency { CurrencyCode = "USD" },
                    BankName = "Default Bank",
                    Swiftcode = "DFLTUS33XXX",
                    AccountTitle = "Default Account",
                    IBANNumber = "US00DEFAULTIBAN",
                    BranchAddress = "Default Branch Address",
                    BeneficeryAddress = "Default Beneficiary Address",
                    AccountNumber = "0000000000"
                };

                // Determine culture and currency symbol
                CultureInfo culture;
                string currencySymbol = client?.CountryCurrency?.Symbol ?? "$"; 
                string currencyname = client?.CountryCurrency?.CurrencyName ?? "USD";
                if (!string.IsNullOrEmpty(client?.CountryCurrency?.CurrencyCode))
                {
                    try
                    {
                        culture = new CultureInfo(client.CountryCurrency.CurrencyCode);
                    }
                    catch (CultureNotFoundException)
                    {
                        culture = new CultureInfo("en-US"); 
                    }
                }
                else
                {
                    culture = new CultureInfo("en-US"); 
                }

                using (MemoryStream ms = new MemoryStream())
                {
                    QuestPDF.Settings.License = LicenseType.Community;

                    Document.Create(container =>
                    {
                        container.Page(page =>
                        {
                            page.Size(PageSizes.A4);
                            page.Margin(30);

                            // HEADER
                            page.Header().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(80);
                                    columns.ConstantColumn(120);
                                    columns.ConstantColumn(180);
                                    columns.ConstantColumn(150);
                                });

                                table.Cell().Border(1).Padding(2).AlignLeft().Text("Atrule Technologies").Bold();
                                table.Cell().Border(1).Padding(2).AlignLeft().Text("From\nAtrule Technologies,\n2nd Floor, Khawar Center, SP Chowk, Multan Pakistan");
                                table.Cell().Border(1).Padding(2).AlignLeft().Text($"To\n{client.Name}\n{client.Address}\nEmail: {client.Email}\nPhone: {client.PhoneNumber}");
                                table.Cell().Border(1).Padding(2).AlignLeft().Text($"Invoice No: INV/{DateTime.Now.Year}/000{clientId}\nDate: {DateTime.Now:MM/dd/yyyy}").Bold();
                            });

                            // CONTENT
                            page.Content().Column(col =>
                            {
                                col.Item().PaddingTop(20);

                                // Payment Instructions
                                col.Item().Container().PaddingBottom(5).Text("Payment Instructions (Wire Transfer)").Bold();
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(120);
                                        columns.RelativeColumn();
                                    });

                                    void AddPaymentRow(string label, string value)
                                    {
                                        table.Cell().Padding(2).Text(label).Bold();
                                        table.Cell().Padding(2).Text(value);
                                    }

                                    AddPaymentRow("Currency:", ownerProfile?.CountryCurrency?.CurrencyCode ?? "USD");
                                    AddPaymentRow("Bank Name:", ownerProfile.BankName);
                                    AddPaymentRow("Swift Code:", ownerProfile.Swiftcode);
                                    AddPaymentRow("Account Title:", ownerProfile.AccountTitle);
                                    AddPaymentRow("IBAN:", ownerProfile.IBANNumber);
                                    AddPaymentRow("Branch Address:", ownerProfile.BranchAddress);
                                    AddPaymentRow("Beneficiary Address:", ownerProfile.BeneficeryAddress);
                                });

                                col.Item().PaddingTop(10);

                                // Service Details Table
                                col.Item().Table(serviceTable =>
                                {
                                    // Define table columns.
                                    serviceTable.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();    
                                        columns.ConstantColumn(60);   
                                        columns.ConstantColumn(80);   
                                        columns.ConstantColumn(120); 
                                        if (showRemainingColumn) columns.ConstantColumn(120);
                                    });

                                    // Table Header.
                                    serviceTable.Header(header =>
                                    {

                                        string headerColor = "#2F4F4F";
                                        header.Cell().Border(1).Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span("Description").FontColor(Colors.White).Bold());
                                        header.Cell().Border(1).Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span("Quantity").FontColor(Colors.White).Bold());
                                        header.Cell().Border(1).Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span($"Rate ({currencySymbol})").FontColor(Colors.White).Bold());
                                        header.Cell().Border(1).Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span($"Subtotal ({currencySymbol})").FontColor(Colors.White).Bold());
                                        if (showRemainingColumn)
                                        {
                                            header.Cell().Border(1).Background(Color.FromHex(headerColor)).Padding(5)
                                                .Text($"Remaining Amount ({currencySymbol})").FontColor(Colors.White).Bold();
                                        }
                                    });

                                    //bool showRemainingColumn = (paidAmount > 0 && remainingAmount > 0);

                                    foreach (var item in invoiceItems)
                                    {
                                        decimal subtotal = item.TotalAmount; 

                                        serviceTable.Cell().ColumnSpan(5).Border(1).Padding(5)
                                            //.Text($"{item.Resource.ResourceName} - {item.Resource.Employee.EmployeeName} - Monthly Contract - {unpaidInvoice.InvoiceDate:MMMM yyyy}");
                                            .Text($"{item.Resource.ResourceName} - {item.Resource.Employee.EmployeeName} - Monthly Contract - {DateTime.Now:MMMM yyyy}");

                                        serviceTable.Cell().ColumnSpan(1).Border(1).Padding(5)
                                            .Text($"Calculation\nAmount in {currencyname}: {item.ConsumedHours} Hours X {currencySymbol}{item.RatePerHour:F2} = {currencySymbol}{subtotal:F2}")
.Italic();
                                        serviceTable.Cell().Border(1).Padding(5).AlignCenter().Text("1");
                                        serviceTable.Cell().Border(1).Padding(5).AlignCenter()
                                            .Text($"{currencySymbol}{item.RatePerHour:F2}");

                                        serviceTable.Cell().Border(1).Padding(5).AlignCenter()
                                            .Text($"{currencySymbol}{subtotal:F2}");

                                        if (showRemainingColumn)
                                        {
                                            // Get the invoice associated with this item
                                            var relatedInvoice = unpaidInvoices.FirstOrDefault(i => i.InvoiceId == item.InvoiceId);

                                            decimal remainingForResource = 0;

                                            if (relatedInvoice != null)
                                            {
                                                bool isPartiallyPaid = relatedInvoice.PaidAmount > 0 && relatedInvoice.RemainingAmount > 0;
                                                bool isFullyPaid = relatedInvoice.RemainingAmount == 0;
                                                bool isUnpaid = relatedInvoice.PaidAmount == 0 && relatedInvoice.RemainingAmount > 0;

                                                if (isPartiallyPaid)
                                                {
                                                    // Show remaining amount for partially paid invoices
                                                    remainingForResource = item.TotalAmount * (relatedInvoice.RemainingAmount / relatedInvoice.TotalAmount);
                                                }
                                                else if (isUnpaid)
                                                {
                                                    remainingForResource = 0;
                                                }
                                                else if (isFullyPaid)
                                                {
                                                    remainingForResource = 0;
                                                }
                                            }

                                            // Ensure we only show remaining for the first occurrence of this resource
                                            bool isFirstOccurrence = invoiceItems
                                                .Where(i => i.InvoiceId == item.InvoiceId)
                                                .OrderBy(i => i.ResourceId)
                                                .FirstOrDefault()?.ResourceId == item.ResourceId;

                                            serviceTable.Cell().Border(1).Padding(5).AlignCenter()
                                                .Text(isFirstOccurrence && remainingForResource > 0
                                                      ? $"{currencySymbol}{remainingForResource:F2}"
                                                      : "");
                                        }
                                        else
                                        {
                                            serviceTable.Cell().Border(1).Padding(5).AlignCenter().Text("0.00");
                                        }


                                    }

                                    // Totals Section.
                                    serviceTable.Cell().ColumnSpan(3).Border(1).Padding(5).Text("Software Consultancy Services").Bold();

                                    //uint totalColumns = showRemainingColumn ? 4u : 3u;
                                    uint totalColumns = 2u;
                                    serviceTable.Cell().ColumnSpan(totalColumns).Border(1).Table(totalTable =>
                                    {
                                        totalTable.ColumnsDefinition(subCols =>
                                        {
                                            subCols.RelativeColumn();
                                            subCols.ConstantColumn(100);
                                        });

                                        if (showRemainingColumn)
                                        {
                                            decimal totalRemaining = unpaidInvoices.Sum(i => i.RemainingAmount);

                                            totalTable.Cell().Border(1).Padding(5).Text("Total Amount").Bold();
                                            totalTable.Cell().Border(1).Padding(5).AlignRight()
                                                .Text($"{currencySymbol}{(totalRemaining > 0 ? totalRemaining : totalAmount):F2}").Bold();
                                        }
                                        else
                                        {
                                            totalTable.Cell().Border(1).Padding(5).Text("Total Amount").Bold();
                                            totalTable.Cell().Border(1).Padding(5).AlignRight()
                                                .Text($"{currencySymbol}{totalAmount:F2}").Bold();
                                        }

                                        totalTable.Cell().Border(1).Padding(5).Text("Total Due By").Bold();
                                        totalTable.Cell().Border(1).Padding(5).AlignRight()
                                            .Text($"{DateTime.Now.AddDays(5):MM/dd/yyyy}").Bold();
                                    });


                                });

                                col.Item().PaddingTop(5);
                            });

                            // FOOTER.
                            page.Footer().AlignCenter().Text("Email: suleman@atrule.com | Web: atrule.com | Phone: +92-313-6120356").FontSize(10);
                        });
                    }).GeneratePdf(ms);

                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating invoice PDF: {ex.Message}");
                throw;
            }
        }













        // Calculates the additional amount for uninvoiced resources for a given client.
        //public async Task<decimal> CalculateAdditionalAmountAsync(int clientId)
        //{
        //    try
        //    {
        //        var totalAmount = await _context.Resources
        //            .Where(r => r.ClientId == clientId && !r.IsInvoiced)
        //            .SumAsync(r => r.ConsumedTotalHours * r.Employee.HourlyRate);
        //        return totalAmount;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error calculating additional amount: {ex.Message}");
        //        throw;
        //    }
        //}

        // Retrieves an unpaid invoice (Pending or PartiallyPaid) for a client.
        //public async Task<Invoice?> GetUnpaidInvoiceForClientAsync(int clientId)
        //{
        //    try
        //    {
        //        return await _context.Invoices
        //            .Where(i => i.ClientId == clientId && i.InvoiceStatuses != InvoiceStatus.Paid)
        //            .OrderByDescending(i => i.InvoiceDate)
        //            .FirstOrDefaultAsync();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error fetching unpaid invoice for client {clientId}: {ex.Message}");
        //        return null;
        //    }
        //}

        // Creates a new invoice for a client and creates InvoiceItem entries for each uninvoiced resource.
        //public async Task<int> CreateInvoiceAsync(int clientId)
        //{
        //    try
        //    {
        //        var client = await _context.Clients
        //            .Include(c => c.Resources)
        //            .ThenInclude(r => r.Employee)
        //            .FirstOrDefaultAsync(c => c.ClientId == clientId);
        //        if (client == null)
        //            throw new Exception("Client not found!");

        //        // Select resources that haven't been invoiced yet.
        //        var newResources = client.Resources.Where(r => !r.IsInvoiced).ToList();
        //        if (!newResources.Any())
        //        {
        //            Console.WriteLine($"⚠️ No new resources to invoice for Client {clientId}.");
        //            return 0;
        //        }

        //        // Calculate total amount from uninvoiced resources.
        //        decimal totalAmount = newResources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

        //        var invoice = new Invoice
        //        {
        //            ClientId = clientId,
        //            InvoiceDate = DateTime.UtcNow,
        //            TotalAmount = totalAmount,
        //            PaidAmount = 0,
        //            RemainingAmount = totalAmount,
        //            CountryCurrencyId = client.CountryCurrencyId,
        //            InvoiceStatuses = InvoiceStatus.Pending,
        //            EmailStatus = "Not Sent"
        //        };

        //        await _context.Invoices.AddAsync(invoice);
        //        await _context.SaveChangesAsync(); // InvoiceId is now generated

        //        // Create an InvoiceItem for each uninvoiced resource and mark them as invoiced.
        //        foreach (var resource in newResources)
        //        {
        //            var invoiceItem = new InvoiceItem
        //            {
        //                InvoiceId = invoice.InvoiceId,
        //                ResourceId = resource.ResourceId,
        //                ConsumedHours = resource.ConsumedTotalHours,
        //                RatePerHour = resource.Employee.HourlyRate
        //            };
        //            invoice.InvoiceItems.Add(invoiceItem);
        //            resource.IsInvoiced = true;
        //            // Optionally, assign resource.InvoiceId = invoice.InvoiceId if maintaining that link.
        //        }

        //        // Update totals based on invoice items.
        //        invoice.TotalAmount = invoice.InvoiceItems.Sum(ii => ii.TotalAmount);
        //        invoice.RemainingAmount = invoice.TotalAmount; // No payment yet.
        //        await _context.SaveChangesAsync();

        //        Console.WriteLine($"✅ Invoice {invoice.InvoiceId} created successfully with amount: {invoice.TotalAmount:C}.");
        //        return invoice.InvoiceId;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"❌ Error creating invoice: {ex.Message}");
        //        throw;
        //    }
        //}


    }
}
