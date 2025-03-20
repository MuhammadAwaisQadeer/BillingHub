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

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            try
            {
                return await _context.Invoices
                    .Include(i => i.Client)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error fetching invoices: {ex.Message}");
                return new List<Invoice>();
            }
        }

        public async Task MarkInvoiceAsPaidAsync(int invoiceId)
        {
            try
            {
                var invoice = await _context.Invoices.FindAsync(invoiceId);
                if (invoice == null)
                {
                    Console.WriteLine($"⚠️ Invoice with ID {invoiceId} not found.");
                    return;
                }
                if (invoice.IsPaid)
                {
                    Console.WriteLine($"✅ Invoice {invoiceId} is already marked as Paid.");
                    return;
                }
                invoice.IsPaid = true;
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Invoice {invoiceId} marked as Paid.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error marking invoice as paid: {ex.Message}");
            }
        }

        // Retrieves an existing unpaid invoice for the client.
        public async Task<Invoice> GetUnpaidInvoiceForClientAsync(int clientId)
        {
            // Retrieves the first unpaid invoice for the client.
            return await _context.Invoices.FirstOrDefaultAsync(i => i.ClientId == clientId && !i.IsPaid);
        }


        // Calculates the additional amount from resources that haven't been invoiced.
        public async Task<decimal> CalculateAdditionalAmountAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found");

                // Only include resources not yet invoiced.
                return client.Resources
                    .Where(r => !r.IsInvoiced)
                    .Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error calculating additional amount: {ex.Message}");
                throw;
            }
        }

        // Updates an existing invoice.
        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            try
            {
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Invoice {invoice.InvoiceId} updated successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error updating invoice: {ex.Message}");
                throw;
            }
        }

        public async Task<int> CreateInvoiceAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found!");

                // Sum only the resources that haven't been invoiced.
                var newResources = client.Resources.Where(r => !r.IsInvoiced).ToList();
                decimal totalAmount = newResources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

                var invoice = new Invoice
                {
                    ClientId = clientId,
                    InvoiceDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    CountryCurrencyId = client?.CountryCurrencyId ?? default,
                    IsPaid = false,
                    EmailStatus = "Not Sent"
                };

                await _context.Invoices.AddAsync(invoice);
                // Mark these resources as invoiced.
                foreach (var resource in newResources)
                {
                    resource.IsInvoiced = true;
                }
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Invoice {invoice.InvoiceId} created successfully with amount: {totalAmount:C}.");
                return invoice.InvoiceId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error creating invoice: {ex.Message}");
                throw;
            }
        }

        // Saves or updates an invoice for the client based on unpaid invoice existence.
        public async Task<int> SaveInvoiceAsync(int clientId)
        {
            try
            {
                // 1. Retrieve the client with all resources and employee details.
                var client = await _context.Clients
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found!");

                // 2. Filter out resources that have already been invoiced.
                var newResources = client.Resources.Where(r => !r.IsInvoiced).ToList();
                decimal newAmount = newResources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

                // 3. If there is no new resource consumption, return the existing unpaid invoice (if any).
                if (newAmount == 0)
                {
                    Console.WriteLine("No new resources to invoice.");
                    var existingInvoice = await GetUnpaidInvoiceForClientAsync(clientId);
                    return existingInvoice?.InvoiceId ?? 0;
                }

                // 4. Check for an existing unpaid invoice.
                var existingInvoiceUnpaid = await GetUnpaidInvoiceForClientAsync(clientId);
                if (existingInvoiceUnpaid != null)
                {
                    // If the existing unpaid invoice is still unpaid, update it only with the amount from new resources.
                    if (!existingInvoiceUnpaid.IsPaid)
                    {
                        existingInvoiceUnpaid.TotalAmount += newAmount;
                        // Mark only the new resources as invoiced.
                        foreach (var resource in newResources)
                        {
                            resource.IsInvoiced = true;
                        }
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"✅ Existing invoice {existingInvoiceUnpaid.InvoiceId} updated with additional amount: {newAmount:C}.");
                        return existingInvoiceUnpaid.InvoiceId;
                    }
                    else
                    {
                        // If the existing invoice is already paid, create a new invoice.
                        return await CreateInvoiceAsync(clientId);
                    }
                }
                else
                {
                    // No existing unpaid invoice, so create a new one.
                    return await CreateInvoiceAsync(clientId);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error saving invoice: {ex.Message}");
                throw;
            }
        }


        // Sends an invoice email and updates the EmailStatus.
        public async Task<bool> SendInvoiceToClientAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients.FindAsync(clientId);
                if (client == null)
                    throw new Exception("Client not found.");

                var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.ClientId == clientId);
                if (invoice == null)
                    throw new Exception("Invoice not found.");

                byte[] invoicePdf = await GenerateInvoicePdfAsync(clientId);
                string fileName = $"Invoice_{clientId}.pdf";

                await _emailService.SendInvoiceEmailAsync(client.Email, invoicePdf, fileName);

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
                    Console.WriteLine($"Invoice with ID {invoiceId} not found.");
                    return;
                }
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
                Console.WriteLine($"✅ Invoice {invoiceId} deleted successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error deleting invoice: {ex.Message}");
                throw;
            }
        }


        // Generates a PDF for the given client using QuestPDF
        public async Task<byte[]> GenerateInvoicePdfAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Where(c => c.ClientId == clientId)
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found!");

                var paymentProfile = await _context.Owners.FirstOrDefaultAsync();
                if (paymentProfile == null)
                    throw new Exception("Owners Payment profile not found!");

                decimal totalAmount = client.Resources
                           .Where(r => !r.IsInvoiced)
                           .Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);


                // Determine culture based on client's currency
                // Fetch currency symbol from the client's associated CountryCurrency table
                CultureInfo culture;
                string currencySymbol = client?.CountryCurrency?.Symbol ?? "$"; // Default to USD symbol

                if (!string.IsNullOrEmpty(client?.CountryCurrency?.CurrencyCode))
                {
                    try
                    {
                        culture = new CultureInfo(client.CountryCurrency.CurrencyCode);
                    }
                    catch (CultureNotFoundException)
                    {
                        culture = new CultureInfo("en-US"); // Fallback to default
                    }
                }
                else
                {
                    culture = new CultureInfo("en-US"); // Default if currency is not set
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

                            // ---- HEADER ----
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

                            page.Content().Column(col =>
                            {
                                col.Item().PaddingTop(20);

                                // ---- PAYMENT INSTRUCTIONS ----
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

                                    AddPaymentRow("Currency:", paymentProfile?.CountryCurrency?.CurrencyCode ?? "USD");
                                    AddPaymentRow("Bank Name:", paymentProfile.BankName);
                                    AddPaymentRow("Swift Code:", paymentProfile.Swiftcode);
                                    AddPaymentRow("Account Title:", paymentProfile.AccountTitle);
                                    AddPaymentRow("IBAN:", paymentProfile.IBANNumber);
                                    AddPaymentRow("Branch Address:", paymentProfile.BranchAddress);
                                    AddPaymentRow("Beneficiary Address:", paymentProfile.BeneficeryAddress);
                                });

                                col.Item().PaddingTop(10);

                                // ---- SERVICE DETAILS TABLE ----
                                col.Item().Container().PaddingTop(5);

                                col.Item().PaddingTop(5);
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(120);
                                    });

                                    table.Header(header =>
                                    {
                                        string headerColor = "#2F4F4F";

                                        header.Cell().Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span("Description").FontColor(Colors.White).Bold());

                                        header.Cell().Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span("Quantity").FontColor(Colors.White).Bold());

                                        header.Cell().Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span($"Rate ({currencySymbol})").FontColor(Colors.White).Bold());

                                        header.Cell().Background(Color.FromHex(headerColor)).Padding(5)
                                            .Text(text => text.Span($"Subtotal ({currencySymbol})").FontColor(Colors.White).Bold());
                                    });

                                    foreach (var resource in client.Resources.Where(r => !r.IsInvoiced))
                                    {
                                        table.Cell().ColumnSpan(4).Border(1).Padding(5).Text($"{resource.ResourceName} - {resource.Employee.Designation} - Monthly Contract - {DateTime.Now:MMMM yyyy}");

                                        table.Cell().ColumnSpan(1).Border(1).Padding(5).Text($"Calculation\nAmount in {currencySymbol}: {resource.ConsumedTotalHours} Hours X {resource.Employee.HourlyRate.ToString("C2", culture)} = {(resource.ConsumedTotalHours * resource.Employee.HourlyRate).ToString("C2", culture)}").Italic(); ;

                                        table.Cell().Border(1).Padding(5).AlignCenter().Text("1");
                                        table.Cell().Border(1).Padding(5).AlignCenter().Text($"{resource.Employee.HourlyRate.ToString("C2", culture)}");
                                        table.Cell().Border(1).Padding(5).AlignCenter().Text($"{(resource.ConsumedTotalHours * resource.Employee.HourlyRate).ToString("C2", culture)}");
                                    }

                                    // Last Section: Software Consultancy Services & Total Amount
                                    table.Cell().ColumnSpan(1).Border(1).Padding(5).Text("Software Consultancy Services").Bold();
                                    table.Cell().ColumnSpan(3).Border(1).Table(subTable =>
                                    {
                                        subTable.ColumnsDefinition(subCols =>
                                        {
                                            subCols.RelativeColumn();
                                            subCols.ConstantColumn(100);
                                        });

                                        subTable.Cell().Padding(5).Text("Total").Bold();
                                        subTable.Cell().Padding(5).AlignRight().Text($"{totalAmount.ToString("C2", culture)}").Bold();

                                        subTable.Cell().Padding(5).Text("Total Due By").Bold();
                                        subTable.Cell().Padding(5).AlignRight().Text($"{DateTime.Now.AddDays(5):MM/dd/yyyy}").Bold();
                                    });
                                });

                                col.Item().PaddingTop(5);
                            });

                            // ---- FOOTER ----
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
    }
}
