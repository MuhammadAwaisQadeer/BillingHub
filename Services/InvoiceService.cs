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
        public async Task<decimal> CalculateAdditionalAmountAsync(int clientId)
        {
            try
            {
                var totalAmount = await _context.Resources
                    .Where(r => r.ClientId == clientId && !r.IsInvoiced)
                    .SumAsync(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

                return totalAmount;
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

        // Fetches an unpaid invoice for a client.
        public async Task<Invoice?> GetUnpaidInvoiceForClientAsync(int clientId)
        {
            return await _context.Invoices
                .Where(i => i.ClientId == clientId && !i.IsPaid)
                .OrderByDescending(i => i.InvoiceDate)
                .FirstOrDefaultAsync();
        }

        // Creates a new invoice.
        public async Task<int> CreateInvoiceAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null)
                    throw new Exception("Client not found!");

                var newResources = client.Resources.Where(r => !r.IsInvoiced).ToList();
                decimal totalAmount = newResources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

                //if (totalAmount == 0)
                //{
                //    Console.WriteLine($"⚠️ No new resources to invoice for Client {clientId}.");
                //    return 0;
                //}

                var invoice = new Invoice
                {
                    ClientId = clientId,
                    InvoiceDate = DateTime.UtcNow,
                    TotalAmount = totalAmount,
                    CountryCurrencyId = client.CountryCurrencyId,
                    IsPaid = false,
                    EmailStatus = "Not Sent"
                };

                await _context.Invoices.AddAsync(invoice);

                // ✅ Mark resources as invoiced
                newResources.ForEach(r => r.IsInvoiced = true);

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

        public async Task<int> SaveInvoiceAsync(int clientId)
        {
            try
            {
                var client = await _context.Clients
                    .Include(c => c.Resources)
                    .ThenInclude(r => r.Employee)
                    .FirstOrDefaultAsync(c => c.ClientId == clientId);

                if (client == null)
                    throw new Exception("Client not found!");

                // Only select resources that haven't been invoiced yet
                var newResources = client.Resources.Where(r => !r.IsInvoiced).ToList();
                decimal newAmount = newResources.Sum(r => r.ConsumedTotalHours * r.Employee.HourlyRate);

                if (newAmount == 0)
                {
                    Console.WriteLine($"⚠️ No new resources to invoice for Client {clientId}.");
                    var unpaidInvoice = await GetUnpaidInvoiceForClientAsync(clientId);
                    return unpaidInvoice?.InvoiceId ?? 0;
                }

                var existingUnpaidInvoice = await GetUnpaidInvoiceForClientAsync(clientId);

                if (existingUnpaidInvoice != null && !existingUnpaidInvoice.IsPaid)
                {
                    // Update existing invoice
                    existingUnpaidInvoice.TotalAmount += newAmount;

                    // Only add resources that are not already invoiced
                    newResources.ForEach(r =>
                    {
                        r.IsInvoiced = true;
                        r.InvoiceId = existingUnpaidInvoice.InvoiceId;
                    });

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Updated Invoice {existingUnpaidInvoice.InvoiceId} with {newAmount:C}.");
                    return existingUnpaidInvoice.InvoiceId;
                }
                else
                {
                    // Create a new invoice
                    var newInvoice = new Invoice
                    {
                        ClientId = clientId,
                        InvoiceDate = DateTime.UtcNow,
                        TotalAmount = newAmount,
                        CountryCurrencyId = client.CountryCurrencyId,
                        IsPaid = false,
                        EmailStatus = "Not Sent"
                    };

                    await _context.Invoices.AddAsync(newInvoice);
                    await _context.SaveChangesAsync();

                    newResources.ForEach(r =>
                    {
                        r.IsInvoiced = true;
                        r.InvoiceId = newInvoice.InvoiceId;
                    });

                    await _context.SaveChangesAsync();
                    Console.WriteLine($"✅ Invoice {newInvoice.InvoiceId} created successfully with amount: {newAmount:C}.");
                    return newInvoice.InvoiceId;
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
                  .ThenInclude(r => r.OwnerProfile)
              .Include(c => c.Resources)
                  .ThenInclude(r => r.Employee)
              .Include(c => c.CountryCurrency)
              .FirstOrDefaultAsync();

                if (client == null)
                    throw new Exception("Client not found!");

                var unpaidInvoice = await _context.Invoices
                 .Where(i => i.ClientId == clientId && !i.IsPaid)
                 .Include(i => i.Resources)
                     .ThenInclude(r => r.OwnerProfile)
                 .Include(i => i.Resources)
                     .ThenInclude(r => r.Employee)
                 .Include(i => i.CountryCurrency)
                 .Include(i => i.Client)
                 .OrderByDescending(i => i.InvoiceDate)
                 .FirstOrDefaultAsync();

                if (unpaidInvoice == null)
                    throw new Exception("No unpaid invoice found!");

                // Use only the resources linked to this unpaid invoice
                var resourcesForInvoice = await _context.Resources
                 .Where(r => r.InvoiceId == unpaidInvoice.InvoiceId)
                 .Include(r => r.Employee)
                 .Include(r => r.OwnerProfile)
                 .ToListAsync();

                decimal totalAmount = unpaidInvoice.TotalAmount;

                var ownerProfile = client.Resources.FirstOrDefault()?.OwnerProfile;

                if (ownerProfile == null)
                {
                    ownerProfile = new OwnerProfile
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
                }


                CultureInfo culture;
                string currencySymbol = client?.CountryCurrency?.Symbol ?? "$"; // Default USD symbol
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

                                // Resource Details Table
                                col.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn();
                                        columns.ConstantColumn(60);
                                        columns.ConstantColumn(80);
                                        columns.ConstantColumn(120);
                                    });

                                    // Table Header
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

                                    // Resource rows
                                    foreach (var resource in resourcesForInvoice)
                                    {
                                        decimal subtotal = resource.ConsumedTotalHours * resource.Employee.HourlyRate;

                                        table.Cell().ColumnSpan(4).Border(1).Padding(5)
                                            .Text($"{resource.ResourceName} - {resource.Employee.Designation} - Monthly Contract - {DateTime.Now:MMMM yyyy}");

                                        table.Cell().ColumnSpan(1).Border(1).Padding(5)
                                            .Text($"Amount in {currencySymbol}: {resource.ConsumedTotalHours} Hours X {resource.Employee.HourlyRate.ToString("C2", culture)} = {subtotal.ToString("C2", culture)}")
                                            .Italic();

                                        table.Cell().Border(1).Padding(5).AlignCenter().Text("1");
                                        table.Cell().Border(1).Padding(5).AlignCenter().Text($"{resource.Employee.HourlyRate.ToString("C2", culture)}");
                                        table.Cell().Border(1).Padding(5).AlignCenter().Text($"{subtotal.ToString("C2", culture)}");
                                    }

                                    // Total Section
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

                            // FOOTER
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
